import { Routes } from '@angular/router';
import { AuthLayoutComponent } from './layouts/auth-layout/auth-layout.component';
import { DashboardLayoutComponent } from './layouts/dashboard-layout/dashboard-layout.component';
import { authGuard } from '../core/guards/auth.guard';
import { guestGuard } from '../core/guards/guest.guard';
import { roleGuard } from '../core/guards/role.guard';

export const routes: Routes = [
  { path: 'confirm-email', redirectTo: 'auth/confirm-email' },
  { path: 'reset-password', redirectTo: 'auth/reset-password' },

  // Authentication Routes
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

  // Dashboard Layout Routes (Protected)
  {
    path: '',
    component: DashboardLayoutComponent,
    canActivate: [authGuard],
    children: [
      { path: 'dashboard', loadComponent: () => import('./features/dashboard/dashboard.component').then(m => m.DashboardComponent) },

      // Drills
      { path: 'drills', loadComponent: () => import('./features/drills/drill-template-list/drill-template-list.component').then(m => m.DrillTemplateListComponent) },
      { path: 'drills/sessions', loadComponent: () => import('./features/drills/drill-session-list.component/drill-session-list.component').then(m => m.DrillSessionListComponent) },
      { path: 'drills/sessions/new', loadComponent: () => import('./features/drills/drill-session-create.component/drill-session-create.component').then(m => m.DrillSessionCreateComponent) },
      { path: 'drills/sessions/:id', loadComponent: () => import('./features/drills/drill-session-details.component/drill-session-details.component').then(m => m.DrillSessionDetailsComponent) },
      { path: 'drills/players/:playerId/progression', loadComponent: () => import('./features/drills/player-drill-progression.component/player-drill-progression.component').then(m => m.PlayerDrillProgressionComponent) },

      // Drill Analytics
      { path: 'drills/analytics/weak-categories', loadComponent: () => import('./features/drills/squad-weak-categories.component/squad-weak-categories.component').then(m => m.SquadWeakCategoriesComponent) },
      { path: 'drills/analytics/coach-bias', loadComponent: () => import('./features/drills/coach-bias-analytics.component/coach-bias-analytics.component').then(m => m.CoachBiasAnalyticsComponent) },
      { path: 'drills/analytics/coach-bias/:coachId', loadComponent: () => import('./features/drills/coach-bias-analytics.component/coach-bias-analytics.component').then(m => m.CoachBiasAnalyticsComponent) },
      { path: 'drills/analytics/coach_bias', redirectTo: 'drills/analytics/coach-bias' },
      { path: 'drills/analytics/coach_bias/:coachId', redirectTo: 'drills/analytics/coach-bias/:coachId' },

      // Settings & Tournaments
      { path: 'settings/change-password', loadComponent: () => import('./features/auth/pages/change-password/change-password.component').then(m => m.ChangePasswordComponent) },
      { path: 'tournament/list', loadComponent: () => import('./features/tournament/pages/tournament-list/tournament-list.component').then(m => m.TournamentListComponent) },
      { path: 'tournament/create', loadComponent: () => import('./features/tournament/pages/tournament-manage/tournament-manage.component').then(m => m.TournamentManageComponent) },
      { path: 'tournament/manage/:id', loadComponent: () => import('./features/tournament/pages/tournament-manage/tournament-manage.component').then(m => m.TournamentManageComponent) },
      { path: 'tournament/manage', redirectTo: 'tournament/list', pathMatch: 'full' },
      { path: 'tournament/details/:id', loadComponent: () => import('./features/tournament/pages/tournament-details/tournament-details.component').then(m => m.TournamentDetailsComponent) },
      // Parent Features
      {
        path: 'parent/dashboard',
        loadComponent: () => import("./features/Parent/parent-dashboard.component/parent-dashboard.component").then(m => m.ParentDashboardComponent),
        canActivate: [roleGuard],
        data: { roles: ['Parent'] }
      },
      {
        path: 'parent/subscriptions',
        loadComponent: () => import("./features/Parent/parent-subscriptions.component/parent-subscriptions.component").then(m => m.ParentSubscriptionsComponent),
        canActivate: [roleGuard],
        data: { roles: ['Parent'] }
      },
      {
        path: 'academy-admin/subscriptions',
        loadComponent: () =>
          import('./features/academy-admin/components/academy-admin-subscriptions.component/academy-admin-subscriptions.component')
            .then(m => m.AcademyAdminSubscriptionsComponent),
        canActivate: [roleGuard],
        data: { roles: ['AcademyAdmin'] }
      },

      { path: '', redirectTo: 'dashboard', pathMatch: 'full' }
    ]
  },

  // Matches
  {
    path: 'coach/matches',
    loadComponent: () => import('./features/match/pages/coach-match-list/coach-match-list.component').then(m => m.CoachMatchListComponent),
    canActivate: [authGuard]
  },
  {
    path: 'match/:id',
    loadComponent: () => import('./features/match/pages/match-detail/match-detail.component').then(m => m.MatchDetailComponent),
    canActivate: [authGuard]
  },
  {
    path: 'academy/matches',
    loadComponent: () => import('./features/match/pages/academy-match-list/academy-match-list.component').then(m => m.AcademyMatchListComponent),
    canActivate: [authGuard, roleGuard],
    data: { roles: ['AcademyAdmin', 'SuperAdmin'] }
  },

  // Academy Admin
  {
    path: 'academy-admin/dashboard',
    loadComponent: () => import('./features/academy-admin/pages/academy-admin-dashboard/academy-admin-dashboard.component').then(m => m.AcademyAdminDashboardComponent),
    canActivate: [authGuard, roleGuard],
    data: { roles: ['AcademyAdmin'] }
  },

  // Player Features
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

  // Notifications & Utilities
  {
    path: 'academy-announcement/:academyId',
    loadComponent: () => import('./features/notification/pages/academy-announcement/academy-announcement').then(m => m.AcademyAnnouncement),
    canActivate: [authGuard]
  },
  { path: 'referenceshowcase', loadComponent: () => import('./reference-showcase').then(m => m.App) },

  // Wildcard Catch-all
  { path: '**', redirectTo: 'auth/login' }
];