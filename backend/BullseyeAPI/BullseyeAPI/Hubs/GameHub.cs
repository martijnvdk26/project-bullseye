using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace BullseyeAPI.Hubs;

// SignalR hub: the live WebSocket connection between the Angular game-board
// and the backend. GameService pushes "GameUpdated" events whenever a turn
// is recorded.
//
// NOTE: GameService broadcasts with Clients.All, not Clients.Group(gameId),
// so JoinGame/LeaveGame's per-match groups aren't actually used yet - every
// open game board refreshes on any match's score, not just its own.
public class GameHub : Hub
{
    public async Task JoinGame(string gameId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, gameId);
    }

    public async Task LeaveGame(string gameId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, gameId);
    }

    // Lobby groups (guest or registered) use this before a Game even exists,
    // so the creator can be told the instant someone joins their PIN instead
    // of polling for it. lobbyKey is an opaque string the caller builds
    // (e.g. "guest-lobby-1234" / "registered-lobby-1234") so the two lobby
    // kinds can never collide even if they generate the same PIN.
    public async Task JoinLobby(string lobbyKey)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, lobbyKey);
    }
}