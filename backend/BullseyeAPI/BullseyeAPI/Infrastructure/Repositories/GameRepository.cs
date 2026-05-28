using BullseyeAPI.Application.Interfaces;
using BullseyeAPI.Domain.Entities;
using BullseyeAPI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BullseyeAPI.Infrastructure.Repositories;

public class GameRepository : IGameRepository
{
    private readonly AppDbContext _context;

    public GameRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Game?> GetByIdAsync(int id)
    {
        // Haal de game op INCLUSIEF alle gespeelde beurten en scores
        return await _context.Games
            .Include(g => g.Turns)
            .ThenInclude(t => t.Scores)
            .Include(g => g.Players)
            .FirstOrDefaultAsync(g => g.Id == id);
    }

    public async Task AddAsync(Game game)
    {
        await _context.Games.AddAsync(game);
    }

    public async Task UpdateAsync(Game game)
    {
        _context.Games.Update(game);
        await Task.CompletedTask; // Update is synchroon in EF Core, maar we houden de interface Task-based
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}