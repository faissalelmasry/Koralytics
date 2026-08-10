import { Component, OnInit, OnDestroy, inject, signal, computed, ElementRef, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { NavbarComponent } from '../../../../../shared/components/navbar/navbar';
import { Footer } from '../../../../../shared/components/footer/footer';
import { LoadingSpinnerComponent } from '../../../../../shared/components/loading-spinner/loading-spinner';
import { ScrollRevealDirective } from '../../../../../shared/directives/scroll-reveal.directive';
import { AuthService } from '../../../../../core/services/auth/auth.service';
import { CoachSquadService } from '../../../../../core/services/coach/coach-squad.service';
import { MatchService, PostMatchAnalysisResponseDto } from '../../../../../core/services/match/match.service';
import { ProfileService } from '../../../../../core/services/profile/profile.service';
import { CoachTeamDto, SquadPlayerDto } from '../../../../../core/interfaces/coach.interfaces';
import {
  BaseUserProfileResponse,
  CoachProfileResponse
} from '../../../../../core/models/profile/profile.models';
import { MiniPlayerCardComponent } from '../../../match/mini-player-card/mini-player-card.component';
import { MiniPlayerCardModel } from '../../../../../core/models/Player/mini-player-card-model';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';

@Component({
  selector: 'app-coach-profile',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    LoadingSpinnerComponent,
    ScrollRevealDirective,
    MiniPlayerCardComponent,
    TranslatePipe
  ],
  templateUrl: './coach-profile.component.html',
  styleUrls: ['./coach-profile.component.css']
})
export class CoachProfileComponent implements OnInit, OnDestroy {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private authService = inject(AuthService);
  private squadService = inject(CoachSquadService);
  private matchService = inject(MatchService);
  private profileService = inject(ProfileService);
  private destroyRef = inject(DestroyRef);
  private translate = inject(TranslateService);

  @ViewChild('countersSection') countersSection!: ElementRef<HTMLElement>;

  // ── State ──
  isLoading = true;
  imageError = false;
  isOwnProfile = false;
  coachId: number | null = null;
  error: string | null = null;

  // ── Profile Data ──
  profile: CoachProfileResponse | null = null;

  // ── Teams & Squad ──
  teams = signal<CoachTeamDto[]>([]);
  selectedTeamId = 0;
  squadPlayers = signal<SquadPlayerDto[]>([]);
  squadLoading = signal(false);
  expandedTeams = new Set<number>();

  // ── All team squads (for profile summary) ──
  teamSquads = new Map<number, SquadPlayerDto[]>();

  // ── Performance ──
  performance = signal<PostMatchAnalysisResponseDto | null>(null);
  performanceLoading = signal(false);

  // ── Computed ──
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

  totalMatchesPlayed = computed(() => {
    const p = this.performance();
    return p ? p.wins + p.losses + p.draws : 0;
  });

  winRate = computed(() => {
    const total = this.totalMatchesPlayed();
    if (total === 0) return 0;
    return (this.performance()!.wins / total) * 100;
  });

  goalDifference = computed(() => {
    const p = this.performance();
    if (!p) return 0;
    return p.goalsFor - p.goalsAgainst;
  });

  // ── Counter animation ──
  animatedCounters = { teams: 0, players: 0, matches: 0, winPct: 0 };
  private countersAnimated = false;
  private observer?: IntersectionObserver;

  get initials(): string {
    if (!this.profile) return 'C';
    return `${this.profile.firstName?.charAt(0) || ''}${this.profile.lastName?.charAt(0) || ''}`.toUpperCase();
  }

  ngOnInit(): void {
    this.route.paramMap.subscribe(params => {
      const idParam = params.get('coachId');
      const currentUser = this.authService.getCurrentUserSync();

      if (idParam) {
        this.coachId = Number(idParam);
        this.isOwnProfile = currentUser?.userId === this.coachId;
      } else {
        this.coachId = currentUser?.userId ?? null;
        this.isOwnProfile = true;
      }

      if (this.coachId) {
        this.loadCoachData();
      } else {
        this.error = this.translate.instant('COACH.MESSAGES.NO_ID_FOUND');
        this.isLoading = false;
      }
    });
  }

  ngOnDestroy(): void {
    this.observer?.disconnect();
  }

