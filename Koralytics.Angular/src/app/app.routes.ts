import { Routes } from '@angular/router';
import { AuthLayoutComponent } from './layouts/auth-layout/auth-layout.component';
import { DashboardLayoutComponent } from './layouts/dashboard-layout/dashboard-layout.component';
import { authGuard } from '../core/guards/auth.guard';
import { guestGuard } from '../core/guards/guest.guard';

export const routes: Routes = [
  { path: 'confirm-email', redirectTo: 'auth/confirm-email' },
  { path: 'reset-password', redirectTo: 'auth/reset-password' },
  {
    path: 'auth',
    component: AuthLayoutComponent,
    canActivate: [guestGuard],
    children: [
      { path: 'login', loadComponent: () => import('./features/auth/pages/login/login.component').then(m => m.LoginComponent) },
      { path: 'register', loadComponent: () => import('./features/auth/pages/register/register.component').then(m => m.RegisterComponent) },
      { path: 'complete-profile', loadComponent: () => import('./features/auth/pages/complete-profile/complete-profile.component').then(m => m.CompleteProfileComponent) },
      { path: 'forgot-password', loadComponent: () => import('./features/auth/pages/forgot-password/forgot-password.component').then(m => m.ForgotPasswordComponent) },
      { path: 'reset-password', loadComponent: () => import('./features/auth/pages/reset-password/reset-password.component').then(m => m.ResetPasswordComponent) },
      { path: 'confirm-email', loadComponent: () => import('./features/auth/pages/confirm-email/confirm-email.component').then(m => m.ConfirmEmailComponent) },
      { path: '', redirectTo: 'login', pathMatch: 'full' }
    ]
  },
  {
    path: '',
    component: DashboardLayoutComponent,
    canActivate: [authGuard],
    children: [
      { path: 'dashboard', loadComponent: () => import('./features/dashboard/dashboard.component').then(m => m.DashboardComponent) },
      { path: 'settings/change-password', loadComponent: () => import('./features/auth/pages/change-password/change-password.component').then(m => m.ChangePasswordComponent) },
      { path: 'tournament/list', loadComponent: () => import('./features/tournament/pages/tournament-list/tournament-list.component').then(m => m.TournamentListComponent) },
      { path: 'tournament/create', loadComponent: () => import('./features/tournament/pages/tournament-manage/tournament-manage.component').then(m => m.TournamentManageComponent) },
      { path: 'tournament/manage/:id', loadComponent: () => import('./features/tournament/pages/tournament-manage/tournament-manage.component').then(m => m.TournamentManageComponent) },
      { path: 'tournament/manage', redirectTo: 'tournament/list', pathMatch: 'full' },
      { path: 'tournament/details/:id', loadComponent: () => import('./features/tournament/pages/tournament-details/tournament-details.component').then(m => m.TournamentDetailsComponent) },
      { path: 'tournament/:id/squad-registration', loadComponent: () => import('./features/tournament/pages/squad-registration/squad-registration.component').then(m => m.SquadRegistrationComponent) },
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' }
    ]
  },
  { path: 'referenceshowcase', loadComponent: () => import('./reference-showcase').then(m => m.App) },
  { path: '**', redirectTo: 'auth/login' }
];
