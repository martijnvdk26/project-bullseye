using BullseyeAPI.Application.DTOs;
using BullseyeAPI.Application.Interfaces;
using BullseyeAPI.Domain.Entities;
using BullseyeAPI.Domain.Rules;
using Microsoft.AspNetCore.SignalR;
using BullseyeAPI.Hubs;

namespace BullseyeAPI.Application.Services;

public class GameService : IGameService
{
    private readonly IGameRepository _gameRepository;
    private readonly DartGameRules _rules;
    private readonly IPlayerRepository _playerRepository;
    private readonly IHubContext<GameHub> _hubContext;

    public GameService(
        IGameRepository gameRepository, 
        DartGameRules rules, 
        IPlayerRepository playerRepository,
        IHubContext<GameHub> hubContext)
    {
        _gameRepository = gameRepository;
        _rules = rules;
        _playerRepository = playerRepository;
        _hubContext = hubContext;
    }

    // --- 0. WEDSTRIJD OPHALEN (Voor de frontend) ---
    public async Task<GameDto?> GetGameAsync(int gameId)
    {
        var game = await _gameRepository.GetByIdAsync(gameId);
        if (game == null) return null;

        // Zet de database beurten (Turns) om naar DTO's voor de frontend
        var turnDtos = game.Turns.OrderBy(t => t.ThrownAt).Select(t => new TurnDto
        {
            PlayerId = t.PlayerId,
            ScoreBefore = t.ScoreBefore,
            IsBust = t.IsBust,
            ScoreAfter = t.IsBust ? t.ScoreBefore : t.ScoreBefore - t.Scores.Sum(s => s.Points),
            
            // Zet de bijbehorende pijlen (Scores) om naar DTO's
            Scores = t.Scores.OrderBy(s => s.DartNumber).Select(s => new ScoreDto
            {
                Points = s.Points,
                Segment = s.Segment,
                DartNumber = s.DartNumber
            }).ToList()
        }).ToList();

        // Geef het perfect gevormde DTO terug aan de Controller
        return new GameDto
        {
            Id = game.Id,
            Variant = game.Variant,
            StartedAt = game.StartedAt,
            WinnerId = game.WinnerId,
            Turns = turnDtos
        };
    }

    // --- 1. INVOER PIJL-VOOR-PIJL (Voor de AI Camera of losse invoer) ---
    public async Task<bool> SubmitScoreAsync(SubmitScoreRequest request)
    {
        var game = await _gameRepository.GetByIdAsync(request.GameId);
        if (game == null) return false;

        var currentTurn = game.Turns.LastOrDefault(t => t.PlayerId == request.PlayerId && t.Scores.Count < 3 && !t.IsBust);
        
        // Nieuwe beurt aanmaken als er nog geen is, of de vorige vol is
        if (currentTurn == null)
        {
            var lastTurn = game.Turns.Where(t => t.PlayerId == request.PlayerId).LastOrDefault();
            int startScore = game.Variant == "501" ? 501 : 301;
            int scoreBefore = lastTurn == null ? startScore : (lastTurn.IsBust ? lastTurn.ScoreBefore : lastTurn.ScoreBefore - lastTurn.Scores.Sum(s => s.Points));

            currentTurn = new Turn
            {
                PlayerId = request.PlayerId,
                ScoreBefore = scoreBefore,
                ThrownAt = DateTime.UtcNow,
                Scores = new List<Score>()
            };
            game.Turns.Add(currentTurn);
        }

        // Foutafhandeling: Controleer of deze pijl al gegooid is in deze beurt
        if (currentTurn.Scores.Any(s => s.DartNumber == request.DartNumber))
        {
            throw new ArgumentException($"Pijl nummer {request.DartNumber} is al gegooid in deze beurt!");
        }

        int currentTurnTotal = currentTurn.Scores.Sum(s => s.Points);
        int scoreVoorDezeWorp = currentTurn.ScoreBefore - currentTurnTotal;

        // Check voor winst
        if (_rules.IsWinningThrow(scoreVoorDezeWorp, request.Points, request.IsDouble, game.Variant))
        {
            game.EndedAt = DateTime.UtcNow;
            game.WinnerId = request.PlayerId;
            currentTurn.Scores.Add(new Score { Points = request.Points, Segment = request.Segment, DartNumber = request.DartNumber });
            
            await _gameRepository.UpdateAsync(game);
            await _gameRepository.SaveChangesAsync();
            await UpdatePlayerStatsAsync(request.PlayerId, game); // Update statistieken!
        }
        // Check voor bust
        else if (!_rules.IsValidScore(scoreVoorDezeWorp, request.Points, request.IsDouble, game.Variant))
        {
            currentTurn.IsBust = true;
            currentTurn.Scores.Add(new Score { Points = request.Points, Segment = request.Segment, DartNumber = request.DartNumber });
            
            await _gameRepository.UpdateAsync(game);
            await _gameRepository.SaveChangesAsync();
        }
        // Geldige worp
        else
        {
            currentTurn.Scores.Add(new Score { Points = request.Points, Segment = request.Segment, DartNumber = request.DartNumber });
            await _gameRepository.UpdateAsync(game);
            await _gameRepository.SaveChangesAsync();
        }

        // Live seintje naar de Angular frontend
        await _hubContext.Clients.All.SendAsync("GameUpdated", game.Id);

        return true;
    }

