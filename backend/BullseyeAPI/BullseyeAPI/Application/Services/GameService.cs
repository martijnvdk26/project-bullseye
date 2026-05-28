using BullseyeAPI.Application.DTOs;
using BullseyeAPI.Application.Interfaces;
using BullseyeAPI.Domain.Entities;
using BullseyeAPI.Domain.Rules;

namespace BullseyeAPI.Application.Services;

public class GameService : IGameService
{
    private readonly IGameRepository _gameRepository;
    private readonly DartGameRules _rules;

    public GameService(IGameRepository gameRepository, DartGameRules rules)
    {
        _gameRepository = gameRepository;
        _rules = rules;
    }
    
    public async Task<GameDto?> GetGameAsync(int gameId)
    {
        var game = await _gameRepository.GetByIdAsync(gameId);
        if (game == null) return null;

        // Zet de database beurten (Turns) om naar DTO's
        var turnDtos = game.Turns.OrderBy(t => t.ThrownAt).Select(t => new TurnDto
        {
            PlayerId = t.PlayerId,
            ScoreBefore = t.ScoreBefore,
            IsBust = t.IsBust,
            // Als de speler bust gegooid heeft, blijft de score hetzelfde. Anders trekken we de punten ervan af.
            ScoreAfter = t.IsBust ? t.ScoreBefore : t.ScoreBefore - t.Scores.Sum(s => s.Points),
            
            // Zet de bijbehorende pijlen (Scores) om naar DTO's
            Scores = t.Scores.OrderBy(s => s.DartNumber).Select(s => new ScoreDto
            {
                Points = s.Points,
                Segment = s.Segment,
                DartNumber = s.DartNumber
            }).ToList()
        }).ToList();

        // Geef de complete game terug
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
        if (game == null || game.EndedAt != null) return false; // Game bestaat niet of is al afgelopen

        // Zoek de huidige actieve beurt (Turn) voor deze speler, of maak een nieuwe aan
        var currentTurn = game.Turns
            .Where(t => t.PlayerId == request.PlayerId)
            .OrderByDescending(t => t.ThrownAt)
            .FirstOrDefault();

        // Als er geen beurt is, of de vorige beurt heeft al 3 pijlen, start een nieuwe beurt
        if (currentTurn == null || currentTurn.Scores.Count >= 3 || currentTurn.IsBust)
        {
            // Bepaal de huidige score (Startscore - alle vorige geldige punten)
            int currentScore = int.Parse(game.Variant); 
            var previousTurns = game.Turns.Where(t => t.PlayerId == request.PlayerId && !t.IsBust);
            
            foreach(var turn in previousTurns) {
                currentScore -= turn.Scores.Sum(s => s.Points);
            }

            currentTurn = new Turn
            {
                GameId = game.Id,
                PlayerId = request.PlayerId,
                ScoreBefore = currentScore,
                ThrownAt = DateTime.UtcNow
            };
            game.Turns.Add(currentTurn);
        }

        // --- SPELREGELS TOEPASSEN ---
        int currentTurnTotal = currentTurn.Scores.Sum(s => s.Points);
        int scoreVoorDezeWorp = currentTurn.ScoreBefore - currentTurnTotal;

        // Check voor winst via jouw regels
        if (_rules.IsWinningThrow(scoreVoorDezeWorp, request.Points, request.IsDouble, game.Variant))
        {
            game.EndedAt = DateTime.UtcNow;
            game.WinnerId = request.PlayerId;
        }
        // Check voor bust via jouw regels
        else if (!_rules.IsValidScore(scoreVoorDezeWorp, request.Points, request.IsDouble, game.Variant))
        {
            currentTurn.IsBust = true;
        }

        // Voeg de daadwerkelijke pijl toe
        var score = new Score
        {
            Points = request.Points,
            Segment = request.Segment,
            DartNumber = request.DartNumber
        };
        currentTurn.Scores.Add(score);

        // Bijwerken en opslaan
        await _gameRepository.UpdateAsync(game);
        await _gameRepository.SaveChangesAsync();

        return true;
    }
}