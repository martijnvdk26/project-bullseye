using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace BullseyeAPI.Hubs;

public class GameHub : Hub
{
    // Deze methode wordt door Angular aangeroepen zodra het dartbord laadt
    public async Task JoinGame(string gameId)
    {
        // Plaatst de speler in een specifieke groep voor deze ene wedstrijd
        await Groups.AddToGroupAsync(Context.ConnectionId, gameId);
    }

    // Optioneel: Handig voor als een speler het scherm verlaat
    public async Task LeaveGame(string gameId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, gameId);
    }
}