    // --- 2. INVOER COMPLETE BEURT (Voor handmatig typen op de bank) ---
    public async Task<bool> SubmitManualTurnAsync(SubmitTurnRequest request)
    {
        var game = await _gameRepository.GetByIdAsync(request.GameId);
        if (game == null) return false;

        var lastTurn = game.Turns.Where(t => t.PlayerId == request.PlayerId).LastOrDefault();
        int startScore = game.Variant == "501" ? 501 : 301;
        int currentScore = lastTurn == null ? startScore : (lastTurn.IsBust ? lastTurn.ScoreBefore : lastTurn.ScoreBefore - lastTurn.Scores.Sum(s => s.Points));

        var newTurn = new Turn
        {
            PlayerId = request.PlayerId,
            ScoreBefore = currentScore,
            ThrownAt = DateTime.UtcNow,
            Scores = new List<Score>()
        };

        // Regels controleren op basis van de totale score
        if (request.TotalPoints == currentScore) // Winst!
        {
            game.EndedAt = DateTime.UtcNow;
            game.WinnerId = request.PlayerId;
            newTurn.Scores.Add(new Score { Points = request.TotalPoints, Segment = "Manual", DartNumber = 1 });
            
            // Trucje: Vul de beurt af met dummy-pijlen zodat de rest van de logica klopt
            if (newTurn.Scores.Count < 3) newTurn.Scores.Add(new Score { Points = 0, Segment = "-", DartNumber = 2 });
            if (newTurn.Scores.Count < 3) newTurn.Scores.Add(new Score { Points = 0, Segment = "-", DartNumber = 3 });
            
            game.Turns.Add(newTurn);
            await _gameRepository.UpdateAsync(game);
            await _gameRepository.SaveChangesAsync();
            
            await UpdatePlayerStatsAsync(request.PlayerId, game); // Update statistieken!
        }
        else if (request.TotalPoints > currentScore || (currentScore - request.TotalPoints) == 1) // Bust!
        {
            newTurn.IsBust = true;
            newTurn.Scores.Add(new Score { Points = request.TotalPoints, Segment = "Manual Bust", DartNumber = 1 });
            
            if (newTurn.Scores.Count < 3) newTurn.Scores.Add(new Score { Points = 0, Segment = "-", DartNumber = 2 });
            if (newTurn.Scores.Count < 3) newTurn.Scores.Add(new Score { Points = 0, Segment = "-", DartNumber = 3 });
            
            game.Turns.Add(newTurn);
            await _gameRepository.UpdateAsync(game);
            await _gameRepository.SaveChangesAsync();
        }
        else // Geldige beurt
        {
            newTurn.Scores.Add(new Score { Points = request.TotalPoints, Segment = "Manual", DartNumber = 1 });
            
            if (newTurn.Scores.Count < 3) newTurn.Scores.Add(new Score { Points = 0, Segment = "-", DartNumber = 2 });
            if (newTurn.Scores.Count < 3) newTurn.Scores.Add(new Score { Points = 0, Segment = "-", DartNumber = 3 });
            
            game.Turns.Add(newTurn);
            await _gameRepository.UpdateAsync(game);
            await _gameRepository.SaveChangesAsync();
        }

        // Live seintje naar de Angular frontend
        await _hubContext.Clients.All.SendAsync("GameUpdated", game.Id);

        return true;
    }

    // --- 3. REKENMACHINE SPELERSTATISTIEKEN ---
    private async Task UpdatePlayerStatsAsync(int playerId, Game game)
    {
        var player = await _playerRepository.GetByIdAsync(playerId);
        if (player == null) return;

        // 1. Highest Finish berekenen
        var winningTurn = game.Turns.OrderByDescending(t => t.ThrownAt).FirstOrDefault();
        if (winningTurn != null)
        {
            int finishScore = winningTurn.Scores.Sum(s => s.Points);
            if (finishScore > player.HighestFinish)
            {
                player.HighestFinish = finishScore;
            }
        }

        // 2. Three-Dart Average berekenen
        var playerTurns = game.Turns.Where(t => t.PlayerId == playerId).ToList();
        int totalPoints = 0;
        int totalDarts = 0;

        foreach (var turn in playerTurns)
        {
            // Bust pijlen leveren geen punten op, maar tellen wél mee als gegooid
            if (!turn.IsBust) totalPoints += turn.Scores.Sum(s => s.Points);
            totalDarts += turn.Scores.Count; 
        }

        if (totalDarts > 0)
        {
            decimal gameAverage = ((decimal)totalPoints / totalDarts) * 3;
            player.ThreeDartAverage = player.ThreeDartAverage == 0 
                ? Math.Round(gameAverage, 2) 
                : Math.Round((player.ThreeDartAverage + gameAverage) / 2m, 2);
        }

        await _playerRepository.UpdateAsync(player);
        await _playerRepository.SaveChangesAsync();
    }
}