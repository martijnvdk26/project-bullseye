import { Component, OnInit, OnDestroy, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { ActivatedRoute, Router } from '@angular/router';
import { environment } from '../../../../environments/environment';

// Imports the SignalR service and the separated child components
import { SignalRService } from '../../../shared/services/signalr';
import { Subscription } from 'rxjs';
import { NumpadComponent } from '../numpad/numpad'; // Fits your existing file name
import { PlayerCardComponent } from '../player-card/player-card';
import { StatsPanelComponent } from '../stats-panel/stats-panel';

@Component({
  selector: 'app-game-board',
  standalone: true,
  imports: [CommonModule, NumpadComponent, PlayerCardComponent, StatsPanelComponent],
  templateUrl: './game-board.html',
  styleUrl: './game-board.css',
})
export class GameBoardComponent implements OnInit, OnDestroy {
  private http = inject(HttpClient);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private signalRService = inject(SignalRService);
  private cdr = inject(ChangeDetectorRef);

  private readonly apiUrl = environment.apiUrl;
  private signalRSub!: Subscription;

  // Stores the active match identifiers and settings
  matchId!: number;
  variant: string = '501';
  sessionCode: string = '';
  targetSets: number = 1;
  targetLegs: number = 3;

  // Initializes players with match statistics
  player1 = {
    name: 'PLAYER 1',
    score: 501,
    sets: 0,
    legs: 0,
    isActive: true,
    average: 0,
    dartsThrown: 0,
    highestFinish: 0,
  };
  player2 = {
    name: 'PLAYER 2',
    score: 501,
    sets: 0,
    legs: 0,
    isActive: false,
    average: 0,
    dartsThrown: 0,
    highestFinish: 0,
  };

  ngOnInit() {
    this.matchId = Number(this.route.snapshot.paramMap.get('id'));

    // Retrieves session state and match rules passed from the router
    const navState = history.state;
    if (navState && navState.sessionCode) {
      this.sessionCode = navState.sessionCode;
      this.targetSets = navState.targetSets || 1;
      this.targetLegs = navState.targetLegs || 3;
    }

    // Fetches the most up-to-date player names directly from the database based on the PIN
    if (this.sessionCode) {
      this.http.get(`${this.apiUrl}/guest/${this.sessionCode}`).subscribe({
        next: (res: any) => {
          if (res.player1Name) this.player1.name = res.player1Name.toUpperCase();
          if (res.player2Name) this.player2.name = res.player2Name.toUpperCase();
          this.cdr.detectChanges();
        },
      });
    }

    // Initializes the SignalR WebSocket connection
    this.signalRService.startConnection(this.matchId);

    // Listens for backend updates and synchronizes the frontend data
    this.signalRSub = this.signalRService.scoreUpdated$.subscribe(() => {
      this.loadGameData();
    });

    this.loadGameData();
  }

  ngOnDestroy() {
    // Closes the WebSocket connection upon leaving the component
    this.signalRService.stopConnection();
    if (this.signalRSub) this.signalRSub.unsubscribe();
  }

  loadGameData() {
    // Retrieves the complete match state from the API
    this.http.get(`${this.apiUrl}/game/${this.matchId}`).subscribe({
      next: (game: any) => {
        this.variant = game.variant || game.Variant || '501';
        const startScore = parseInt(this.variant, 10);

        const turns = game.turns || game.Turns || [];
        const p1Turns = turns.filter((t: any) => (t.playerId || t.PlayerId) === 1);
        const p2Turns = turns.filter((t: any) => (t.playerId || t.PlayerId) === 2);

        // Safely calculates remaining scores by subtracting valid turn scores from the starting score
        const calculateRemaining = (playerTurns: any[]) => {
          const totalThrown = playerTurns.reduce((acc: number, turn: any) => {
            if (turn.isBust || turn.IsBust) return acc;
            const turnScore = (turn.scores || turn.Scores || []).reduce(
              (a: number, s: any) => a + (s.points ?? s.Points ?? s.score ?? s.Score ?? 0),
              0,
            );
            return acc + turnScore;
          }, 0);
          return startScore - totalThrown;
        };

        this.player1.score = calculateRemaining(p1Turns);
        this.player2.score = calculateRemaining(p2Turns);

        // Evaluates if a player has won the current leg
        this.checkLegWinCondition(startScore);

        // Corrects the math: A numpad turn equals 3 darts
        this.player1.dartsThrown = p1Turns.length * 3;
        const p1Scored = startScore - this.player1.score;
        this.player1.average = p1Turns.length > 0 ? p1Scored / p1Turns.length : 0;

        this.player2.dartsThrown = p2Turns.length * 3;
        const p2Scored = startScore - this.player2.score;
        this.player2.average = p2Turns.length > 0 ? p2Scored / p2Turns.length : 0;

        // Toggles active turn states
        this.player1.isActive = p1Turns.length === p2Turns.length;
        this.player2.isActive = p1Turns.length > p2Turns.length;

        this.cdr.detectChanges();
      },
      error: (err) => console.error('Failed to load match data:', err),
    });
  }

  checkLegWinCondition(startScore: number) {
    // Increments the leg counter if player 1 hits zero
    if (this.player1.score === 0) {
      this.player1.legs++;
      this.resetLegScores(startScore);
    }
    // Increments the leg counter if player 2 hits zero
    else if (this.player2.score === 0) {
      this.player2.legs++;
      this.resetLegScores(startScore);
    }

    // Evaluates if the target amount of legs has been reached to win a set
    if (this.player1.legs === this.targetLegs) {
      this.player1.sets++;
      this.player1.legs = 0;
      this.player2.legs = 0;
    } else if (this.player2.legs === this.targetLegs) {
      this.player2.sets++;
      this.player1.legs = 0;
      this.player2.legs = 0;
    }

    // Evaluates if the target amount of sets has been reached to end the match
    if (this.player1.sets === this.targetSets) {
      alert(`${this.player1.name} wins the match!`);
      this.leaveMatch();
    } else if (this.player2.sets === this.targetSets) {
      alert(`${this.player2.name} wins the match!`);
      this.leaveMatch();
    }
  }

  resetLegScores(startScore: number) {
    // Resets the local scores to prepare for the next leg
    this.player1.score = startScore;
    this.player2.score = startScore;
  }

  processSubmittedScore(score: number) {
    // Receives the validated score from the child numpad component and sends it to the API
    const activePlayerId = this.player1.isActive ? 1 : 2;

    this.http
      .post(`${this.apiUrl}/score`, {
        gameId: this.matchId,
        playerId: activePlayerId,
        score: score,
        points: score,
        totalPoints: score,
        segment: 'Manual',
      })
      .subscribe({
        next: () => {
          // Enforces an immediate data reload, completely bypassing the need for manual refreshes
          this.loadGameData();
        },
        error: (err) => alert('Failed to process score.'),
      });
  }

  leaveMatch() {
    // Navigates the user back to the lobby
    void this.router.navigate(['/guest-lobby']);
  }
}
