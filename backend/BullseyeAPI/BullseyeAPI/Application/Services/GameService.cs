using BullseyeAPI.Application.DTOs;
using BullseyeAPI.Application.Interfaces;
using BullseyeAPI.Domain.Entities;
using BullseyeAPI.Domain.Rules;
using Microsoft.AspNetCore.SignalR;
using BullseyeAPI.Hubs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BullseyeAPI.Application.Services;

// Orchestrates a live match: recording darts, detecting busts/checkouts,
// ending the game, pushing live updates over SignalR, and updating every
// participating player's lifetime stats once a leg ends.
//
// SubmitScoreAsync records one dart at a time; SubmitManualTurnAsync records
// a whole visit (the numpad's combined 3-dart total) in one call and is what
// the frontend actually uses. Mixing the two for the same player would merge
// a later visit into an still-open turn from the other path, so don't.
public class GameService : IGameService
{
    private readonly IGameRepository _gameRepository;
    private readonly DartGameRules _rules;
    private readonly IPlayerRepository _playerRepository;
    private readonly IHubContext<GameHub> _hubContext;
    private readonly IAiDartbotClient _aiDartbotClient;

    public GameService(
        IGameRepository gameRepository,
        DartGameRules rules,
        IPlayerRepository playerRepository,
        IHubContext<GameHub> hubContext,
        IAiDartbotClient aiDartbotClient)
    {
        _gameRepository = gameRepository;
        _rules = rules;
        _playerRepository = playerRepository;
        _hubContext = hubContext;
        _aiDartbotClient = aiDartbotClient;
    }

    public async Task<GameDto?> GetGameAsync(int gameId)
    {
        var game = await _gameRepository.GetByIdAsync(gameId);
        if (game == null) return null;

        var turnDtos = game.Turns.OrderBy(t => t.ThrownAt).Select(t => new TurnDto
        {
            PlayerId = t.PlayerId,
            ScoreBefore = t.ScoreBefore,
            IsBust = t.IsBust,
            ScoreAfter = t.IsBust ? t.ScoreBefore : t.ScoreBefore - t.Scores.Sum(s => s.Points),
            Scores = t.Scores.OrderBy(s => s.DartNumber).Select(s => new ScoreDto
            {
                Points = s.Points,
                Segment = s.Segment,
                DartNumber = s.DartNumber
            }).ToList()
        }).ToList();

        return new GameDto
        {
            Id = game.Id,
            Variant = game.Variant,
            StartedAt = game.StartedAt,
            WinnerId = game.WinnerId,
            Turns = turnDtos
        };
    }

