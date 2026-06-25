using BullseyeAPI.Application.DTOs;
using BullseyeAPI.Application.Interfaces;
using BullseyeAPI.Application.Services;
using BullseyeAPI.Domain.Entities;
using BullseyeAPI.Domain.Rules;
using BullseyeAPI.Hubs;
using Microsoft.AspNetCore.SignalR;
using Xunit;

namespace Tests;

// Covers the Dartbot turn-trigger logic added to GameService.SubmitManualTurnAsync.
// Uses small hand-written fakes instead of a mocking library, in keeping with
// this test project's existing lean dependency footprint (see DartGameRulesTests).
public class GameServiceBotTests
{
    private static GameService CreateService(FakeGameRepository gameRepository, IAiDartbotClient aiClient)
    {
        return new GameService(gameRepository, new DartGameRules(), new FakePlayerRepository(), new FakeHubContext(), aiClient);
    }

    [Fact]
    public async Task SubmitManualTurnAsync_InVsBotGame_TriggersOneBotTurn_AfterNonFinishingHumanTurn()
    {
        // Arrange: fresh 501 game with the Dartbot as PlayerId 2
        var game = new Game { Id = 1, Variant = "501", BotPlayerId = 2 };
        var repository = new FakeGameRepository(game);
        var aiClient = new FakeAiDartbotClient(totalToReturn: 45);
        var service = CreateService(repository, aiClient);

        // Act: the human (PlayerId 1) throws a normal, non-finishing visit
        await service.SubmitManualTurnAsync(new SubmitTurnRequest { GameId = 1, PlayerId = 1, TotalPoints = 60 });

        // Assert: exactly one human turn and one bot turn were recorded
        Assert.Equal(2, game.Turns.Count);
        Assert.Equal(1, game.Turns.ElementAt(0).PlayerId);
        Assert.Equal(441, game.Turns.ElementAt(0).ScoreAfter); // 501 - 60
        Assert.Equal(2, game.Turns.ElementAt(1).PlayerId);
        Assert.Equal(456, game.Turns.ElementAt(1).ScoreAfter); // 501 - 45
        Assert.Null(game.WinnerId);
    }

    [Fact]
    public async Task SubmitManualTurnAsync_WhenHumanTurnWinsTheLeg_TriggersBotsOpeningThrowForNextLeg()
    {
        // Arrange: player 1 is already sitting on a reachable checkout (40)
        var priorTurn = new Turn
        {
            PlayerId = 1,
            ScoreBefore = 501,
            // Must be non-zero, or GetCurrentScoreForPlayer's leg-boundary scan
            // would mistake this seeded turn itself for a leg that already ended
            ScoreAfter = 40,
            Scores = new List<Score> { new Score { Points = 461, Segment = "Manual", DartNumber = 1 } },
        };
        var game = new Game { Id = 1, Variant = "501", BotPlayerId = 2, Turns = new List<Turn> { priorTurn } };
        var repository = new FakeGameRepository(game);
        var aiClient = new FakeAiDartbotClient(totalToReturn: 99);
        var service = CreateService(repository, aiClient);

        // Act: the human checks out exactly on 40, winning leg 1
        await service.SubmitManualTurnAsync(new SubmitTurnRequest { GameId = 1, PlayerId = 1, TotalPoints = 40 });

        // Assert: the bot's reactive response fires immediately as usual, but
        // since the human's turn just crossed a leg boundary, the bot's score
        // is computed fresh for leg 2 instead of continuing leg 1's scoring -
        // this is the bot's opening throw of leg 2.
        Assert.Equal(3, game.Turns.Count);
        Assert.Equal(1, game.WinnerId); // still records leg 1's winner
        Assert.Equal(2, game.Turns.ElementAt(2).PlayerId);
        Assert.Equal(402, game.Turns.ElementAt(2).ScoreAfter); // 501 - 99, fresh leg
    }

    [Fact]
    public async Task SubmitManualTurnAsync_WhenBotTurnWinsTheLegItStarted_DoesNotTriggerAnotherBotTurn()
    {
        // Arrange: leg 1 already finished (human won it), so leg 2 is in
        // progress and was opened by the bot per the starter alternation.
        // The bot is now sitting on a reachable checkout (40).
        var leg1EndTurn = new Turn
        {
            PlayerId = 1,
            ScoreBefore = 40,
            ScoreAfter = 0,
            Scores = new List<Score> { new Score { Points = 40, Segment = "Manual", DartNumber = 1 } },
        };
        var botOpeningThrow = new Turn
        {
            PlayerId = 2,
            ScoreBefore = 501,
            ScoreAfter = 40,
            Scores = new List<Score> { new Score { Points = 461, Segment = "Bot", DartNumber = 1 } },
        };
        var game = new Game
        {
            Id = 1,
            Variant = "501",
            BotPlayerId = 2,
            Turns = new List<Turn> { leg1EndTurn, botOpeningThrow },
        };
        var repository = new FakeGameRepository(game);
        // Returns 40 so the bot's reactive response below checks out exactly
        // (its leg-2 score is 40, set up via botOpeningThrow above).
        var aiClient = new FakeAiDartbotClient(totalToReturn: 40);
        var service = CreateService(repository, aiClient);

        // Act: the human throws a non-finishing visit in leg 2; the bot then
        // responds and, per the fake client, checks out and wins leg 2
        await service.SubmitManualTurnAsync(new SubmitTurnRequest { GameId = 1, PlayerId = 1, TotalPoints = 60 });

        // Assert: leg 2 ends with the bot's checkout (the 4th turn overall:
        // leg1EndTurn, botOpeningThrow, the human's leg-2 visit, then the
        // bot's winning one) - exactly one bot turn per human turn, never more.
        Assert.Equal(4, game.Turns.Count);
        Assert.Equal(2, game.WinnerId);
        Assert.Equal(1, aiClient.CallCount);
    }

