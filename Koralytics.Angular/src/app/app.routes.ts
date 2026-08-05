import { inject } from '@angular/core';
import { Routes } from '@angular/router';
import { AuthLayoutComponent } from './layouts/auth-layout/auth-layout.component';
import { DashboardLayoutComponent } from './layouts/dashboard-layout/dashboard-layout.component';
import { authGuard } from '../core/guards/auth.guard';
import { guestGuard } from '../core/guards/guest.guard';
import { roleGuard } from '../core/guards/role.guard';
import { AuthService } from '../core/services/auth/auth.service';

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
      { path: 'dashboard', redirectTo: () => inject(AuthService).getRoleDashboardRoute(), pathMatch: 'full' },

      // Drills
      {
        path: 'drills',
        loadComponent: () => import('./features/drills/drill-template-list/drill-template-list.component').then(m => m.DrillTemplateListComponent),
        canActivate: [roleGuard],
        data: { roles: ['AcademyAdmin', 'Coach', 'SystemAdmin'] }
      },
      {
        path: 'drills/sessions',
        loadComponent: () => import('./features/drills/drill-session-list.component/drill-session-list.component').then(m => m.DrillSessionListComponent),
        canActivate: [roleGuard],
        data: { roles: ['AcademyAdmin', 'Coach'] }
      },
      {
        path: 'drills/sessions/new',
        loadComponent: () => import('./features/drills/drill-session-create.component/drill-session-create.component').then(m => m.DrillSessionCreateComponent),
        canActivate: [roleGuard],
        data: { roles: ['Coach'] }
      },
      {
        path: 'drills/sessions/:id',
        loadComponent: () => import('./features/drills/drill-session-details.component/drill-session-details.component').then(m => m.DrillSessionDetailsComponent),
        canActivate: [roleGuard],
        data: { roles: ['AcademyAdmin', 'Coach'] }
      },

      // Drill Analytics
      {
        path: 'drills/analytics/weak-categories',
        loadComponent: () => import('./features/drills/squad-weak-categories.component/squad-weak-categories.component').then(m => m.SquadWeakCategoriesComponent),
        canActivate: [roleGuard],
        data: { roles: ['AcademyAdmin', 'Coach', 'SystemAdmin'] }
      },
      {
        path: 'drills/analytics/coach-bias',
        loadComponent: () => import('./features/drills/coach-bias-analytics.component/coach-bias-analytics.component').then(m => m.CoachBiasAnalyticsComponent),
        canActivate: [roleGuard],
        data: { roles: ['AcademyAdmin', 'Coach', 'SystemAdmin'] }
      },
      {
        path: 'drills/analytics/coach-bias/:coachId',
        loadComponent: () => import('./features/drills/coach-bias-analytics.component/coach-bias-analytics.component').then(m => m.CoachBiasAnalyticsComponent),
        canActivate: [roleGuard],
        data: { roles: ['AcademyAdmin', 'Coach', 'SystemAdmin'] }
      },
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

      { path: 'profile/me', loadComponent: () => import('./features/profile/my-profile/my-profile.component').then(m => m.MyProfileComponent) },
      { path: 'settings/change-password', loadComponent: () => import('./features/auth/pages/change-password/change-password.component').then(m => m.ChangePasswordComponent) },
      {
        path: 'coach/squad',
        loadComponent: () => import('./features/coach/pages/coach-squad/coach-squad.component').then(m => m.CoachSquadComponent),
        canActivate: [roleGuard],
        data: { roles: ['Coach'] }
      },
      {
        path: 'coach/training-split',
        loadComponent: () => import('./features/coach/pages/training-split/training-split.component').then(m => m.TrainingSplitComponent),
        canActivate: [roleGuard],
        data: { roles: ['Coach'] }
      },
      {
        path: 'coach/notes',
        loadComponent: () => import('./features/coach/pages/coach-notes/coach-notes.component').then(m => m.CoachNotesComponent),
        canActivate: [roleGuard],
        data: { roles: ['Coach'] }
      },
      {
        path: 'coach/access',
        loadComponent: () => import('./features/coach/pages/temp-access/temp-access.component').then(m => m.TempAccessComponent),
        canActivate: [roleGuard],
        data: { roles: ['Coach'] }
      },
      {
        path: 'coach/highlights',
        redirectTo: 'player/highlights',
        pathMatch: 'full'
      },
      {
        path: 'player/highlights',
        loadComponent: () => import('./features/player/player-highlights/player-highlights.component').then(m => m.PlayerHighlightsComponent),
        canActivate: [roleGuard],
        data: { roles: ['Player'] }
      },
      {
        path: 'coach/match-requests',
        loadComponent: () => import('./features/coach/pages/match-request/match-request.component').then(m => m.MatchRequestComponent),
        canActivate: [roleGuard],
        data: { roles: ['Coach', 'AcademyAdmin'] }
      },
      {
        path: 'coach/readiness',
        loadComponent: () => import('./features/coach/pages/player-readiness/player-readiness.component').then(m => m.PlayerReadinessComponent),
        canActivate: [roleGuard],
        data: { roles: ['Coach'] }
      },
      { path: 'tournament/list', loadComponent: () => import('./features/tournament/pages/tournament-list/tournament-list.component').then(m => m.TournamentListComponent) },
      { path: 'tournament/create', loadComponent: () => import('./features/tournament/pages/tournament-manage/tournament-manage.component').then(m => m.TournamentManageComponent) },
      { path: 'tournament/manage/:id', loadComponent: () => import('./features/tournament/pages/tournament-manage/tournament-manage.component').then(m => m.TournamentManageComponent) },
      { path: 'tournament/manage', redirectTo: 'tournament/list', pathMatch: 'full' },
      { path: 'tournament/details/:id', loadComponent: () => import('./features/tournament/pages/tournament-details/tournament-details.component').then(m => m.TournamentDetailsComponent) },
      { path: 'tournament/:id/squad-registration', loadComponent: () => import('./features/tournament/pages/squad-registration/squad-registration.component').then(m => m.SquadRegistrationComponent) },
      { path: 'match/friendly-request', loadComponent: () => import('./features/match/pages/friendly-match-request/friendly-match-request.component').then(m => m.FriendlyMatchRequestComponent), canActivate: [roleGuard], data: { roles: ['Coach', 'AcademyAdmin'] } },
      { path: 'match/requests/incoming', loadComponent: () => import('./features/match/pages/match-request-incoming/match-request-incoming.component').then(m => m.MatchRequestIncomingComponent), canActivate: [roleGuard], data: { roles: ['Coach', 'AcademyAdmin'] } },
      { path: 'match/requests/outgoing', loadComponent: () => import('./features/match/pages/match-request-outgoing/match-request-outgoing.component').then(m => m.MatchRequestOutgoingComponent), canActivate: [roleGuard], data: { roles: ['Coach', 'AcademyAdmin'] } },
      { path: '', redirectTo: () => inject(AuthService).getRoleDashboardRoute(), pathMatch: 'full' }
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
    path: 'match/:id/report',
    loadComponent: () => import('./features/match/pages/match-report/match-report.component').then(m => m.MatchReportComponent),
    canActivate: [authGuard]
  },
  {
    path: 'match/:id/submit-lineup',
    loadComponent: () => import('./features/match/pages/submit-lineup/submit-lineup.component').then(m => m.SubmitLineupComponent),
    canActivate: [authGuard, roleGuard],
    data: { roles: ['Coach'] }
  },
  {
    path: 'session/:sessionId/create-match',
    loadComponent: () => import('./features/match/pages/session-match/session-match.component').then(m => m.SessionMatchComponent),
    canActivate: [authGuard, roleGuard],
    data: { roles: ['Coach'] }
  },
  {
    path: 'tournament/fixture/:fixtureId/create-match',
    loadComponent: () => import('./features/match/pages/create-tournament-match/create-tournament-match.component').then(m => m.CreateTournamentMatchComponent),
    canActivate: [authGuard]
  },
  {
    path: 'academy/matches',
    loadComponent: () => import('./features/match/pages/academy-match-list/academy-match-list.component').then(m => m.AcademyMatchListComponent),
    canActivate: [authGuard, roleGuard],
    data: { roles: ['AcademyAdmin', 'SystemAdmin'] }
  },
  {
    path: 'academy-admin/dashboard',
    loadComponent: () => import('./features/academy-admin/pages/academy-admin-dashboard/academy-admin-dashboard.component').then(m => m.AcademyAdminDashboardComponent),
    canActivate: [authGuard, roleGuard],
    data: { roles: ['AcademyAdmin'] }
  },
  {
    path: 'academy-admin/drills-dashboard',
    loadComponent: () => import('./features/academy-admin/pages/drills-dashboard/drills-dashboard.component').then(m => m.DrillsDashboardComponent),
    canActivate: [authGuard, roleGuard],
    data: { roles: ['AcademyAdmin'] }
  },
  {
    path: 'academy/profile/:id',
    loadComponent: () => import('./features/academy-profile/academy-profile.component').then(m => m.AcademyProfileComponent)
  },

  // Player Features
  {
    path: 'system-admin/dashboard',
    loadComponent: () => import('./features/system-admin/pages/system-admin-dashboard/system-admin-dashboard.component').then(m => m.SystemAdminDashboardComponent),
    canActivate: [authGuard, roleGuard],
    data: { roles: ['SystemAdmin'] }
  },
  {
    path: 'coach/dashboard',
    loadComponent: () => import('./features/coach/pages/coach-dashboard/coach-dashboard.component').then(m => m.CoachDashboardComponent),
    canActivate: [authGuard, roleGuard],
    data: { roles: ['Coach'] }
  },
  {
    path: 'scouter/dashboard',
    loadComponent: () => import('./features/scouter/pages/scouter-dashboard/scouter-dashboard.component').then(m => m.ScouterDashboardComponent),
    canActivate: [authGuard, roleGuard],
    data: { roles: ['Scouter'] }
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
    data: { roles: ['Player', 'Parent'] }
  },
  {
    path: 'player/team-events/:playerId',
    loadComponent: () => import('./features/player/player-team-events/player-team-events.component').then(m => m.PlayerTeamEventsComponent),
    canActivate: [authGuard, roleGuard],
    data: { roles: ['Player', 'Parent'] }
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
    path: 'player/academy-comparison/:playerId',
    loadComponent: () => import('./features/player/player-academy-comparison/player-academy-comparison.component').then(m => m.PlayerAcademyComparisonComponent),
    canActivate: [authGuard]
  },

  // Notifications & Utilities
  {
    path: 'academy-announcement/:academyId',
    loadComponent: () => import('./features/notification/pages/academy-announcement/academy-announcement').then(m => m.AcademyAnnouncement),
    canActivate: [authGuard]
  },
  {
    path: 'notifications-list',
    loadComponent: () => import('./features/notification/pages/notifications-list/notifications-list').then(m => m.NotificationsList),
    canActivate: [authGuard]
  },
  {
    path: 'followed-players/:scouterId',
    loadComponent: () => import('./features/scouter/followed-player/followed-player').then(m => m.FollowedPlayersComponent),
    canActivate: [authGuard]
  },
   {
    path: 'shortlist/:scouterId',
    loadComponent: () => import('./features/scouter/scouter-shortlist/scouter-shortlist').then(m => m.ScouterShortlistComponent),
    canActivate: [authGuard]
  },
  {
    path: 'search/:scouterId',
    loadComponent: () => import('./features/scouter/scoutersearch/scoutersearch').then(m => m.ScouterSearchComponent),
    canActivate: [authGuard]
  },
  { path: 'referenceshowcase', loadComponent: () => import('./reference-showcase').then(m => m.App) },

  // Wildcard Catch-all
  { path: '**', redirectTo: 'auth/login' }
];