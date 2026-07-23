import { Component, computed, DestroyRef, ElementRef, inject, OnInit, signal, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { ScouterService } from '../../../../core/services/Scouter/scouter.service';
import { PlayerProfileViewAnalyticsDto, ProfileViewerDetailDto } from '../../../../core/interfaces/Scouter.interfaces';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpErrorResponse } from '@angular/common/http';
import { extractErrorMessage } from '../../../../core/utils/http-error.util';
import {
  Chart,
  LineController,
  LineElement,
  PointElement,
  LinearScale,
  CategoryScale,
  Title,
  Tooltip,
  Legend,
  Filler,
} from 'chart.js';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner';
import { EmptyStateComponent } from '../../../../shared/components/empty-state/empty-state';
import { NavbarComponent } from '../../../../shared/components/navbar/navbar';
import { ScrollRevealDirective } from '../../../../shared/directives/scroll-reveal.directive';


Chart.register(
  LineController,
  LineElement,
  PointElement,
  LinearScale,
  CategoryScale,
  Title,
  Tooltip,
  Legend,
  Filler
);


Chart.register(
  LineController,
  LineElement,
  PointElement,
  LinearScale,
  CategoryScale,
  Title,
  Tooltip,
  Legend,
  Filler
);

@Component({
  selector: 'app-profile-views-analytics',
  imports: [
    CommonModule,
    LoadingSpinnerComponent,
    EmptyStateComponent,
    NavbarComponent,
    ScrollRevealDirective,
  ],
  templateUrl: './profile-views-analytics.html',
  styleUrl: './profile-views-analytics.css',
})
export class ProfileViewsAnalytics implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly scouterService = inject(ScouterService);
  private readonly destroyRef = inject(DestroyRef);
  @ViewChild('monthlyChartCanvas') monthlyChartCanvas!: ElementRef<HTMLCanvasElement>;

  playerId = signal<number>(0);
  analytics = signal<PlayerProfileViewAnalyticsDto | null>(null);
  isLoading = signal<boolean>(true);
  errorMessage = signal<string | null>(null);
  chart: Chart | null = null;

  
  uniqueRecentScouters = computed<number>(() => {
    const views = this.analytics()?.recentViews ?? [];
    return new Set(views.map((v) => v.scouterId)).size;
  });

  recentViewsThisWeek = computed<number>(() => {
    const views = this.analytics()?.recentViews ?? [];
    const sevenDaysAgo = Date.now() - 7 * 24 * 60 * 60 * 1000;
    return views.filter((v) => new Date(v.viewedAt).getTime() >= sevenDaysAgo).length;
  });

  constructor() {
    
    this.destroyRef.onDestroy(() => this.chart?.destroy());
  }

  ngOnInit(): void {
    const idParam = this.route.snapshot.paramMap.get('playerId');
    if (idParam) {
      this.playerId.set(+idParam);
      this.loadAnalytics();
    } else {
      this.errorMessage.set('Player ID is missing from the route.');
      this.isLoading.set(false);
    }
  }

  loadAnalytics(): void {
    if (!this.playerId()) return;

    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.scouterService
      .getProfileViewsAnalytics(this.playerId())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (data) => {
          this.analytics.set(data);
          this.isLoading.set(false);

          setTimeout(() => this.renderViewsChart(), 0);
        },
        error: (err: HttpErrorResponse) => {
          this.errorMessage.set(extractErrorMessage(err, 'Failed to load profile views analytics.'));
          this.isLoading.set(false);
        },
      });
  }

  private renderViewsChart(): void {
    if (!this.monthlyChartCanvas || !this.analytics()?.recentViews) return;

    if (this.chart) {
      this.chart.destroy();
    }

    const recentViews = this.analytics()!.recentViews;
    const dateCountsMap = new Map<string, number>();

    recentViews.forEach((v: ProfileViewerDetailDto) => {
      const dateStr = new Date(v.viewedAt).toLocaleDateString('en-US', {
        month: 'short',
        day: 'numeric',
      });
      dateCountsMap.set(dateStr, (dateCountsMap.get(dateStr) || 0) + 1);
    });

    const labels = Array.from(dateCountsMap.keys()).reverse();
    const counts = Array.from(dateCountsMap.values()).reverse();

    const ctx = this.monthlyChartCanvas.nativeElement.getContext('2d');
    if (!ctx) return;

    this.chart = new Chart(ctx, {
      type: 'line',
      data: {
        labels: labels,
        datasets: [
          {
            label: 'Views',
            data: counts,
            borderColor: '#c8ff4d',
            backgroundColor: 'rgba(200, 255, 77, 0.08)',
            fill: true,
            tension: 0.35,
            pointBackgroundColor: '#c8ff4d',
            pointBorderColor: '#0a0c0f',
            pointRadius: 4,
            pointHoverRadius: 6,
          },
        ],
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
          legend: { display: false },
          tooltip: {
            mode: 'index',
            intersect: false,
            backgroundColor: '#0d0f14',
            titleColor: '#f2f3f5',
            bodyColor: '#c8ff4d',
            borderColor: '#1e232d',
            borderWidth: 1,
            padding: 10,
          },
        },
        scales: {
          y: {
            beginAtZero: true,
            ticks: { stepSize: 1, color: '#808897', font: { family: 'Inter' } },
            grid: { color: '#1e232d' },
          },
          x: {
            ticks: { color: '#808897', font: { family: 'Inter' } },
            grid: { display: false },
          },
        },
      },
    });
  }
}