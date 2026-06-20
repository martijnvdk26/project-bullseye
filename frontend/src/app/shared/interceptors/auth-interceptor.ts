import { HttpInterceptorFn } from '@angular/common/http';
import {inject} from '@angular/core';
import {AuthService } from '../services/auth';


// Attaches the stored JWT (if any) to every outgoing HTTP request as a
// Bearer token, so the backend can identify the logged-in player without
// every individual service having to remember to add the header itself.
// In practice this is currently a no-op for every request in the app: guest
// sessions never log in, so getToken() returns null for them, and even the
// registered-login flow never actually receives a token to store (see the
// NOTE in auth.ts) - so this interceptor is correctly wired, but has nothing
// to attach yet.
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  // Inject the AuthService to access the stored JWT token
  const authService = inject(AuthService);
  const token = authService.getToken();

  // If a token exits (if a user is logged in), clone the request and add the Authorization header with the token
  if (token) {
    const clonedRequest = req.clone({
      setHeaders: {
        Authorization: `Bearer ${token}`,
      },
    });

    // Send the modified request to the backend
    return next(clonedRequest);
  }

  // If no token exists (e.g., during login or guest sessions), send the original request)
  return next(req);
};