    // Per-dart submission: records ONE dart, attaching to the player's
    // current turn while it still has fewer than 3 darts. Currently unused
    // by the frontend (the numpad submits whole turns via
    // SubmitManualTurnAsync below), kept for a future dart-by-dart UI.
    public async Task<bool> SubmitScoreAsync(SubmitScoreRequest request)
    {
        var game = await _gameRepository.GetByIdAsync(request.GameId);
        if (game == null) return false;

        var currentTurn = game.Turns.LastOrDefault(t => t.PlayerId == request.PlayerId && t.Scores.Count < 3 && !t.IsBust);

        // Initializes a new turn if none exists, or if the previous one is completed
        if (currentTurn == null)
        {
            // NOTE: only distinguishes 501 vs 301 - a "701" or "170" variant
            // would silently start at 301 here.
            int startScore = game.Variant == "501" ? 501 : 301;
            int scoreBefore = GetCurrentScoreForPlayer(game, request.PlayerId, startScore);

            currentTurn = new Turn
            {
                PlayerId = request.PlayerId,
                ScoreBefore = scoreBefore,
                ThrownAt = DateTime.UtcNow,
                Scores = new List<Score>()
            };
            game.Turns.Add(currentTurn);
        }

        // Auto-calculates the dart number if the frontend does not provide one
        if (request.DartNumber <= 0)
        {
            request.DartNumber = currentTurn.Scores.Count + 1;
        }

        // Prevents duplicate dart entries within the same turn
        if (currentTurn.Scores.Any(s => s.DartNumber == request.DartNumber))
        {
            throw new ArgumentException($"Dart number {request.DartNumber} has already been thrown in this turn!");
        }

        int currentTurnTotal = currentTurn.Scores.Sum(s => s.Points);
        int scoreBeforeThisThrow = currentTurn.ScoreBefore - currentTurnTotal;

        if (_rules.IsWinningThrow(scoreBeforeThisThrow, request.Points, request.IsDouble, game.Variant))
        {
            game.EndedAt = DateTime.UtcNow;
            game.WinnerId = request.PlayerId;
            currentTurn.Scores.Add(new Score { Points = request.Points, Segment = request.Segment, DartNumber = request.DartNumber });
            
            // Forces the database to explicitly record the remaining score as zero
            currentTurn.ScoreAfter = 0;
            
            await _gameRepository.UpdateAsync(game);
            await _gameRepository.SaveChangesAsync();
            await UpdateStatsForLegAsync(game, request.PlayerId);
        }
        // Evaluates a bust throw
        else if (!_rules.IsValidScore(scoreBeforeThisThrow, request.Points, request.IsDouble, game.Variant))
        {
            currentTurn.IsBust = true;
            currentTurn.Scores.Add(new Score { Points = request.Points, Segment = request.Segment, DartNumber = request.DartNumber });
            
            // Reverts the remaining score due to the bust explicitly
            currentTurn.ScoreAfter = currentTurn.ScoreBefore;
            
            await _gameRepository.UpdateAsync(game);
            await _gameRepository.SaveChangesAsync();
        }
        // Evaluates a valid throw
        else
        {
            currentTurn.Scores.Add(new Score { Points = request.Points, Segment = request.Segment, DartNumber = request.DartNumber });
            
            // Forces the database to explicitly record the exact remaining score
            currentTurn.ScoreAfter = currentTurn.ScoreBefore - currentTurn.Scores.Sum(s => s.Points);
            
            await _gameRepository.UpdateAsync(game);
            await _gameRepository.SaveChangesAsync();
        }

        // Broadcasts a live update signal to the Angular frontend
        await _hubContext.Clients.All.SendAsync("GameUpdated", game.Id);

        return true;
    }

    // Whole-visit submission: the frontend numpad sends one combined total
    // per turn, so this always creates a fresh Turn rather than reusing one
    // the way SubmitScoreAsync does. Delegates the actual bust/win/score
    // handling to ProcessTurnAsync below, which is shared with the Dartbot
    // turn triggered right after, so both paths run through identical rules.
    public async Task<bool> SubmitManualTurnAsync(SubmitTurnRequest request)
    {
        var game = await _gameRepository.GetByIdAsync(request.GameId);
        if (game == null) return false;

        await ProcessTurnAsync(game, request.PlayerId, request.TotalPoints);

        // Vs-Dartbot games have no server-side "whose turn" state (see the
        // class-level note); instead we rely on the human always starting
        // every leg, so a human turn that didn't just win the leg is always
        // immediately followed by exactly one bot turn. If the AI service is
        // unreachable, the bot simply skips its turn rather than blocking or
        // failing the human's request.
        if (game.BotPlayerId.HasValue && game.WinnerId == null && request.PlayerId != game.BotPlayerId.Value)
        {
            int startScore = game.Variant == "501" ? 501 : 301;
            int botCurrentScore = GetCurrentScoreForPlayer(game, game.BotPlayerId.Value, startScore);

            var botTotal = await _aiDartbotClient.GetBotTurnTotalAsync(botCurrentScore, game.Variant, game.BotDifficulty);
            if (botTotal.HasValue)
            {
                await ProcessTurnAsync(game, game.BotPlayerId.Value, botTotal.Value);
            }
        }

        // Broadcasts a live update signal to the Angular frontend
        await _hubContext.Clients.All.SendAsync("GameUpdated", game.Id);

        return true;
    }

