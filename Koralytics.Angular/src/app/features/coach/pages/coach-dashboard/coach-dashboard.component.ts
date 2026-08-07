import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { NavbarComponent } from '../../../../../shared/components/navbar/navbar';
import { Footer } from '../../../../../shared/components/footer/footer';
import { LoadingSpinnerComponent } from '../../../../../shared/components/loading-spinner/loading-spinner';
import { ScrollRevealDirective } from '../../../../../shared/directives/scroll-reveal.directive';

import { AuthService } from '../../../../../core/services/auth/auth.service';
import { CoachSquadService } from '../../../../../core/services/coach/coach-squad.service';
import { MatchService, PostMatchAnalysisResponseDto } from '../../../../../core/services/match/match.service';
import { DrillSessionService } from '../../../../../core/services/drill/drill-session.service';

import { CoachTeamDto, SquadPlayerDto } from '../../../../../core/interfaces/coach.interfaces';
import { MatchCardModel } from '../../../../../core/models/Match/match-card.model';
import { DrillSessionDto, SessionFilterDto } from '../../../../../core/interfaces/drill-session.model';

@Component({
  selector: 'app-coach-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    NavbarComponent,
    Footer,
    LoadingSpinnerComponent,
    ScrollRevealDirective
  ],
  templateUrl: './coach-dashboard.component.html',
  styleUrls: ['./coach-dashboard.component.css']
})
export class CoachDashboardComponent implements OnInit {
  private authService = inject(AuthService);
  private squadService = inject(CoachSquadService);
  private matchService = inject(MatchService);
  private sessionService = inject(DrillSessionService);
  private destroyRef = inject(DestroyRef);

  // ── User ──
  userName = '';
  userInitial = '';
  todayFormatted = '';

  // ── Teams ──
  teams = signal<CoachTeamDto[]>([]);
  selectedTeamId = 0;
  selectedTeamName = computed(() => {
    const currentId = this.selectedTeamId;
    const match = this.teams().find(t => t.teamId === currentId);
    return match ? `${match.teamName} (${match.ageGroupName})` : 'Squad Command';
  });

  // ── Loading states ──
  pageLoading = signal(true);
  squadLoading = signal(false);
  matchesLoading = signal(false);
  sessionsLoading = signal(false);
  performanceLoading = signal(false);

  // ── Squad data ──
  squadPlayers = signal<SquadPlayerDto[]>([]);

  totalPlayers = computed(() => this.squadPlayers().length);
  avgRating = computed(() => {
    const players = this.squadPlayers();
    if (players.length === 0) return 0;
    return players.reduce((sum, p) => sum + p.overallRating, 0) / players.length;
  });

  availableCount = computed(() =>
    this.squadPlayers().filter(p => p.availabilityStatus?.toLowerCase() === 'available').length
  );
  injuredCount = computed(() =>
    this.squadPlayers().filter(p => p.availabilityStatus?.toLowerCase() === 'injured').length
  );
  otherCount = computed(() =>
    this.totalPlayers() - this.availableCount() - this.injuredCount()
  );

  readinessAvailablePercent = computed(() =>
    this.totalPlayers() > 0 ? (this.availableCount() / this.totalPlayers()) * 100 : 0
  );
  readinessInjuredPercent = computed(() =>
    this.totalPlayers() > 0 ? (this.injuredCount() / this.totalPlayers()) * 100 : 0
  );
  readinessOtherPercent = computed(() =>
    this.totalPlayers() > 0 ? (this.otherCount() / this.totalPlayers()) * 100 : 0
  );

  // ── Matches ──
  upcomingMatches = signal<MatchCardModel[]>([]);
  upcomingMatchCount = computed(() => this.upcomingMatches().length);

  // ── Sessions ──
  recentSessions = signal<DrillSessionDto[]>([]);
  recentSessionCount = computed(() => this.recentSessions().length);

  // ── Performance ──
  performance = signal<PostMatchAnalysisResponseDto | null>(null);
  totalMatchesPlayed = computed(() => {
    const p = this.performance();
    return p ? p.wins + p.losses + p.draws : 0;
  });
  winRate = computed(() => {
    const total = this.totalMatchesPlayed();
    if (total === 0) return 0;
    return (this.performance()!.wins / total) * 100;
  });

