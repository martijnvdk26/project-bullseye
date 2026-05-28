using BullseyeAPI.Domain.Entities;

namespace BullseyeAPI.Application.Interfaces;

public interface IGuestSessionRepository
{
    Task<GuestSession?> GetByCodeAsync(string sessionCode);
    Task<bool> SessionCodeExistsAsync(string sessionCode);
    Task AddAsync(GuestSession session);
    Task SaveChangesAsync();
}