    // Shared bust/win/normal handling for one (playerId, totalPoints) visit,
    // used by both a human's manual turn and the Dartbot's auto-generated
    // turn above.
    //
    // NOTE: re-implements win/bust/normal inline instead of calling
    // DartGameRules, and doesn't enforce double-out - a manual "40" entered
    // from 40 always wins even in a double-out variant.
    private async Task ProcessTurnAsync(Game game, int playerId, int totalPoints)
    {
        int startScore = game.Variant == "501" ? 501 : 301;
        int currentScore = GetCurrentScoreForPlayer(game, playerId, startScore);

        bool isBotTurn = game.BotPlayerId.HasValue && playerId == game.BotPlayerId.Value;
        string segment = isBotTurn ? "Bot" : "Manual";
        string bustSegment = isBotTurn ? "Bot Bust" : "Manual Bust";

        var newTurn = new Turn
        {
            PlayerId = playerId,
            ScoreBefore = currentScore,
            ThrownAt = DateTime.UtcNow,
            Scores = new List<Score>()
        };

        // A finish only counts if currentScore could legally be checked out
        // on a double this visit - claiming e.g. "169" as a finish is
        // impossible (no dart combination reaches exactly zero from a bogey
        // number on a double), so that's treated as a bust instead of a win.
        bool reachesZero = totalPoints == currentScore;
        bool isLegalFinish = reachesZero
            && (!_rules.RequiresDoubleOut(game.Variant) || _rules.IsCheckoutPossible(currentScore, game.Variant));

        // Each branch pads newTurn.Scores up to 3 entries so every Turn has
        // the same shape regardless of submission path.
        if (isLegalFinish)
        {
            game.EndedAt = DateTime.UtcNow;
            game.WinnerId = playerId;
            newTurn.Scores.Add(new Score { Points = totalPoints, Segment = segment, DartNumber = 1 });

            if (newTurn.Scores.Count < 3) newTurn.Scores.Add(new Score { Points = 0, Segment = "-", DartNumber = 2 });
            if (newTurn.Scores.Count < 3) newTurn.Scores.Add(new Score { Points = 0, Segment = "-", DartNumber = 3 });

            newTurn.ScoreAfter = 0;

            game.Turns.Add(newTurn);
            await _gameRepository.UpdateAsync(game);
            await _gameRepository.SaveChangesAsync();

            await UpdateStatsForLegAsync(game, playerId);
        }
        // Overshooting, leaving exactly 1 (unreachable on a double), or
        // claiming an impossible checkout is a bust
        else if (totalPoints > currentScore || (currentScore - totalPoints) == 1 || reachesZero)
        {
            newTurn.IsBust = true;
            newTurn.Scores.Add(new Score { Points = totalPoints, Segment = bustSegment, DartNumber = 1 });

            if (newTurn.Scores.Count < 3) newTurn.Scores.Add(new Score { Points = 0, Segment = "-", DartNumber = 2 });
            if (newTurn.Scores.Count < 3) newTurn.Scores.Add(new Score { Points = 0, Segment = "-", DartNumber = 3 });

            newTurn.ScoreAfter = newTurn.ScoreBefore;

            game.Turns.Add(newTurn);
            await _gameRepository.UpdateAsync(game);
            await _gameRepository.SaveChangesAsync();
        }
        else
        {
            newTurn.Scores.Add(new Score { Points = totalPoints, Segment = segment, DartNumber = 1 });

            if (newTurn.Scores.Count < 3) newTurn.Scores.Add(new Score { Points = 0, Segment = "-", DartNumber = 2 });
            if (newTurn.Scores.Count < 3) newTurn.Scores.Add(new Score { Points = 0, Segment = "-", DartNumber = 3 });

            newTurn.ScoreAfter = newTurn.ScoreBefore - totalPoints;

            game.Turns.Add(newTurn);
            await _gameRepository.UpdateAsync(game);
            await _gameRepository.SaveChangesAsync();
        }
    }

    // There's no "leg" entity - a Game's Turns span every leg played back to
    // back. A leg boundary is the most recent non-bust turn (by anyone) that
    // reached zero; a player's score is derived only from their own turns
    // after that point, so a new leg starts back at startScore.
    private static int GetCurrentScoreForPlayer(Game game, int playerId, int startScore)
    {
        var lastLegEndTurn = game.Turns
            .Where(t => !t.IsBust && t.ScoreAfter == 0)
            .OrderByDescending(t => t.Id)
            .FirstOrDefault();

        var lastTurn = game.Turns
            .Where(t => t.PlayerId == playerId && (lastLegEndTurn == null || t.Id > lastLegEndTurn.Id))
            .OrderByDescending(t => t.Id)
            .FirstOrDefault();

        if (lastTurn == null) return startScore;
        return lastTurn.IsBust ? lastTurn.ScoreBefore : lastTurn.ScoreBefore - lastTurn.Scores.Sum(s => s.Points);
    }

