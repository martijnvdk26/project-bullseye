import { Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { Subject } from 'rxjs';
import { environment } from '../../../environments/environment';

// Thin wrapper around the @microsoft/signalr client. GameBoardComponent uses
// this to find out when the OTHER player's browser has submitted a score, so
// its own view refreshes without the player having to reload the page.
// The submitting browser doesn't actually need this at all - it calls
// loadGameData() directly off its own HTTP response - this is purely for the
// other, passive browser watching the same match.
@Injectable({
  providedIn: 'root',
})
export class SignalRService {
  private hubConnection: signalR.HubConnection | undefined;

  // A Subject is an Observable that we can push new values into.
  // The GameBoardComponent will subscribe to this to know when the score changes.
  // Carries the updated game's id, mirroring the payload the backend actually sends.
  public scoreUpdated$ = new Subject<number>();

  // GuestLobbyComponent/RegisteredLobbyComponent subscribe to this to find out
  // the instant the opponent joins their PIN, instead of polling for it.
  public lobbyPlayerJoined$ = new Subject<{ player2Name: string }>();

  public startConnection(gameId: number) {
    // Build the connection to the SignalR hub defined in your C# Program.cs
    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(environment.hubUrl)
      .withAutomaticReconnect()
      .build();

    // Start the connection
    this.hubConnection
      .start()
      .then(() => {
        console.log('SignalR Connection established.');
        // Once connected, tell the backend which specific match room to join
        this.joinGameGroup(gameId);
      })
      .catch((err) => console.error('Error starting SignalR connection: ', err));

    // Registering the listener doesn't depend on the connection having
    // finished starting - .on() just attaches a handler for when the event
    // eventually arrives, so this can safely run before .start() resolves
    this.addScoreListener();
  }

  private joinGameGroup(gameId: number) {
    // 'JoinGame' must match the exact method name inside your GameHub.cs file.
    // NOTE: this puts the connection into a per-game SignalR group on the
    // server, but GameService currently broadcasts "GameUpdated" to
    // Clients.All rather than Clients.Group(gameId) - so joining the group
    // has no real effect yet. It's the hook a future, multi-table version of
    // this app would use to scope updates to just this match.
    this.hubConnection
      ?.invoke('JoinGame', gameId.toString())
      .catch((err) => console.error('Could not join game group:', err));
  }

  private addScoreListener() {
    // 'GameUpdated' must match the exact string sent by the C# backend, see
    // GameService.cs: await _hubContext.Clients.All.SendAsync("GameUpdated", game.Id);
    this.hubConnection?.on('GameUpdated', (gameId: number) => {
      // Broadcast the incoming data to any Angular components that are listening
      this.scoreUpdated$.next(gameId);
    });
  }

  // Used by the lobby (guest or registered) before a Game even exists, so the
  // session creator finds out the opponent joined via a push instead of
  // polling. lobbyKey must match the group name the backend broadcasts to,
  // e.g. "guest-lobby-1234" or "registered-lobby-1234".
  public startLobbyConnection(lobbyKey: string) {
    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(environment.hubUrl)
      .withAutomaticReconnect()
      .build();

    this.hubConnection.on('PlayerJoined', (data: { player2Name: string }) => {
      this.lobbyPlayerJoined$.next(data);
    });

    this.hubConnection
      .start()
      .then(() => {
        this.hubConnection?.invoke('JoinLobby', lobbyKey).catch((err) =>
          console.error('Could not join lobby group:', err),
        );
      })
      .catch((err) => console.error('Error starting SignalR connection: ', err));
  }

  public stopConnection() {
    if (this.hubConnection) {
      this.hubConnection.stop();
    }
  }
}
