import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../shared/services/auth';
import { environment } from '../../../environments/environment';

interface PlayerStats {
  id: number;
  name: string;
  email?: string;
  avatarUrl?: string;
  threeDartAverage: number;
  checkoutPercentage: number;
  highestFinish: number;
}

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.css',
})
export class DashboardComponent {
  private http = inject(HttpClient);
  authService = inject(AuthService);

  stats = signal<PlayerStats | null>(null);
  errorMessage = '';

  constructor() {
    const player = this.authService.currentUser();
    if (player) {
      this.http.get<PlayerStats>(`${environment.apiUrl}/player/${player.id}/stats`).subscribe({
        next: (res) => this.stats.set(res),
        error: () => (this.errorMessage = 'Could not load your stats.'),
      });
    }
  }

  logout() {
    this.authService.logout();
  }
}