  ngOnInit(): void {
    // Set user info
    const user = this.authService.getCurrentUserSync();
    if (user) {
      this.userName = user.fullName || user.userName;
      this.userInitial = this.userName ? this.userName[0].toUpperCase() : 'C';
    }

    // Format today
    const now = new Date();
    this.todayFormatted = now.toLocaleDateString('en-US', {
      weekday: 'long', year: 'numeric', month: 'long', day: 'numeric'
    });

    // Load coach teams first, then load everything else
    this.squadService.getCoachTeams()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (teams) => {
          this.teams.set(teams);
          if (teams.length > 0) {
            this.selectedTeamId = teams[0].teamId;
            this.pageLoading.set(false);
            this.loadAllData();
          } else {
            this.pageLoading.set(false);
          }
        },
        error: () => {
          this.pageLoading.set(false);
        }
      });

    // Load sessions independently (not team-specific)
    this.loadSessions();
  }

  onTeamChange(event: Event): void {
    const select = event.target as HTMLSelectElement;
    this.selectedTeamId = +select.value;
    this.loadAllData();
  }

  private loadAllData(): void {
    this.loadSquad();
    this.loadMatches();
    this.loadSessions();
    this.loadPerformance();
  }

  private loadSquad(): void {
    if (!this.selectedTeamId) return;
    this.squadLoading.set(true);

    this.squadService.getSquad(this.selectedTeamId)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        catchError(() => of(null))
      )
      .subscribe(data => {
        if (data?.players) {
          this.squadPlayers.set(data.players);
        } else {
          this.squadPlayers.set([]);
        }
        this.squadLoading.set(false);
      });
  }

  private loadMatches(): void {
    this.matchesLoading.set(true);

    this.matchService.getCoachMatches('Scheduled', undefined, undefined, undefined, 1, 5)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        catchError(() => of(null))
      )
      .subscribe(res => {
        if (res?.isSuccess && res.data?.matches) {
          this.upcomingMatches.set(res.data.matches.slice(0, 5));
        } else if (res?.data?.matches) {
          this.upcomingMatches.set(res.data.matches.slice(0, 5));
        } else {
          this.upcomingMatches.set([]);
        }
        this.matchesLoading.set(false);
      });
  }

  private loadSessions(): void {
    this.sessionsLoading.set(true);
    const filter: SessionFilterDto = {
      pageNumber: 1,
      pageSize: 5,
      teamId: this.selectedTeamId || null
    };

    this.sessionService.getCoachSessions(filter)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        catchError(() => of([]))
      )
      .subscribe(res => {
        const raw = res as any;
        const items = Array.isArray(raw) ? raw : (raw?.items || raw?.data || []);
        const formatted = items.map((s: any) => {
          let dateVal = s.sessionDate;
          if (!dateVal || dateVal.startsWith('0001') || dateVal.startsWith('0000')) {
            dateVal = s.createdAt || new Date().toISOString();
          }
          return {
            ...s,
            sessionDate: dateVal && !dateVal.endsWith('Z') && !dateVal.includes('+') ? dateVal + 'Z' : dateVal
          };
        });
        this.recentSessions.set(formatted.slice(0, 5));
        this.sessionsLoading.set(false);
      });
  }

  private loadPerformance(): void {
    if (!this.selectedTeamId) return;
    this.performanceLoading.set(true);

    this.matchService.getPostMatchAnalysis(this.selectedTeamId)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        catchError(() => of(null))
      )
      .subscribe(res => {
        if (res?.isSuccess && res.data) {
          this.performance.set(res.data);
        } else if (res?.data) {
          this.performance.set(res.data);
        } else {
          // Try treating `res` itself as the DTO (some endpoints return data directly)
          const candidate = res as any;
          if (candidate && candidate.wins !== undefined) {
            this.performance.set(candidate);
          } else {
            this.performance.set(null);
          }
        }
        this.performanceLoading.set(false);
      });
  }

  // ── Helpers ──

  getSessionStatusClass(status: any): string {
    const s = (typeof status === 'string' ? status : '').toLowerCase();
    if (s === 'completed' || status === 2) return 'completed';
    if (s === 'inprogress' || s === 'in_progress' || status === 1) return 'in-progress';
    return 'scheduled';
  }

  getSessionStatusLabel(status: any): string {
    const s = (typeof status === 'string' ? status : '').toLowerCase();
    if (s === 'completed' || status === 2) return 'Completed';
    if (s === 'inprogress' || s === 'in_progress' || status === 1) return 'In Progress';
    if (s === 'scheduled' || status === 0) return 'Scheduled';
    return status?.toString() || 'Unknown';
  }
}
