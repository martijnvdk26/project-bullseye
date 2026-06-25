import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { tap } from 'rxjs/operators';
import {environment} from '../../../environments/environment'

// Handles the registered-account login session: storing the JWT, exposing
// the current user, and clearing both on logout.
@Injectable({
  providedIn: 'root',
})
export class AuthService {
  private http = inject(HttpClient);
  private router = inject(Router);

  // Get backend url from environment.ts file
  private readonly apiUrl = environment.apiUrl;

  // Using an Angular Signal to hold the current user's state.
  currentUser = signal<{ id: number; name: string; email: string } | null>(null);

  constructor() {
    // currentUser is otherwise only ever set inside login()'s response
    // handler, so a page refresh would make every logged-in player look
    // anonymous again even though their token is still valid in storage.
    this.restoreSession();
  }

  private restoreSession() {
    const token = this.getToken();
    if (!token) return;

    const playerId = this.decodeSubClaim(token);
    if (playerId == null) return;

    // Reuses the existing stats endpoint as a "who am I" lookup - it already
    // returns the full PlayerDto (id/name/email) for an authenticated player
    this.http.get<any>(`${this.apiUrl}/player/${playerId}/stats`).subscribe({
      next: (player) => this.currentUser.set(player),
      error: () => {
        // Token is stale/invalid - drop it rather than keep failing silently
        localStorage.removeItem('token');
      },
    });
  }

  private decodeSubClaim(token: string): number | null {
    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      return payload.sub ? Number(payload.sub) : null;
    } catch {
      return null;
    }
  }

  // Calls the POST api/player/login endpoint
  login(credentials: any) {
    return this.http.post<any>(`${this.apiUrl}/player/login`, credentials).pipe(
      // tap() allows us to perform side effects (like saving tokens) without altering the response data
      tap((res) => {
        if (res.token) {
          // Save the token to local storage so the interceptor can grab it
          localStorage.setItem('token', res.token);
          // Update the application state with the logged-in player's details
          this.currentUser.set(res.player);
          // Navigate away from the login screen
          this.router.navigate(['/dashboard']);
        }
      }),
    );
  }

  // Calls the POST api/player/register endpoint. Does not log the player in -
  // a freshly registered account can't log in until the Resend verification
  // link is clicked, so they're sent back to /login with a "check your inbox"
  // message instead.
  register(payload: { name: string; email: string; password: string }) {
    return this.http.post<any>(`${this.apiUrl}/player/register`, payload);
  }

  // Calls the GET api/player/verify-email endpoint hit by the link in the
  // Resend verification email.
  verifyEmail(token: string) {
    return this.http.get<any>(`${this.apiUrl}/player/verify-email`, { params: { token } });
  }

  // Calls the POST api/player/resend-verification endpoint. Backend always
  // returns the same generic message regardless of whether the address
  // exists, so there's nothing account-specific to branch on here.
  resendVerification(email: string) {
    return this.http.post<any>(`${this.apiUrl}/player/resend-verification`, { email });
  }

  // Clears the session and boots the user back to the login screen
  logout() {
    localStorage.removeItem('token');
    this.currentUser.set(null);
    this.router.navigate(['/login']);
  }

  // Helper method for the interceptor to retrieve the token
  getToken(): string | null {
    return localStorage.getItem('token');
  }
}
