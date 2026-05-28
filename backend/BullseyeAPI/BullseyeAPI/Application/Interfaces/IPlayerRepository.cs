using BullseyeAPI.Domain.Entities;

namespace BullseyeAPI.Application.Interfaces;

public interface IPlayerRepository
{
    Task <Player?> GetByIdAsync(int id);
    Task <Player?> GetByEmailAsync(string email);
    Task UpdateAsync(Player player);
    Task SaveChangesAsync();

}