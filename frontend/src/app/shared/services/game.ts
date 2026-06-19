import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Game, SubmitTurnRequest } from '../models/game.model';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root',
})
export class GameService {
  private http = inject(HttpClient);
  private readonly apiUrl = environment.apiUrl;

  // Fetches the initial state of the match (e.g., variant type like '501')
  getGame(id: number): Observable<Game> {
    return this.http.get<Game>(`${this.apiUrl}/game/${id}`);
  }

  // Sends the validated 3-dart turn score to the backend
  submitManualTurn(request: SubmitTurnRequest): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.apiUrl}/game/turn`, request);
  }

  // Used for individual dart tracking if you expand the UI later
  submitScore(gameId: number, score: number): Observable<any> {
    return this.http.post(`${this.apiUrl}/score`, { gameId, score });
  }
}
