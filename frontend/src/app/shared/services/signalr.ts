import { Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { Subject } from 'rxjs';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root',
})
export class SignalRService {
  private hubConnection: signalR.HubConnection | undefined;

  // A Subject is an Observable that we can push new values into.
  // The GameBoardComponent will subscribe to this to know when the score changes.
  public scoreUpdated$ = new Subject<{
    playerId: number;
    score: number;
    pointsRemaining: number;
  }>();

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

    // Set up the listeners for incoming messages from the server
    this.addScoreListener();
  }

  private joinGameGroup(gameId: number) {
    // 'JoinGame' must match the exact method name inside your GameHub.cs file
    this.hubConnection
      ?.invoke('JoinGame', gameId.toString())
      .catch((err) => console.error('Could not join game group:', err));
  }

  private addScoreListener() {
    // 'ReceiveScoreUpdate' must match the exact string sent by your C# backend
    // e.g., await Clients.Group(gameId).SendAsync("ReceiveScoreUpdate", playerId, score, remaining);
    this.hubConnection?.on(
      'ReceiveScoreUpdate',
      (playerId: number, score: number, pointsRemaining: number) => {
        // Broadcast the incoming data to any Angular components that are listening
        this.scoreUpdated$.next({ playerId, score, pointsRemaining });
      },
    );
  }

  public stopConnection() {
    if (this.hubConnection) {
      this.hubConnection.stop();
    }
  }
}
