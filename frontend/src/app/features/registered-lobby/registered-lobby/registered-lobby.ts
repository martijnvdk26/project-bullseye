import { Component, inject, ChangeDetectorRef, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { Router, RouterLink } from '@angular/router';
import { Subscription } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { AuthService } from '../../../shared/services/auth';
import { SignalRService } from '../../../shared/services/signalr';

// Account-bound counterpart to GuestLobbyComponent: same PIN create/join
// flow, but the player name comes from the logged-in account (AuthService)
// instead of a free-text field, and every request carries the JWT (via the
// auth interceptor) so the backend can attribute the match to real Player
// records rather than strings.
@Component({
  selector: 'app-registered-lobby',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './registered-lobby.html',
  styleUrl: './registered-lobby.css',
})
export class RegisteredLobbyComponent implements OnDestroy {
  private http = inject(HttpClient);
  private router = inject(Router);
  private cdr = inject(ChangeDetectorRef);
  private signalRService = inject(SignalRService);
  authService = inject(AuthService);
  private readonly apiUrl = environment.apiUrl;

  private lobbyJoinSub: Subscription | undefined;

  pinCode = '';

  // Stores the match rules the creator picks before generating the PIN
  selectedVariant = '501';
  targetLegs = 3;
  targetSets = 1;

  // Stores the session data returned by the API
  sessionInfo: any = null;

  isSubmitting = false;

  createSession() {
    if (this.isSubmitting) return;
    this.isSubmitting = true;

    this.http.post(`${this.apiUrl}/registered-session`, {
      variant: this.selectedVariant,
      targetSets: this.targetSets,
      targetLegs: this.targetLegs,
    }).subscribe({
      next: (res: any) => {
        this.sessionInfo = res;
        this.isSubmitting = false;
        this.cdr.detectChanges();
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
    this.signalRService.startLobbyConnection(`registered-lobby-${sessionCode}`);
  }

  ngOnDestroy() {
    this.signalRService.stopConnection();
    this.lobbyJoinSub?.unsubscribe();
  }

  joinSession() {
    if (!this.pinCode || this.pinCode.length !== 4) {
      alert('Please enter a 4-digit PIN.');
      return;
    }

    if (this.isSubmitting) return;
    this.isSubmitting = true;

    this.http.get(`${this.apiUrl}/registered-session/${this.pinCode.toUpperCase()}`).subscribe({
      next: (res: any) => {
        this.sessionInfo = res;
        this.isSubmitting = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        alert(
          err.status === 404
            ? "PIN not found. Double-check the code - if your opponent created their match in the Guest Lobby, you'll need to join from there instead."
            : 'Could not join: server offline or unreachable.',
        );
        this.isSubmitting = false;
        this.cdr.detectChanges();
      },
    });
  }

  startGame() {
    this.http.post(`${this.apiUrl}/registered-session/${this.sessionInfo.sessionCode}/game`, {}).subscribe({
      next: (game: any) => {
        const matchId = game.id || game.Id;

        void this.router.navigate(['/game', matchId], {
          state: {
            p1Name: this.sessionInfo.player1Name || 'PLAYER 1',
            p2Name: this.sessionInfo.player2Name || 'PLAYER 2',
            sessionCode: this.sessionInfo.sessionCode,
            sessionType: 'registered',
            targetLegs: this.sessionInfo.targetLegs,
            targetSets: this.sessionInfo.targetSets,
          },
        });
      },
      error: (err) => alert('Could not start the game.'),
    });
  }
}