  private loadCoachData(): void {
    this.isLoading = true;

    const profile$ = this.isOwnProfile
      ? this.profileService.getMyProfile().pipe(catchError(() => of(null)))
      : this.profileService.getProfileById(this.coachId!).pipe(catchError(() => of(null)));

    const teams$ = this.squadService.getCoachTeams(this.coachId ?? undefined)
      .pipe(catchError(() => of([] as CoachTeamDto[])));

    forkJoin({ profile: profile$, teams: teams$ })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: ({ profile, teams }) => {
          if (profile && (profile as any).isSuccess && (profile as any).data) {
            this.profile = (profile as any).data as CoachProfileResponse;
          } else if (profile && (profile as any).data) {
            this.profile = (profile as any).data as CoachProfileResponse;
          }
          this.teams.set(teams);
          if (teams.length > 0) {
            this.selectedTeamId = teams[0].teamId;
            this.loadSquadForTeam(teams[0].teamId);
            this.loadPerformance(teams[0].teamId);
            this.loadAllTeamSquads(teams);
          }
          this.isLoading = false;
          this.setupCounterAnimation();
        },
        error: () => {
          this.error = this.translate.instant('COACH.MESSAGES.LOAD_PROFILE_FAILED');
          this.isLoading = false;
        }
      });
  }

  loadSquadForTeam(teamId: number): void {
    this.squadLoading.set(true);
    this.selectedTeamId = teamId;

    this.squadService.getSquad(teamId, this.coachId ?? undefined)
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

  private loadAllTeamSquads(teams: CoachTeamDto[]): void {
    teams.forEach(team => {
      this.squadService.getSquad(team.teamId, this.coachId ?? undefined)
        .pipe(
          takeUntilDestroyed(this.destroyRef),
          catchError(() => of(null))
        )
        .subscribe(data => {
          if (data?.players) {
            this.teamSquads.set(team.teamId, data.players);
          }
        });
    });
  }

  loadPerformance(teamId: number): void {
    this.performanceLoading.set(true);

    this.matchService.getPostMatchAnalysis(teamId)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        catchError(() => of(null))
      )
      .subscribe(res => {
        if (res && (res as any).isSuccess && (res as any).data) {
          this.performance.set((res as any).data);
        } else if (res && (res as any).data) {
          this.performance.set((res as any).data);
        } else {
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

  onTeamChange(event: Event): void {
    const select = event.target as HTMLSelectElement;
    const teamId = +select.value;
    this.selectedTeamId = teamId;
    this.loadSquadForTeam(teamId);
    this.loadPerformance(teamId);
  }

  toggleTeamExpand(teamId: number): void {
    if (this.expandedTeams.has(teamId)) {
      this.expandedTeams.delete(teamId);
    } else {
      this.expandedTeams.add(teamId);
      // Load squad if not cached
      if (!this.teamSquads.has(teamId)) {
        this.squadService.getSquad(teamId, this.coachId ?? undefined)
          .pipe(
            takeUntilDestroyed(this.destroyRef),
            catchError(() => of(null))
          )
          .subscribe(data => {
            if (data?.players) {
              this.teamSquads.set(teamId, data.players);
            }
          });
      }
    }
  }

  isTeamExpanded(teamId: number): boolean {
    return this.expandedTeams.has(teamId);
  }

  getTeamPlayers(teamId: number): SquadPlayerDto[] {
    return this.teamSquads.get(teamId) || [];
  }

  getTeamPlayerCount(teamId: number): number {
    return this.teamSquads.get(teamId)?.length || 0;
  }

  getTotalManagedPlayers(): number {
    let total = 0;
    this.teamSquads.forEach(players => total += players.length);
    return total;
  }

  navigateToPlayer(playerId: number): void {
    this.router.navigate(['/player/profile', playerId]);
  }

  getPlayerRatingClass(rating: number): string {
    if (rating >= 85) return 'elite';
    if (rating >= 75) return 'great';
    if (rating >= 65) return 'good';
    if (rating >= 50) return 'average';
    return 'developing';
  }

  getAvailabilityClass(status: string): string {
    if (!status) return 'unknown';
    const s = status.toLowerCase();
    if (s === 'available') return 'available';
    if (s === 'injured') return 'injured';
    if (s === 'suspended') return 'suspended';
    return 'unknown';
  }

  getResultClass(result: string): string {
    if (!result) return '';
    const r = result.toLowerCase();
    if (r === 'win' || r === 'w') return 'win';
    if (r === 'loss' || r === 'l') return 'loss';
    if (r === 'draw' || r === 'd') return 'draw';
    return '';
  }

  // ── Counter animation ──
  private setupCounterAnimation(): void {
    setTimeout(() => {
      if (this.countersSection) {
        this.observer = new IntersectionObserver(
          entries => {
            if (entries[0].isIntersecting && !this.countersAnimated) {
              this.countersAnimated = true;
              this.animateCounters();
            }
          },
          { threshold: 0.3 }
        );
        this.observer.observe(this.countersSection.nativeElement);
      } else {
        // If no countersSection ref, animate immediately
        this.animateCounters();
      }
    }, 100);
  }

  private animateCounters(): void {
    const targets = {
      teams: this.teams().length,
      players: this.getTotalManagedPlayers(),
      matches: this.totalMatchesPlayed(),
      winPct: Math.round(this.winRate())
    };

    const duration = 1500;
    const steps = 60;
    const interval = duration / steps;
    let step = 0;

    const timer = setInterval(() => {
      step++;
      const progress = Math.min(step / steps, 1);
      const eased = 1 - Math.pow(1 - progress, 3); // ease-out cubic

      this.animatedCounters = {
        teams: Math.round(eased * targets.teams),
        players: Math.round(eased * targets.players),
        matches: Math.round(eased * targets.matches),
        winPct: Math.round(eased * targets.winPct)
      };

      if (step >= steps) {
        clearInterval(timer);
        this.animatedCounters = targets;
      }
    }, interval);
  }

  mapSquadPlayerToMiniCard(player: SquadPlayerDto): MiniPlayerCardModel {
    return {
      playerId: player.playerId,
      fullName: player.fullName,
      position: player.primaryPosition || 'N/A',
      naturalPosition: player.primaryPosition || 'N/A',
      profileImageUrl: player.profileImageUrl ?? null,
      overallRating: player.overallRating || 0
    };
  }
}
