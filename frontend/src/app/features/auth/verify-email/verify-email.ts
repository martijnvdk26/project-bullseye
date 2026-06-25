import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { AuthService } from '../../../shared/services/auth';

type VerifyState = 'verifying' | 'success' | 'error';

@Component({
  selector: 'app-verify-email',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './verify-email.html',
  styleUrl: './verify-email.css',
})
export class VerifyEmailComponent {
  private route = inject(ActivatedRoute);
  private authService = inject(AuthService);

  state: VerifyState = 'verifying';
  message = '';

  constructor() {
    const token = this.route.snapshot.queryParamMap.get('token');
    if (!token) {
      this.state = 'error';
      this.message = 'Missing verification token.';
      return;
    }

    this.authService.verifyEmail(token).subscribe({
      next: (res) => {
        this.state = 'success';
        this.message = res?.message || 'Email verified — you can now log in.';
      },
      error: (err) => {
        this.state = 'error';
        this.message = err.error?.message || 'This verification link is invalid or expired.';
      },
    });
  }
}
