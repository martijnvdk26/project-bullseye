import { Component, OnInit, OnDestroy, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { ActivatedRoute, Router } from '@angular/router';
import { environment } from '../../../../environments/environment';

import { SignalRService } from '../../../shared/services/signalr';
import { GameService } from '../../../shared/services/game';
import { Subscription } from 'rxjs';
import { NumpadComponent } from '../numpad/numpad';
import { PlayerCardComponent } from '../player-card/player-card';
import { StatsPanelComponent } from '../stats-panel/stats-panel';

// Smart container for one live match. The backend is the source of truth
// for every Turn ever thrown (fetched in full on every loadGameData() call),
// but legs/sets/whose-turn-it-is are all derived client-side from that
// history by replaying it from scratch each time - see replayTurns() below.
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
  private gameService = inject(GameService);
  private cdr = inject(ChangeDetectorRef);

  private readonly apiUrl = environment.apiUrl;
  private signalRSub!: Subscription;

  // Set once the match-winner alert has fired for this component instance,
  // so a duplicate loadGameData() call for the same final turn (the
  // submitting browser's own SignalR echo) doesn't pop the alert/navigate
  // twice.
  private matchEndHandled = false;

  matchId!: number;
  variant: string = '501';
  sessionCode: string = '';
  sessionType: 'guest' | 'registered' = 'guest';
  targetSets: number = 1;
  targetLegs: number = 3;

  // Guest matches have no real Player rows to tie a turn to, so 1/2 are just
  // placeholders distinguishing "this match's player 1/2" - that's also all
  // the backend needs to derive scores/legs from turn history. Registered
  // matches, though, need turns tagged with the REAL Player id so the
  // backend's stats update (UpdatePlayerStatsAsync) can find the right
  // account; these get overwritten once the registered session loads below.
  player1Id = 1;
  player2Id = 2;

  // Camera mode has no Computer Vision behind it yet (Future Work) - it's
  // here so the score-input toggle itself is real, while manual entry stays
  // the only way to actually submit a score.
  inputMode: 'manual' | 'camera' = 'manual';

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

    const navState = history.state;
    if (navState && navState.sessionCode) {
      this.sessionCode = navState.sessionCode;
      this.sessionType = navState.sessionType === 'registered' ? 'registered' : 'guest';
      this.targetSets = navState.targetSets || 1;
      this.targetLegs = navState.targetLegs || 3;
    }

    if (this.sessionCode) {
      // sessionType decides which session table the names live in - guest
      // sessions are keyed by free-text names, registered ones by Player accounts
      const sessionUrl = this.sessionType === 'registered'
        ? `${this.apiUrl}/registered-session/${this.sessionCode}`
        : `${this.apiUrl}/guest/${this.sessionCode}`;

      this.http.get(sessionUrl).subscribe({
        next: (res: any) => {
          if (res.player1Name) this.player1.name = res.player1Name.toUpperCase();
          if (res.player2Name) this.player2.name = res.player2Name.toUpperCase();
          if (this.sessionType === 'registered') {
            if (res.player1Id) this.player1Id = res.player1Id;
            if (res.player2Id) this.player2Id = res.player2Id;
          }
          this.cdr.detectChanges();
        },
      });
    }

    // Lets this browser find out when the OTHER player (different
    // browser/tab) has thrown, since their POST never passes through here.
    this.signalRService.startConnection(this.matchId);

    this.signalRSub = this.signalRService.scoreUpdated$.subscribe(() => {
      this.loadGameData();
    });

    this.loadGameData();
  }

  ngOnDestroy() {
    this.signalRService.stopConnection();
    if (this.signalRSub) this.signalRSub.unsubscribe();
  }

  loadGameData() {
    this.http.get(`${this.apiUrl}/game/${this.matchId}`).subscribe({
      next: (game: any) => {
        this.variant = game.variant || game.Variant || '501';
        const startScore = parseInt(this.variant, 10);
        const turns = game.turns || game.Turns || [];

        const state = this.replayTurns(turns, startScore);

        this.player1.score = state.p1Score;
        this.player2.score = state.p2Score;
        this.player1.dartsThrown = state.p1DartsThisLeg;
        this.player2.dartsThrown = state.p2DartsThisLeg;
        this.player1.average = state.p1Turns > 0 ? state.p1Scored / state.p1Turns : 0;
        this.player2.average = state.p2Turns > 0 ? state.p2Scored / state.p2Turns : 0;
        this.player1.legs = state.player1Legs;
        this.player2.legs = state.player2Legs;
        this.player1.sets = state.player1Sets;
        this.player2.sets = state.player2Sets;
        this.player1.isActive = state.player1IsActive;
        this.player2.isActive = state.player2IsActive;

        if (state.matchWinnerId !== null && !this.matchEndHandled) {
          this.matchEndHandled = true;
          const winnerName = state.matchWinnerId === this.player1Id ? this.player1.name : this.player2.name;
          alert(`${winnerName} wins the match!`);
          this.leaveMatch();
        }

        // No zone.js, so nothing re-renders unless asked explicitly.
        this.cdr.detectChanges();
      },
      error: (err) => console.error('Failed to load match data:', err),
    });
  }

  // legs/sets/targetLegs/targetSets are never sent to or read from the
  // backend - the backend only stores the raw Turn history. So every
  // browser watching the same match must derive identical leg/set/starter
  // state from that same history, every time it loads. Rather than
  // incrementally mutating counters across loadGameData() calls (fragile -
  // a page refresh or remount mid-match would reset those counters to zero
  // and break the leg-starter alternation, e.g. the dartbot suddenly
  // "forgetting" it's supposed to start the even legs), this replays the
  // entire turn history from scratch on every call using Turn.Id as the
  // authoritative chronological order, so the result only ever depends on
  // the backend data, never on this component's prior in-memory state.
  private replayTurns(turns: any[], startScore: number) {
    const sortedTurns = [...turns].sort((a, b) => (a.id ?? a.Id) - (b.id ?? b.Id));

    let p1Score = startScore;
    let p2Score = startScore;
    let p1DartsThisLeg = 0;
    let p2DartsThisLeg = 0;
    let p1TurnsThisLeg = 0;
    let p2TurnsThisLeg = 0;
    let p1Turns = 0;
    let p2Turns = 0;
    let p1Scored = 0;
    let p2Scored = 0;
    let player1Legs = 0;
    let player2Legs = 0;
    let player1Sets = 0;
    let player2Sets = 0;
    let legsPlayedTotal = 0;
    let matchWinnerId: number | null = null;

    for (const turn of sortedTurns) {
      const playerId = turn.playerId ?? turn.PlayerId;
      const isBust = turn.isBust ?? turn.IsBust;
      const turnScore = (turn.scores || turn.Scores || []).reduce(
        (a: number, s: any) => a + (s.points ?? s.Points ?? s.score ?? s.Score ?? 0),
        0,
      );

      if (playerId === this.player1Id) {
        p1Turns++;
        p1DartsThisLeg += 3;
        p1TurnsThisLeg++;
        if (!isBust) {
          p1Scored += turnScore;
          p1Score -= turnScore;
        }
      } else if (playerId === this.player2Id) {
        p2Turns++;
        p2DartsThisLeg += 3;
        p2TurnsThisLeg++;
        if (!isBust) {
          p2Scored += turnScore;
          p2Score -= turnScore;
        }
      }

      if (p1Score === 0 || p2Score === 0) {
        if (p1Score === 0) player1Legs++;
        else player2Legs++;
        legsPlayedTotal++;

        p1Score = startScore;
        p2Score = startScore;
        p1DartsThisLeg = 0;
        p2DartsThisLeg = 0;
        p1TurnsThisLeg = 0;
        p2TurnsThisLeg = 0;

        if (player1Legs === this.targetLegs) {
          player1Sets++;
          player1Legs = 0;
          player2Legs = 0;
        } else if (player2Legs === this.targetLegs) {
          player2Sets++;
          player1Legs = 0;
          player2Legs = 0;
        }

        if (player1Sets === this.targetSets) matchWinnerId = this.player1Id;
        else if (player2Sets === this.targetSets) matchWinnerId = this.player2Id;
      }
    }

    // Whoever should start THIS leg (legStarter, alternating strictly by
    // leg number across the whole match) is active once they and the other
    // player have thrown the same number of turns since the leg began; the
    // other player is active after the starter has thrown one more.
    const legStarter = legsPlayedTotal % 2 === 0 ? this.player1Id : this.player2Id;
    const player1IsActive =
      legStarter === this.player1Id ? p1TurnsThisLeg === p2TurnsThisLeg : p2TurnsThisLeg > p1TurnsThisLeg;
    const player2IsActive =
      legStarter === this.player2Id ? p1TurnsThisLeg === p2TurnsThisLeg : p1TurnsThisLeg > p2TurnsThisLeg;

    return {
      p1Score,
      p2Score,
      p1DartsThisLeg,
      p2DartsThisLeg,
      p1Turns,
      p2Turns,
      p1Scored,
      p2Scored,
      player1Legs,
      player2Legs,
      player1Sets,
      player2Sets,
      player1IsActive,
      player2IsActive,
      matchWinnerId,
    };
  }

  processSubmittedScore(score: number) {
    const activePlayerId = this.player1.isActive ? this.player1Id : this.player2Id;

    // Always uses the whole-turn endpoint so each visit starts a fresh Turn
    // instead of merging into the player's previous one (see GameService.cs).
    this.gameService
      .submitManualTurn({
        gameId: this.matchId,
        playerId: activePlayerId,
        totalPoints: score,
      })
      .subscribe({
        next: () => this.loadGameData(),
        error: (err) => alert('Failed to process score.'),
      });
  }

  leaveMatch() {
    void this.router.navigate([this.sessionType === 'registered' ? '/dashboard' : '/guest-lobby']);
  }
}
