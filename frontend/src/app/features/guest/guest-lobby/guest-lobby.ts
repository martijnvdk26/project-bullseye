import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { Router, RouterLink } from '@angular/router';
import { environment } from '../../../../environments/environment';

@Component({
  selector: 'app-guest-lobby',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './guest-lobby.html',
  styleUrl: './guest-lobby.css'
})
export class GuestLobbyComponent {
  private http = inject(HttpClient);
  private router = inject(Router);
  private readonly apiUrl = environment.apiUrl;

  // Stores the entered PIN code and player name
  pinCode = '';
  playerName = '';

  // Stores the selected match rules
  selectedVariant = '501';
  targetLegs = 3;
  targetSets = 1;

  // Stores the session data returned by the API
  sessionInfo: any = null;

  createSession() {
    // Validates the player name before creating a lobby
    if (!this.playerName) {
      alert('Please enter your name first.');
      return;
    }

    // Submits the player name to the backend to create a new session
    this.http.post(`${this.apiUrl}/guest`, { playerName: this.playerName }).subscribe({
      next: (res: any) => this.sessionInfo = res,
      error: (err) => console.error(err)
    });
  }

  joinSession() {
    // Validates the presence of both a name and a PIN code
    if (!this.pinCode || this.pinCode.length !== 4 || !this.playerName) {
      alert('Please enter your name and a 4-digit PIN.');
      return;
    }

    // Appends the player name as a query parameter to register the opponent
    this.http.get(`${this.apiUrl}/guest/${this.pinCode.toUpperCase()}?playerName=${this.playerName}`).subscribe({
      next: (res: any) => this.sessionInfo = res,
      error: (err) => alert('Invalid PIN or server offline.')
    });
  }

  startGame() {
    // Submits the selected game variant to the backend to initialize the match
    this.http.post(`${this.apiUrl}/guest/${this.sessionInfo.sessionCode}/game`, { variant: this.selectedVariant }).subscribe({
      next: (game: any) => {
        // Extracts the match ID from the response
        const matchId = game.id || game.Id;

        // Navigates the user to the game board and passes match rules via the Router state
        void this.router.navigate(['/game', matchId], {
          state: {
            p1Name: this.sessionInfo.player1Name || 'PLAYER 1',
            p2Name: this.sessionInfo.player2Name || 'PLAYER 2',
            sessionCode: this.sessionInfo.sessionCode,
            targetLegs: this.targetLegs,
            targetSets: this.targetSets
          }
        });
      },
      error: (err) => alert('Could not start the game.')
    });
  }
}
