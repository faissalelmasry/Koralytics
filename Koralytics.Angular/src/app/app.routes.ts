import { Routes } from '@angular/router';
import { AuthLayoutComponent } from './layouts/auth-layout/auth-layout.component';
import { DashboardLayoutComponent } from './layouts/dashboard-layout/dashboard-layout.component';
import { authGuard } from '../core/guards/auth.guard';
import { guestGuard } from '../core/guards/guest.guard';
import { roleGuard } from '../core/guards/role.guard';

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
      { path: 'drills', loadComponent: () => import('./features/drills/drill-template-list/drill-template-list.component').then(m => m.DrillTemplateListComponent) },
      { path: 'drills/sessions', loadComponent: () => import('./features/drills/drill-session-list.component/drill-session-list.component').then(m => m.DrillSessionListComponent) },
      { path: 'drills/sessions/new', loadComponent: () => import('./features/drills/drill-session-create.component/drill-session-create.component').then(m => m.DrillSessionCreateComponent) },
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
  { 
    path: 'academy-admin/dashboard', 
    loadComponent: () => import('./features/academy-admin/pages/academy-admin-dashboard/academy-admin-dashboard.component').then(m => m.AcademyAdminDashboardComponent),
    canActivate: [authGuard, roleGuard],
    data: { roles: ['AcademyAdmin'] }
  },
  {
    path: 'player/profile',
    loadComponent: () => import('./features/player/player-profile/player-profile.component').then(m => m.PlayerProfileComponent),
    canActivate: [authGuard, roleGuard],
    data: { roles: ['Player'] }
  },
  {
    path: 'player/profile/:playerId',
    loadComponent: () => import('./features/player/player-profile/player-profile.component').then(m => m.PlayerProfileComponent),
    canActivate: [authGuard]
  },
  {
    path: 'player/timeline',
    loadComponent: () => import('./features/player/player-match-timeline/player-match-timeline.component').then(m => m.PlayerMatchTimelineComponent),
    canActivate: [authGuard, roleGuard],
    data: { roles: ['Player'] }
  },
  {
    path: 'player/timeline/:playerId',
    loadComponent: () => import('./features/player/player-match-timeline/player-match-timeline.component').then(m => m.PlayerMatchTimelineComponent),
    canActivate: [authGuard]
  },
  {
    path: 'player/drill-timeline',
    loadComponent: () => import('./features/player/player-drill-timeline/player-drill-timeline.component').then(m => m.PlayerDrillTimelineComponent),
    canActivate: [authGuard, roleGuard],
    data: { roles: ['Player'] }
  },
  {
    path: 'player/drill-timeline/:playerId',
    loadComponent: () => import('./features/player/player-drill-timeline/player-drill-timeline.component').then(m => m.PlayerDrillTimelineComponent),
    canActivate: [authGuard]
  },
  {
    path: 'player/team-events',
    loadComponent: () => import('./features/player/player-team-events/player-team-events.component').then(m => m.PlayerTeamEventsComponent),
    canActivate: [authGuard, roleGuard],
    data: { roles: ['Player'] }
  },
  {
    path: 'player/scouter-views',
    loadComponent: () => import('./features/player/player-scouter-views/player-scouter-views.component').then(m => m.PlayerScouterViewsComponent),
    canActivate: [authGuard, roleGuard],
    data: { roles: ['Player'] }
  },
  {
    path: 'player/academy-comparison',
    loadComponent: () => import('./features/player/player-academy-comparison/player-academy-comparison.component').then(m => m.PlayerAcademyComparisonComponent),
    canActivate: [authGuard, roleGuard],
    data: { roles: ['Player'] }
  },
  {
    path: 'profile-views-analytics/:playerId',
    loadComponent: () => import('./features/ProfileViewsAnalyticsPage/profile-views-analytics/profile-views-analytics').then(m => m.ProfileViewsAnalytics),
    canActivate: [authGuard]
  },
  {
    path: 'academy-announcement/:academyId',
    loadComponent: () => import('./features/notification/pages/academy-announcement/academy-announcement').then(m => m.AcademyAnnouncement),
    canActivate: [authGuard]
  },
  { path: 'referenceshowcase', loadComponent: () => import('./reference-showcase').then(m => m.App) },
  { path: '**', redirectTo: 'auth/login' }
];