    [Fact]
    public async Task SubmitManualTurnAsync_InHumanVsHumanGame_NeverCallsTheAiService()
    {
        // Arrange: no BotPlayerId set - an ordinary human-vs-human game
        var game = new Game { Id = 1, Variant = "501", BotPlayerId = null };
        var repository = new FakeGameRepository(game);
        var aiClient = new FakeAiDartbotClient(totalToReturn: 45);
        var service = CreateService(repository, aiClient);

        // Act
        await service.SubmitManualTurnAsync(new SubmitTurnRequest { GameId = 1, PlayerId = 1, TotalPoints = 60 });

        // Assert: only the human's own turn was recorded
        Assert.Single(game.Turns);
        Assert.Equal(0, aiClient.CallCount);
    }

    [Fact]
    public async Task SubmitManualTurnAsync_WhenAiServiceIsUnreachable_SkipsBotTurnWithoutThrowing()
    {
        // Arrange: the fake AI client returns null, simulating a down/unreachable ai-service
        var game = new Game { Id = 1, Variant = "501", BotPlayerId = 2 };
        var repository = new FakeGameRepository(game);
        var aiClient = new FakeAiDartbotClient(totalToReturn: null);
        var service = CreateService(repository, aiClient);

        // Act
        var success = await service.SubmitManualTurnAsync(new SubmitTurnRequest { GameId = 1, PlayerId = 1, TotalPoints = 60 });

        // Assert: the human's turn still went through; the bot simply never got one
        Assert.True(success);
        Assert.Single(game.Turns);
    }

    private class FakeGameRepository : IGameRepository
    {
        private readonly Game _game;
        private int _nextId;

        public FakeGameRepository(Game game)
        {
            _game = game;
            _nextId = (game.Turns.Count == 0 ? 0 : game.Turns.Max(t => t.Id)) + 1;
        }

        public Task<Game?> GetByIdAsync(int id) => Task.FromResult(id == _game.Id ? _game : null);
        public Task AddAsync(Game game) => Task.CompletedTask;
        public Task UpdateAsync(Game game) => Task.CompletedTask;

        // Mimics EF Core auto-assigning the database-generated Id once a Turn
        // is actually persisted - GameService's leg-boundary logic
        // (GetCurrentScoreForPlayer, IsBotTurnToStartLeg) orders by Turn.Id to
        // find the most recent turn/leg boundary, so tests with multiple
        // turns need real, increasing Ids just like production data has.
        public Task SaveChangesAsync()
        {
            foreach (var turn in _game.Turns.Where(t => t.Id == 0))
            {
                turn.Id = _nextId++;
            }
            return Task.CompletedTask;
        }
    }

    // Never expected to be exercised: every test game has an empty Players
    // collection, so GameService's stats update is a no-op (see the comment
    // on UpdateStatsForLegAsync).
    private class FakePlayerRepository : IPlayerRepository
    {
        public Task<Player?> GetByIdAsync(int id) => throw new NotImplementedException();
        public Task<Player?> GetByEmailAsync(string email) => throw new NotImplementedException();
        public Task<Player?> GetByVerificationTokenAsync(string token) => throw new NotImplementedException();
        public Task AddAsync(Player player) => throw new NotImplementedException();
        public Task UpdateAsync(Player player) => throw new NotImplementedException();
        public Task SaveChangesAsync() => throw new NotImplementedException();
    }

    private class FakeAiDartbotClient : IAiDartbotClient
    {
        private readonly int? _totalToReturn;
        public int CallCount { get; private set; }

        public FakeAiDartbotClient(int? totalToReturn) => _totalToReturn = totalToReturn;

        public Task<int?> GetBotTurnTotalAsync(int remainingScore, string variant, string difficulty)
        {
            CallCount++;
            return Task.FromResult(_totalToReturn);
        }
    }

    private class FakeHubContext : IHubContext<GameHub>
    {
        public IHubClients Clients { get; } = new FakeHubClients();
        public IGroupManager Groups { get; } = new FakeGroupManager();
    }

    private class FakeHubClients : IHubClients
    {
        private readonly IClientProxy _proxy = new FakeClientProxy();

        public IClientProxy All => _proxy;
        public IClientProxy AllExcept(IReadOnlyList<string> excludedConnectionIds) => _proxy;
        public IClientProxy Client(string connectionId) => _proxy;
        public IClientProxy Clients(IReadOnlyList<string> connectionIds) => _proxy;
        public IClientProxy Group(string groupName) => _proxy;
        public IClientProxy GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds) => _proxy;
        public IClientProxy Groups(IReadOnlyList<string> groupNames) => _proxy;
        public IClientProxy OthersInGroup(string groupName) => _proxy;
        public IClientProxy User(string userId) => _proxy;
        public IClientProxy Users(IReadOnlyList<string> userIds) => _proxy;
    }

    private class FakeClientProxy : IClientProxy
    {
        public Task SendCoreAsync(string method, object?[] args, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private class FakeGroupManager : IGroupManager
    {
        public Task AddToGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task RemoveFromGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
