using BullseyeAPI.Application.Interfaces;
using BullseyeAPI.Domain.Entities;
using BullseyeAPI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BullseyeAPI.Infrastructure.Repositories;

public class PlayerRepository : IPlayerRepository
{
    private readonly AppDbContext _context;

    public PlayerRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Player?> GetByIdAsync(int id)
    {
        return await _context.Players.FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<Player?> GetByEmailAsync(string email)
    {
        // Zoek de speler op basis van e-mail en negeer hoofdletters
        return await _context.Players.FirstOrDefaultAsync(p => p.Email == email.ToLower());
    }

    public async Task<Player?> GetByVerificationTokenAsync(string token)
    {
        return await _context.Players.FirstOrDefaultAsync(p => p.VerificationToken == token);
    }

    public async Task AddAsync(Player player)
    {
        await _context.Players.AddAsync(player);
    }

    public async Task UpdateAsync(Player player)
    {
        _context.Players.Update(player);
        await Task.CompletedTask;
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}