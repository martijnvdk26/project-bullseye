import { Component, inject, ChangeDetectorRef, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { Router, RouterLink } from '@angular/router';
import { Subscription } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { AuthService } from '../../../shared/services/auth';
import { SignalRService } from '../../../shared/services/signalr';

// Pre-match lobby for the guest (no-login) flow: one player generates a PIN,
// the other joins with it, then either side can press "Start Game" once
// both names are visible - StartGameAsync on the backend is idempotent
// (returns the existing game if one was already created), so it's fine for
// both players to press it independently rather than needing one player to
// "host" and the other to wait for a signal.
//
// The match rules (variant/sets/legs) are chosen by the creator before the
// PIN is generated and stored on the GuestSession server-side - the joiner
// only ever displays them, never edits them, so both browsers always agree
// on the same rules instead of each having their own independent default.
//
// This component has no zone.js to lean on (see the comment on createSession
// below for why that matters), so every async HTTP response still has to
// manually trigger a re-render. The "opponent joined" notification, though,
// now comes from a SignalR push (see SignalRService.startLobbyConnection)
// instead of polling.
@Component({
  selector: 'app-guest-lobby',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './guest-lobby.html',
  styleUrl: './guest-lobby.css'
})
export class GuestLobbyComponent implements OnDestroy {
  private http = inject(HttpClient);
  private router = inject(Router);
  private cdr = inject(ChangeDetectorRef);
  private signalRService = inject(SignalRService);
  authService = inject(AuthService);
  private readonly apiUrl = environment.apiUrl;

  private lobbyJoinSub: Subscription | undefined;

  // Stores the entered PIN code and player name
  pinCode = '';
  playerName = '';

  // Stores the match rules the creator picks before generating the PIN
  selectedVariant = '501';
  targetLegs = 3;
  targetSets = 1;
  vsBot = false;
  botDifficulty = 'beginner';

  // Stores the session data returned by the API
  sessionInfo: any = null;

  // Guards against duplicate session/join requests from repeated clicks
  isSubmitting = false;

  createSession() {
    // Validates the player name before creating a lobby
    if (!this.playerName) {
      alert('Please enter your name first.');
      return;
    }

    if (this.isSubmitting) return;
    this.isSubmitting = true;

    // Submits the player name and chosen match rules to the backend to create a new session
    this.http.post(`${this.apiUrl}/guest`, {
      playerName: this.playerName,
      variant: this.selectedVariant,
      targetSets: this.targetSets,
      targetLegs: this.targetLegs,
      vsBot: this.vsBot,
      botDifficulty: this.botDifficulty,
    }).subscribe({
      next: (res: any) => {
        this.sessionInfo = res;
        this.isSubmitting = false;
        // This project has no zone.js dependency and no zoneless-change-
        // detection provider configured either (check package.json /
        // app.config.ts), so Angular only re-renders automatically right
        // after a DOM event it dispatched (like the click that called this
        // method) - NOT after an async HTTP response that resolves later,
        // which is exactly what's happening on this line. Without this call,
        // `sessionInfo` would be set correctly in memory but the PIN box
        // would never actually appear, making it look like nothing happened.
        this.cdr.detectChanges();
        // The creator is now waiting on the PIN screen, so listen for the
        // opponent joining via SignalR instead of polling for it
        this.listenForOpponent(res.sessionCode);
      },
      error: (err) => {
        console.error(err);
        this.isSubmitting = false;
        this.cdr.detectChanges();
      },
    });
  }

  private listenForOpponent(sessionCode: string) {
    this.lobbyJoinSub = this.signalRService.lobbyPlayerJoined$.subscribe((data) => {
      this.sessionInfo = { ...this.sessionInfo, player2Name: data.player2Name };
      this.cdr.detectChanges();
    });
    this.signalRService.startLobbyConnection(`guest-lobby-${sessionCode}`);
  }

  ngOnDestroy() {
    this.signalRService.stopConnection();
    this.lobbyJoinSub?.unsubscribe();
  }

  joinSession() {
    // Validates the presence of both a name and a PIN code
    if (!this.pinCode || this.pinCode.length !== 4 || !this.playerName) {
      alert('Please enter your name and a 4-digit PIN.');
      return;
    }

    if (this.isSubmitting) return;
    this.isSubmitting = true;

    // Appends the player name as a query parameter to register the opponent
    this.http.get(`${this.apiUrl}/guest/${this.pinCode.toUpperCase()}?playerName=${this.playerName}`).subscribe({
      next: (res: any) => {
        this.sessionInfo = res;
        this.isSubmitting = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        alert(
          err.status === 404
            ? "PIN not found. Double-check the code - if your opponent created their match in the Registered Lobby, you'll need to join from there instead."
            : 'Could not join: server offline or unreachable.',
        );
        this.isSubmitting = false;
        this.cdr.detectChanges();
      },
    });
  }

  startGame() {
    // Match rules already live on the session; the backend uses those directly
    this.http.post(`${this.apiUrl}/guest/${this.sessionInfo.sessionCode}/game`, {}).subscribe({
      next: (game: any) => {
        // Extracts the match ID from the response
        const matchId = game.id || game.Id;

        // Navigates the user to the game board and passes match rules via the Router state
        void this.router.navigate(['/game', matchId], {
          state: {
            p1Name: this.sessionInfo.player1Name || 'PLAYER 1',
            p2Name: this.sessionInfo.player2Name || 'PLAYER 2',
            sessionCode: this.sessionInfo.sessionCode,
            sessionType: 'guest',
            targetLegs: this.sessionInfo.targetLegs,
            targetSets: this.sessionInfo.targetSets
          }
        });
      },
      error: (err) => alert('Could not start the game.')
    });
  }
}
