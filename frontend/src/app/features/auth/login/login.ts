import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { AuthService } from '../../../shared/services/auth';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './login.html',
  styleUrl: './login.css',
})
export class LoginComponent {
  // Injecting dependencies using the modern inject() function instead of the constructor
  private fb = inject(FormBuilder);
  private authService = inject(AuthService);

  // Holds any error messages returned from the C# API
  errorMessage = '';

  // Set up a Reactive Form with built-in validation rules
  // This prevents invalid data from even reaching the backend
  loginForm = this.fb.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', Validators.required],
  });

  // Triggered when the user submits the form
  onSubmit() {
    // Only proceed if the form passes validation
    if (this.loginForm.valid) {
      this.authService.login(this.loginForm.value).subscribe({
        // If login fails, extract the error message from the backend response
        error: (err) =>
          (this.errorMessage = err.error.message || 'Login failed. Check your credentials.'),
      });
    }
  }
}
