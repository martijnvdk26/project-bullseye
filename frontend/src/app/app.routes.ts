import { Routes } from '@angular/router';
import { authGuard } from './shared/guards/auth-guard';

// Import all your components traditionally at the top
import { LoginComponent } from './features/auth/login/login';
import { GuestLobbyComponent } from './features/guest/guest-lobby/guest-lobby';
import { GameBoardComponent } from './features/game/game-board/game-board';

export const routes: Routes = [
  // 1. Default route redirects to login
  { path: '', redirectTo: 'login', pathMatch: 'full' },

  // 2. Public routes
  { path: 'login', component: LoginComponent },
  { path: 'guest-lobby', component: GuestLobbyComponent },

  // 3. Protected route (Live Game) - Requires the authGuard!
  {
    path: 'game/:id',
    component: GameBoardComponent,
    canActivate: [authGuard],
  },

  // 4. Wildcard route catches bad URLs and redirects to login
  { path: '**', redirectTo: 'login' },
];
