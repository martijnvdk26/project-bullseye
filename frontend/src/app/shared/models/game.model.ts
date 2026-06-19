export interface Game {
  id: number;
  variant: string;
  startedAt: string;
  endedAt?: string;
  winnerId?: number;
  guestSessionId: number;
}

export interface SubmitTurnRequest {
  gameId: number;
  playerId: number;
  score: number;
}
