import { Routes } from '@angular/router';

// Import all your components traditionally at the top
import { LoginComponent } from './features/auth/login/login';
import { RegisterComponent } from './features/auth/register/register';
import { DashboardComponent } from './features/dashboard/dashboard';
import { GuestLobbyComponent } from './features/guest/guest-lobby/guest-lobby';
import { RegisteredLobbyComponent } from './features/registered-lobby/registered-lobby/registered-lobby';
import { GameBoardComponent } from './features/game/game-board/game-board';
import { authGuard } from './shared/guards/auth-guard';

export const routes: Routes = [
  // 1. Default route redirects to login
  { path: '', redirectTo: 'login', pathMatch: 'full' },

  // 2. Public routes
  { path: 'login', component: LoginComponent },
  { path: 'register', component: RegisterComponent },
  { path: 'guest-lobby', component: GuestLobbyComponent },

  // 3. Requires a logged-in registered player
  { path: 'dashboard', component: DashboardComponent, canActivate: [authGuard] },
  { path: 'registered-lobby', component: RegisteredLobbyComponent, canActivate: [authGuard] },

  // 4. Live Game - reached only through the guest-lobby flow, which never logs
  // players in, so this must stay public like the rest of that flow
  { path: 'game/:id', component: GameBoardComponent },

  // 5. Wildcard route catches bad URLs and redirects to login
  { path: '**', redirectTo: 'login' },
];
