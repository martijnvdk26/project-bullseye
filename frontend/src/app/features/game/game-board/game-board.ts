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
// history - see loadGameData() and checkLegWinCondition() below.
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

  // Index into p1Turns/p2Turns marking where the CURRENT leg starts, so
  // score/dartsThrown can be computed from just this leg while `average`
  // still uses the full, unsliced turn history.
  private p1LegStartIndex = 0;
  private p2LegStartIndex = 0;

  // Total turns (both players combined) already processed by
  // checkLegWinCondition - see the comment there.
  private lastProcessedTurnCount = 0;

  // Legs completed so far across the whole match (never reset by a set
  // rollover, unlike player1.legs/player2.legs). Determines who SHOULD
  // start the current leg - player 1 on even counts (legs 1, 3, 5...),
  // player 2 on odd counts (legs 2, 4, 6...) - independent of who actually
  // won the previous leg. See checkLegWinCondition for why this can't be
  // derived from raw turn counts alone.
  private legsPlayedTotal = 0;

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
        const p1Turns = turns.filter((t: any) => (t.playerId || t.PlayerId) === this.player1Id);
        const p2Turns = turns.filter((t: any) => (t.playerId || t.PlayerId) === this.player2Id);

        // Sums points from valid (non-bust) turns
        const calculateScored = (playerTurns: any[]) =>
          playerTurns.reduce((acc: number, turn: any) => {
            if (turn.isBust || turn.IsBust) return acc;
            const turnScore = (turn.scores || turn.Scores || []).reduce(
              (a: number, s: any) => a + (s.points ?? s.Points ?? s.score ?? s.Score ?? 0),
              0,
            );
            return acc + turnScore;
          }, 0);

        // Average accumulates over the whole match, so it uses every turn -
        // not just the current leg's slice.
        const p1Scored = calculateScored(p1Turns);
        const p2Scored = calculateScored(p2Turns);

        // Remaining score for the CURRENT leg only: everything thrown since
        // the last leg boundary (p1LegStartIndex/p2LegStartIndex, advanced in
        // checkLegWinCondition whenever a leg ends).
        const p1ScoredThisLeg = calculateScored(p1Turns.slice(this.p1LegStartIndex));
        const p2ScoredThisLeg = calculateScored(p2Turns.slice(this.p2LegStartIndex));

        this.player1.score = startScore - p1ScoredThisLeg;
        this.player2.score = startScore - p2ScoredThisLeg;

        this.checkLegWinCondition(startScore, p1Turns.length, p2Turns.length);

        // A numpad turn equals 3 darts. Darts thrown reset each leg (like
        // score), while average stays cumulative over the whole match.
        this.player1.dartsThrown = (p1Turns.length - this.p1LegStartIndex) * 3;
        this.player1.average = p1Turns.length > 0 ? p1Scored / p1Turns.length : 0;

        this.player2.dartsThrown = (p2Turns.length - this.p2LegStartIndex) * 3;
        this.player2.average = p2Turns.length > 0 ? p2Scored / p2Turns.length : 0;

        // Relies on every visit producing exactly one Turn row (see
        // processSubmittedScore below). Whoever should start THIS leg
        // (legStarter, alternating strictly by leg number) is active once
        // they and the other player have thrown the same number of turns
        // since the leg began; the other player is active after the
        // starter has thrown one more. Comparing only within-leg turn
        // counts (not the whole match) keeps this correct even when the
        // player who didn't start a leg ends up winning it.
        const legStarter = this.legsPlayedTotal % 2 === 0 ? this.player1Id : this.player2Id;
        const p1ThisLegTurns = p1Turns.length - this.p1LegStartIndex;
        const p2ThisLegTurns = p2Turns.length - this.p2LegStartIndex;

        if (legStarter === this.player1Id) {
          this.player1.isActive = p1ThisLegTurns === p2ThisLegTurns;
          this.player2.isActive = p1ThisLegTurns > p2ThisLegTurns;
        } else {
          this.player2.isActive = p1ThisLegTurns === p2ThisLegTurns;
          this.player1.isActive = p2ThisLegTurns > p1ThisLegTurns;
        }

        // No zone.js, so nothing re-renders unless asked explicitly.
        this.cdr.detectChanges();
      },
      error: (err) => console.error('Failed to load match data:', err),
    });
  }

  // legs/sets/targetLegs/targetSets only ever live as in-memory fields on
  // this component - never sent to or read from the backend. Each browser
  // watching the same match derives its own counters from the same raw turn
  // history, so they stay in sync only as long as every browser processes
  // the same sequence of loadGameData() calls (e.g. a missed SignalR update
  // could leave one browser's leg count behind the other's).
  checkLegWinCondition(startScore: number, p1TurnCount: number, p2TurnCount: number) {
    // loadGameData() can run more than once for the same checkout (the
    // submitting browser gets its own "GameUpdated" broadcast echoed back to
    // it, on top of the reload it already triggers directly). Without this
    // guard a single leg win would get processed twice.
    const totalTurns = p1TurnCount + p2TurnCount;
    if (totalTurns === this.lastProcessedTurnCount) {
      return;
    }
    this.lastProcessedTurnCount = totalTurns;

    if (this.player1.score === 0) {
      this.player1.legs++;
      this.legsPlayedTotal++;
      this.p1LegStartIndex = p1TurnCount;
      this.p2LegStartIndex = p2TurnCount;
      this.resetLegScores(startScore);
    } else if (this.player2.score === 0) {
      this.player2.legs++;
      this.legsPlayedTotal++;
      this.p1LegStartIndex = p1TurnCount;
      this.p2LegStartIndex = p2TurnCount;
      this.resetLegScores(startScore);
    }

    if (this.player1.legs === this.targetLegs) {
      this.player1.sets++;
      this.player1.legs = 0;
      this.player2.legs = 0;
    } else if (this.player2.legs === this.targetLegs) {
      this.player2.sets++;
      this.player1.legs = 0;
      this.player2.legs = 0;
    }

    if (this.player1.sets === this.targetSets) {
      alert(`${this.player1.name} wins the match!`);
      this.leaveMatch();
    } else if (this.player2.sets === this.targetSets) {
      alert(`${this.player2.name} wins the match!`);
      this.leaveMatch();
    }
  }

  resetLegScores(startScore: number) {
    // Immediate display reset to avoid a flash of the stale score before
    // the next loadGameData() recomputes it from the advanced leg index.
    this.player1.score = startScore;
    this.player2.score = startScore;
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