    // Mirrors the leg-boundary logic in GetCurrentScoreForPlayer above, but
    // is called right after the leg-ending turn was added - so the most
    // recent zero-reaching turn IS this leg's end, and we want everything
    // after the one before that.
    private static List<Turn> GetCurrentLegTurns(Game game, int playerId)
    {
        var legEndTurnIds = game.Turns
            .Where(t => !t.IsBust && t.ScoreAfter == 0)
            .OrderByDescending(t => t.Id)
            .Select(t => t.Id)
            .ToList();

        int? previousLegEndId = legEndTurnIds.Count >= 2 ? legEndTurnIds[1] : null;

        return game.Turns
            .Where(t => t.PlayerId == playerId && (previousLegEndId == null || t.Id > previousLegEndId))
            .ToList();
    }

    // Updates lifetime stats for every registered Player in the game once a
    // leg ends - both the winner and whoever they beat threw real darts and
    // should have their average reflect that. Guest matches leave
    // game.Players empty (they use in-match ids 1/2 that don't map to real
    // Player rows - see GuestSessionService), so this is a no-op there.
    private async Task UpdateStatsForLegAsync(Game game, int winnerId)
    {
        foreach (var player in game.Players)
        {
            await UpdatePlayerStatsAsync(player.Id, game, isWinner: player.Id == winnerId);
        }
    }

    private async Task UpdatePlayerStatsAsync(int playerId, Game game, bool isWinner)
    {
        var player = await _playerRepository.GetByIdAsync(playerId);
        if (player == null) return;

        // Checkout % is based on whether a double was actually hit, not on
        // who won: every turn that left this player with a legal shot at a
        // double finish (per DartGameRules.IsCheckoutPossible) is an
        // attempt, and it's only a hit if that exact turn ended the leg.
        // Only the leg that just ended is considered, since this method
        // re-runs each time any leg in this Game finishes.
        foreach (var turn in GetCurrentLegTurns(game, playerId))
        {
            if (_rules.IsCheckoutPossible(turn.ScoreBefore, game.Variant))
            {
                player.CheckoutAttempts++;
                if (!turn.IsBust && turn.ScoreAfter == 0) player.CheckoutHits++;
            }
        }

        if (player.CheckoutAttempts > 0)
        {
            player.CheckoutPercentage = Math.Round((decimal)player.CheckoutHits / player.CheckoutAttempts * 100m, 2);
        }

        if (isWinner)
        {
            // Calculates the highest checkout finish
            var winningTurn = game.Turns.OrderByDescending(t => t.ThrownAt).FirstOrDefault();
            if (winningTurn != null)
            {
                int finishScore = winningTurn.Scores.Sum(s => s.Points);
                if (finishScore > player.HighestFinish)
                {
                    player.HighestFinish = finishScore;
                }
            }
        }

        // Calculates the three-dart average for THIS game only
        var playerTurns = game.Turns.Where(t => t.PlayerId == playerId).ToList();
        int totalPoints = 0;
        int totalDarts = 0;

        foreach (var turn in playerTurns)
        {
            // Bust darts yield zero points but still count towards the total thrown darts
            if (!turn.IsBust) totalPoints += turn.Scores.Sum(s => s.Points);
            totalDarts += turn.Scores.Count;
        }

        if (totalDarts > 0)
        {
            decimal gameAverage = ((decimal)totalPoints / totalDarts) * 3;
            // Simple 50/50 mean with the previous lifetime average, not a
            // true running average weighted by darts thrown per game.
            player.ThreeDartAverage = player.ThreeDartAverage == 0
                ? Math.Round(gameAverage, 2)
                : Math.Round((player.ThreeDartAverage + gameAverage) / 2m, 2);
        }

        await _playerRepository.UpdateAsync(player);
        await _playerRepository.SaveChangesAsync();
    }
}