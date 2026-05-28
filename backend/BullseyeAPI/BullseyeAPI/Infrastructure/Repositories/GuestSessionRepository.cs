using BullseyeAPI.Application.Interfaces;
using BullseyeAPI.Domain.Entities;
using BullseyeAPI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BullseyeAPI.Infrastructure.Repositories;

public class GuestSessionRepository : IGuestSessionRepository
{
    private readonly AppDbContext _context;

    public GuestSessionRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<GuestSession?> GetByCodeAsync(string sessionCode)
    {
        return await _context.GuestSessions
            .Include(gs => gs.Games)
            .FirstOrDefaultAsync(gs => gs.SessionCode == sessionCode);
    }

    public async Task<bool> SessionCodeExistsAsync(string sessionCode)
    {
        return await _context.GuestSessions.AnyAsync(gs => gs.SessionCode == sessionCode);
    }

    public async Task AddAsync(GuestSession session)
    {
        await _context.GuestSessions.AddAsync(session);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}