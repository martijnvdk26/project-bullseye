using BullseyeAPI.Domain.Entities;

namespace BullseyeAPI.Application.Interfaces;

public interface IGameRepository
{
    Task<Game?> GetByIdAsync(int id);
    Task AddAsync(Game game);
    Task UpdateAsync(Game game);
    Task SaveChangesAsync();
}