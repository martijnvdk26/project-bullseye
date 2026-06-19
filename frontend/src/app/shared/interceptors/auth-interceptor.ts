import { HttpInterceptorFn } from '@angular/common/http';
import {inject} from '@angular/core';
import {AuthService } from '../services/auth';


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
