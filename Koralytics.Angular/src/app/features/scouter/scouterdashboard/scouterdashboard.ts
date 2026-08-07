import { Component, OnInit, inject, signal, computed, DestroyRef, ViewChild, ElementRef, Injector, afterNextRender } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, ActivatedRoute } from '@angular/router';
import { forkJoin } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpErrorResponse } from '@angular/common/http';
import {
  Chart,
  BarController,
  BarElement,
  LinearScale,
  CategoryScale,
  Tooltip,
} from 'chart.js';

import { ScouterService } from '../../../../core/services/Scouter/scouter.service';
import { PlayerCardDto, ScouterProfileDto } from '../../../../core/interfaces/Scouter.interfaces';
import { ToastService } from '../../../../core/services/Toast/toast';
// Confirmed real path (used the same way in player-scouter-views.component.ts).
import { TokenStorageService } from '../../../../core/services/auth/token-storage.service';
import { extractErrorMessage } from '../../../../core/utils/http-error.util';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner';
import { EmptyStateComponent } from '../../../../shared/components/empty-state/empty-state';
import { StatusChipComponent } from '../../../../shared/components/status-chip/status-chip';
import { CustomButtonComponent } from '../../../../shared/components/custom-button/custom-button';
import { ScrollRevealDirective } from '@shared/directives/scroll-reveal.directive';
import { NavbarComponent } from '@shared/components/navbar/navbar';
import { Footer } from '@shared/components/footer/footer';
import { ToastContainerComponent } from '@shared/components/toast/toast';

Chart.register(BarController, BarElement, LinearScale, CategoryScale, Tooltip);

const SAMPLE_SIZE = 50;
const PREVIEW_COUNT = 6;

const ELITE_THRESHOLD = 85;
const STRONG_THRESHOLD = 75;

interface PositionBreakdownEntry {
  position: string;
  count: number;
  pct: number;
}

interface RatingTierEntry {
  label: string;
  count: number;
  pct: number;
  colorVar: string;
}

interface DashboardInsight {
  icon: 'star' | 'target' | 'trophy' | 'alert' | 'trend';
  tone: 'info' | 'success' | 'warning';
  text: string;
}

