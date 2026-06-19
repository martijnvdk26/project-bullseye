import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { tap } from 'rxjs/operators';
import {environment} from '../../../environments/environment'

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

  // Calls the POST api/player/login endpoint
  login(credentials: any) {
    return this.http.post<any>('/api/player/login', credentials).pipe(
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
