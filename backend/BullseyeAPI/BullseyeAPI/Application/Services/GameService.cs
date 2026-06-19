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

    public async Task<GameDto?> GetGameAsync(int gameId)
    {
        var game = await _gameRepository.GetByIdAsync(gameId);
        if (game == null) return null;

        // Maps the database turns to DTOs for the frontend
        var turnDtos = game.Turns.OrderBy(t => t.ThrownAt).Select(t => new TurnDto
        {
            PlayerId = t.PlayerId,
            ScoreBefore = t.ScoreBefore,
            IsBust = t.IsBust,
            ScoreAfter = t.IsBust ? t.ScoreBefore : t.ScoreBefore - t.Scores.Sum(s => s.Points),
            
            // Maps the corresponding darts to DTOs
            Scores = t.Scores.OrderBy(s => s.DartNumber).Select(s => new ScoreDto
            {
                Points = s.Points,
                Segment = s.Segment,
                DartNumber = s.DartNumber
            }).ToList()
        }).ToList();

        // Returns the properly formatted DTO to the controller
        return new GameDto
        {
            Id = game.Id,
            Variant = game.Variant,
            StartedAt = game.StartedAt,
            WinnerId = game.WinnerId,
            Turns = turnDtos
        };
    }

    public async Task<bool> SubmitScoreAsync(SubmitScoreRequest request)
    {
        var game = await _gameRepository.GetByIdAsync(request.GameId);
        if (game == null) return false;

        var currentTurn = game.Turns.LastOrDefault(t => t.PlayerId == request.PlayerId && t.Scores.Count < 3 && !t.IsBust);
        
        // Initializes a new turn if none exists, or if the previous one is completed
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

        // Evaluates a winning throw
        if (_rules.IsWinningThrow(scoreBeforeThisThrow, request.Points, request.IsDouble, game.Variant))
        {
            game.EndedAt = DateTime.UtcNow;
            game.WinnerId = request.PlayerId;
            currentTurn.Scores.Add(new Score { Points = request.Points, Segment = request.Segment, DartNumber = request.DartNumber });
            
            // Forces the database to explicitly record the remaining score as zero
            currentTurn.ScoreAfter = 0;
            
            await _gameRepository.UpdateAsync(game);
            await _gameRepository.SaveChangesAsync();
            await UpdatePlayerStatsAsync(request.PlayerId, game); 
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

        // Validates rules based on the total score of the manual turn
        if (request.TotalPoints == currentScore) 
        {
            game.EndedAt = DateTime.UtcNow;
            game.WinnerId = request.PlayerId;
            newTurn.Scores.Add(new Score { Points = request.TotalPoints, Segment = "Manual", DartNumber = 1 });
            
            if (newTurn.Scores.Count < 3) newTurn.Scores.Add(new Score { Points = 0, Segment = "-", DartNumber = 2 });
            if (newTurn.Scores.Count < 3) newTurn.Scores.Add(new Score { Points = 0, Segment = "-", DartNumber = 3 });
            
            newTurn.ScoreAfter = 0;
            
            game.Turns.Add(newTurn);
            await _gameRepository.UpdateAsync(game);
            await _gameRepository.SaveChangesAsync();
            
            await UpdatePlayerStatsAsync(request.PlayerId, game);
        }
        else if (request.TotalPoints > currentScore || (currentScore - request.TotalPoints) == 1) 
        {
            newTurn.IsBust = true;
            newTurn.Scores.Add(new Score { Points = request.TotalPoints, Segment = "Manual Bust", DartNumber = 1 });
            
            if (newTurn.Scores.Count < 3) newTurn.Scores.Add(new Score { Points = 0, Segment = "-", DartNumber = 2 });
            if (newTurn.Scores.Count < 3) newTurn.Scores.Add(new Score { Points = 0, Segment = "-", DartNumber = 3 });
            
            newTurn.ScoreAfter = newTurn.ScoreBefore;
            
            game.Turns.Add(newTurn);
            await _gameRepository.UpdateAsync(game);
            await _gameRepository.SaveChangesAsync();
        }
        else 
        {
            newTurn.Scores.Add(new Score { Points = request.TotalPoints, Segment = "Manual", DartNumber = 1 });
            
            if (newTurn.Scores.Count < 3) newTurn.Scores.Add(new Score { Points = 0, Segment = "-", DartNumber = 2 });
            if (newTurn.Scores.Count < 3) newTurn.Scores.Add(new Score { Points = 0, Segment = "-", DartNumber = 3 });
            
            newTurn.ScoreAfter = newTurn.ScoreBefore - request.TotalPoints;
            
            game.Turns.Add(newTurn);
            await _gameRepository.UpdateAsync(game);
            await _gameRepository.SaveChangesAsync();
        }

        // Broadcasts a live update signal to the Angular frontend
        await _hubContext.Clients.All.SendAsync("GameUpdated", game.Id);

        return true;
    }

    private async Task UpdatePlayerStatsAsync(int playerId, Game game)
    {
        var player = await _playerRepository.GetByIdAsync(playerId);
        if (player == null) return;

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

        // Calculates the three-dart average
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
            player.ThreeDartAverage = player.ThreeDartAverage == 0 
                ? Math.Round(gameAverage, 2) 
                : Math.Round((player.ThreeDartAverage + gameAverage) / 2m, 2);
        }

        await _playerRepository.UpdateAsync(player);
        await _playerRepository.SaveChangesAsync();
    }
}