@Component({
  selector: 'app-scouter-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    LoadingSpinnerComponent,
    EmptyStateComponent,
    StatusChipComponent,
    CustomButtonComponent,
    ScrollRevealDirective,
    NavbarComponent,
    Footer
  ],
  templateUrl: './scouterdashboard.html',
  styleUrls: ['./scouterdashboard.css'],
})
export class ScouterDashboardComponent implements OnInit {
  private readonly scouterService = inject(ScouterService);
  private readonly toastService = inject(ToastService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly tokenStorage = inject(TokenStorageService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly injector = inject(Injector);

  @ViewChild('positionChartCanvas') positionChartCanvas?: ElementRef<HTMLCanvasElement>;
  private chart: Chart | null = null;

  isLoading = signal<boolean>(true);
  errorMessage = signal<string | null>(null);

  profile = signal<ScouterProfileDto | null>(null);


  isOwnProfile = signal<boolean>(true);
  viewedScouterId = signal<number | null>(null);

  viewedFollowedCount = signal<number | null>(null);

  pageTitle = computed<string>(() => {
    if (this.isOwnProfile()) return 'Scouter Dashboard';
    const name = this.scouterFullName();
    return name ? `${name}'s Scouter Profile` : 'Scouter Profile';
  });

  pageSubtitle = computed<string>(() =>
    this.isOwnProfile()
      ? 'Your complete overview of tracked talents, saved shortlists, and latest evaluations.'
      : 'Scouter verification status and profile overview.'
  );

  // Insights shown to any role visiting *another* scouter's dashboard.
  // Deliberately built only from the fields actually on ScouterProfileDto
  // (id, firstName, lastName, isVerified, verifiedAt) -- the private
  // shortlist/followed-player data used for the owner's own insights above
  // isn't fetched or shown here, both because it isn't this viewer's data
  // to see and because it isn't confirmed to be accessible for a scouter
  // other than the caller themselves.
  viewerInsights = computed<DashboardInsight[]>(() => {
    const p = this.profile();
    if (!p || this.isOwnProfile()) return [];

    const name = this.scouterFullName() || 'This scouter';
    const out: DashboardInsight[] = [];

    if (p.isVerified) {
      out.push({
        icon: 'trophy',
        tone: 'success',
        text: `${name} is a verified scouter with full access to the Koralytics scouting network.`,
      });

      const verifiedDate = p.verifiedAt ? new Date(p.verifiedAt) : null;
      if (verifiedDate && !isNaN(verifiedDate.getTime())) {
        out.push({
          icon: 'star',
          tone: 'info',
          text: `Verified on ${verifiedDate.toLocaleDateString('en-US', { month: 'long', day: 'numeric', year: 'numeric' })}.`,
        });
      }
    } else {
      out.push({
        icon: 'alert',
        tone: 'warning',
        text: `${name}'s account is still pending verification by a system admin.`,
      });
    }

    out.push({
      icon: 'target',
      tone: 'info',
      text: `Scouter ID #${p.id} -- part of the Koralytics scouting network.`,
    });

    const followedCount = this.viewedFollowedCount();
    if (followedCount !== null) {
      out.push({
        icon: 'trend',
        tone: 'info',
        text: `${name} is currently following ${followedCount} player${followedCount === 1 ? '' : 's'}.`,
      });
    }

    return out;
  });

  // ScouterProfileDto has firstName/lastName, not a combined fullName --
  // build the display name once here rather than repeating the
  // concatenation everywhere it's needed.
  scouterFullName = computed<string>(() => {
    const p = this.profile();
    if (!p) return '';
    return `${p.firstName ?? ''} ${p.lastName ?? ''}`.trim();
  });

  followedSample = signal<PlayerCardDto[]>([]);
  followedTotal = signal<number>(0);
  shortlistSample = signal<PlayerCardDto[]>([]);
  shortlistTotal = signal<number>(0);

  previewFollowed = computed(() => this.followedSample().slice(0, PREVIEW_COUNT));
  previewShortlist = computed(() => this.shortlistSample().slice(0, PREVIEW_COUNT));

  avgShortlistRating = computed<number>(() => {
    const list = this.shortlistSample();
    if (!list.length) return 0;
    const sum = list.reduce((acc, p) => acc + (p.overallRating || 0), 0);
    return Math.round((sum / list.length) * 10) / 10;
  });

  eliteProspectCount = computed<number>(
    () => this.shortlistSample().filter((p) => p.overallRating >= ELITE_THRESHOLD).length
  );

  positionBreakdown = computed<PositionBreakdownEntry[]>(() => {
    const list = this.shortlistSample();
    if (!list.length) return [];

    const counts = new Map<string, number>();
    for (const p of list) {
      const pos = p.position || 'Unknown';
      counts.set(pos, (counts.get(pos) || 0) + 1);
    }

    const total = list.length;
    return Array.from(counts.entries())
      .map(([position, count]) => ({ position, count, pct: Math.round((count / total) * 100) }))
      .sort((a, b) => b.count - a.count);
  });

  ratingTiers = computed<RatingTierEntry[]>(() => {
    const list = this.shortlistSample();
    const total = list.length || 1;
    const elite = list.filter((p) => p.overallRating >= ELITE_THRESHOLD).length;
    const strong = list.filter((p) => p.overallRating >= STRONG_THRESHOLD && p.overallRating < ELITE_THRESHOLD).length;
    const developing = list.length - elite - strong;

    return [
      { label: `Elite (${ELITE_THRESHOLD}+)`, count: elite, pct: Math.round((elite / total) * 100), colorVar: '--tier-elite' },
      { label: `Strong (${STRONG_THRESHOLD}-${ELITE_THRESHOLD - 1})`, count: strong, pct: Math.round((strong / total) * 100), colorVar: '--tier-strong' },
      { label: 'Developing', count: developing, pct: Math.round((developing / total) * 100), colorVar: '--tier-developing' },
    ];
  });

  // Auto-generated, human-readable observations from the shortlist data --
  // this is the "smart" part of the dashboard: it doesn't just show raw
  // numbers, it surfaces what they mean (concentration risk, standout
  // prospects, coverage gaps) the way a scouting director's summary would.
  insights = computed<DashboardInsight[]>(() => {
    const list = this.shortlistSample();
    if (list.length === 0) return [];

    const out: DashboardInsight[] = [];

    const top = [...list].sort((a, b) => (b.overallRating || 0) - (a.overallRating || 0))[0];
    if (top) {
      out.push({
        icon: 'star',
        tone: 'success',
        text: `${top.playerName} is your highest-rated shortlisted prospect at ${top.overallRating} OVR.`,
      });
    }

    const elite = this.eliteProspectCount();
    if (elite > 0) {
      out.push({
        icon: 'trophy',
        tone: 'success',
        text: `${elite} elite-tier prospect${elite === 1 ? '' : 's'} (${ELITE_THRESHOLD}+ OVR) currently in your shortlist.`,
      });
    }

    const positions = this.positionBreakdown();
    const topPos = positions[0];
    if (topPos && positions.length > 1 && topPos.pct >= 40) {
      out.push({
        icon: 'target',
        tone: 'info',
        text: `${topPos.pct}% of your shortlist plays ${topPos.position} -- your recent scouting has leaned heavily toward this position.`,
      });
    }

    const hasGoalkeeper = list.some((p) => p.position === 'GK');
    if (!hasGoalkeeper && list.length >= 5) {
      out.push({
        icon: 'alert',
        tone: 'warning',
        text: `No goalkeepers in your shortlist yet -- worth checking whether that's intentional or a coverage gap.`,
      });
    }

    return out.slice(0, 4);
  });

  ngOnInit(): void {
    // Support both "scouterId" and "id" as the param name since the exact
    // route segment used to link here (e.g. from the player-scouter-views
    // page) wasn't confirmed against app.routes.ts.
    const idParam = this.route.snapshot.paramMap.get('scouterId') ?? this.route.snapshot.paramMap.get('id');

    if (!idParam) {
      this.loadDashboard();
      return;
    }

    const routeScouterId = Number(idParam);
    const currentUser = this.tokenStorage.getUser();
    const currentUserIsScouter = !!currentUser?.roles?.some((r: string) => r.toLowerCase() === 'scouter');

    if (currentUserIsScouter) {
      if (currentUser?.userId === routeScouterId) {
        // A scouter following a link back to their own profile that
        // happens to include their own id -- just load their own
        // dashboard normally rather than calling getScouterById, which
        // the backend rejects for the Scouter role entirely (see below).
        this.loadDashboard();
      } else {
        // getScouterById is documented as SystemAdmin/Player/Parent only --
        // NOT callable by a Scouter, even to look up a different scouter.
        // Calling it here would just 403; skip the request and show a
        // clear message instead of a generic "failed to load" banner.
        this.isOwnProfile.set(false);
        this.viewedScouterId.set(routeScouterId);
        this.isLoading.set(false);
        this.errorMessage.set("Scouters can't view other scouters' profiles.");
      }
      return;
    }

    this.isOwnProfile.set(false);
    this.viewedScouterId.set(routeScouterId);
    this.loadOtherScouterProfile(routeScouterId);
  }

  private loadOtherScouterProfile(scouterId: number): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);
    this.viewedFollowedCount.set(null);

    this.scouterService
      .getScouterById(scouterId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (profile) => {
          this.profile.set(profile);
          this.isLoading.set(false);
          // Followed players, shortlist, KPIs, insights, and the position/
          // rating charts are this scouter's own private scouting data --
          // intentionally not fetched or shown when another role is just
          // viewing this scouter's public profile. The one exception is a
          // simple follow *count*, fetched best-effort below.
          this.loadViewedFollowedCount(scouterId);
        },
        error: (err: HttpErrorResponse) => {
          this.errorMessage.set(extractErrorMessage(err, 'Failed to load this scouter profile.'));
          this.isLoading.set(false);
        },
      });
  }

  private loadViewedFollowedCount(scouterId: number): void {
    // Backend: [Authorize(Roles = "Scouter,SystemAdmin")] plus an ownership
    // check on top -- so this can only ever succeed for a SystemAdmin
    // caller (a Scouter caller would fail the ownership check against a
    // different scouter's id, and Player/Parent aren't in the role list at
    // all). Skip the request entirely for any other role instead of firing
    // a call that's guaranteed to 401/403.
    const currentUser = this.tokenStorage.getUser();
    const isSystemAdmin = !!currentUser?.roles?.some((r: string) => r.toLowerCase() === 'systemadmin');
    if (!isSystemAdmin) return;

    this.scouterService
      .getFollowedPlayers(scouterId, 1, 1)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (res) => this.viewedFollowedCount.set(res.totalCount),
        error: () => this.viewedFollowedCount.set(null),
      });
  }

  loadDashboard(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    // getMyProfile() resolves the current scouter's id from auth claims
    // server-side, so there's no need to decode the JWT client-side here
    // (unlike a couple of the other scouter pages, which do that because
    // they also support an admin viewing someone else's data via a
    // :scouterId route param -- this dashboard is always "my own").
    this.scouterService
      .getMyProfile()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (profile) => {
          this.profile.set(profile);
          this.loadLists(profile.id);
        },
        error: (err: HttpErrorResponse) => {
          this.errorMessage.set(extractErrorMessage(err, 'Failed to load your scouter profile.'));
          this.isLoading.set(false);
        },
      });
  }

  private loadLists(scouterId: number): void {
    forkJoin({
      followed: this.scouterService.getFollowedPlayers(scouterId, 1, SAMPLE_SIZE),
      shortlist: this.scouterService.getShortlist(scouterId, 1, SAMPLE_SIZE),
    })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: ({ followed, shortlist }) => {
          this.followedSample.set(followed.items);
          this.followedTotal.set(followed.totalCount);
          this.shortlistSample.set(shortlist.items);
          this.shortlistTotal.set(shortlist.totalCount);
          this.isLoading.set(false);

          // setTimeout(0) is a guess about DOM timing that isn't reliable
          // with Angular's signal-based change detection -- the canvas
          // isn't guaranteed to exist in the DOM yet when that macrotask
          // fires, and renderPositionChart()'s ViewChild guard just
          // silently no-ops if it's not there yet (no retry, blank chart
          // forever). afterNextRender waits for the actual DOM update.
          afterNextRender(() => this.renderPositionChart(), { injector: this.injector });
        },
        error: (err: HttpErrorResponse) => {
          this.errorMessage.set(extractErrorMessage(err, 'Failed to load your scouting lists.'));
          this.isLoading.set(false);
        },
      });
  }

  private renderPositionChart(): void {
    if (!this.positionChartCanvas) return;
    const data = this.positionBreakdown();
    if (data.length === 0) return;

    if (this.chart) {
      this.chart.destroy();
    }

    const ctx = this.positionChartCanvas.nativeElement.getContext('2d');
    if (!ctx) return;

    this.chart = new Chart(ctx, {
      type: 'bar',
      data: {
        labels: data.map((d) => d.position),
        datasets: [
          {
            label: 'Shortlisted players',
            data: data.map((d) => d.count),
            backgroundColor: '#00f2fe',
            borderRadius: 6,
            maxBarThickness: 28,
          },
        ],
      },
      options: {
        indexAxis: 'y',
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
          legend: { display: false },
          tooltip: {
            backgroundColor: '#0c111c',
            titleColor: '#f2f3f5',
            bodyColor: '#00f2fe',
            borderColor: 'rgba(255, 255, 255, 0.08)',
            borderWidth: 1,
            padding: 10,
          },
        },
        scales: {
          x: {
            beginAtZero: true,
            ticks: { stepSize: 1, color: '#8b909a', font: { family: 'Inter' } },
            grid: { color: 'rgba(255, 255, 255, 0.05)' },
          },
          y: {
            ticks: { color: '#e2e8f0', font: { family: 'Inter', weight: 600 } },
            grid: { display: false },
          },
        },
      },
    });
  }

  getInitials(name?: string): string {
    if (!name) return 'SC';
    const parts = name.trim().split(' ');
    if (parts.length >= 2) return (parts[0][0] + parts[1][0]).toUpperCase();
    return name.substring(0, 2).toUpperCase();
  }



  goToFollowedPlayers(): void {
    this.router.navigate(['/followed-players', this.profile()?.id]);
  }

  goToShortlist(): void {
    this.router.navigate(['/shortlist', this.profile()?.id]);
  }

  viewPlayerProfile(playerId: number): void {
    this.router.navigate(['/player/profile', playerId]);
  }
  goToSearch(): void {
    this.router.navigate(['/search']);
  }
}