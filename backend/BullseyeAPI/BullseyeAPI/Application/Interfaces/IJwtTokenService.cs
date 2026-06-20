using BullseyeAPI.Application.DTOs;

namespace BullseyeAPI.Application.Interfaces;

public interface IJwtTokenService
{
    string GenerateToken(PlayerDto player);
}
