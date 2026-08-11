import { Component, OnInit, AfterViewInit, inject, ViewChild, ElementRef, ChangeDetectorRef, ChangeDetectionStrategy, DestroyRef, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Chart, registerables } from 'chart.js';
import { NavbarComponent } from '../../../../shared/components/navbar/navbar';
import { Footer } from '../../../../shared/components/footer/footer';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { PlayerProfileService } from '../../../../core/services/player/player-profile.service';
import { TokenStorageService } from '../../../../core/services/auth/token-storage.service';
import { PlayerVsAcademyModel, CategoryComparisonModel } from '../../../../core/models/Player/player-vs-academy-model';

Chart.register(...registerables);

export interface DisplayCategoryComparisonModel extends CategoryComparisonModel {
  isAbove: boolean;
  diffLabel: string;
  playerPercent: number;
  academyPercent: number;
  translatedCategoryName: string;
}

@Component({
  selector: 'app-player-academy-comparison',
  standalone: true,
  imports: [CommonModule, RouterLink, NavbarComponent, Footer, LoadingSpinnerComponent, TranslatePipe],
  templateUrl: './player-academy-comparison.component.html',
  styleUrls: ['./player-academy-comparison.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class PlayerAcademyComparisonComponent implements OnInit, AfterViewInit, OnDestroy {
  private profileService = inject(PlayerProfileService);
  private tokenStorage = inject(TokenStorageService);
  private cdr = inject(ChangeDetectorRef);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private translate = inject(TranslateService);
  private destroyRef = inject(DestroyRef);

  @ViewChild('radarCanvas') radarCanvas!: ElementRef<HTMLCanvasElement>;

  data: PlayerVsAcademyModel | null = null;
  displayCategories: DisplayCategoryComparisonModel[] = [];
  profileImageUrl: string | null = null;
  imageError = false;
  isLoading = true;
  error = '';

  playerId: number | null = null;
  loggedInUserId: number | null = null;

  aboveAvgCount = 0;
  avgRating = '0.0';
  playerInitials = '?';

  private radarChart?: Chart<'radar'>;

  ngOnInit(): void {
    const token = this.tokenStorage.getAccessToken();
    if (!token) {
      this.error = 'Not authenticated';
      this.isLoading = false;
      this.cdr.markForCheck();
      return;
    }

    const claims = this.decodeTokenPayload(token);
    if (claims) {
      this.loggedInUserId = claims.userId;
    }

    const paramId = this.route.snapshot.paramMap.get('playerId');

    if (paramId) {
      this.playerId = Number(paramId);
      this.profileService.getPlayerAcademyComparisonById(this.playerId)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: (res) => this.handleData(res),
          error: (err) => {
            this.error = err?.error?.message || this.translate.instant('PLAYER.MESSAGES.COMPARISON_LOAD_FAILED');
            this.isLoading = false;
            this.cdr.markForCheck();
          }
        });
    } else {
      this.playerId = this.loggedInUserId;
      this.profileService.getPlayerVsAcademyAverage()
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: (res) => this.handleData(res),
          error: (err) => {
            this.error = err?.error?.message || this.translate.instant('PLAYER.MESSAGES.COMPARISON_LOAD_FAILED');
            this.isLoading = false;
            this.cdr.markForCheck();
          }
        });
    }

    this.translate.onLangChange
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        if (this.data) {
          this.computeDisplayCategories();
          this.initRadarChart();
          this.cdr.markForCheck();
        }
      });
  }

  ngAfterViewInit(): void {
    if (this.data?.categories.length && !this.radarChart) {
      this.initRadarChart();
    }
  }

  ngOnDestroy(): void {
    if (this.radarChart) {
      this.radarChart.destroy();
      this.radarChart = undefined;
    }
  }

  private handleData(res: PlayerVsAcademyModel): void {
    this.data = res;
    this.isLoading = false;

    this.computeHeaderSummary();
    this.computeDisplayCategories();

    const targetId = this.playerId || this.loggedInUserId;
    if (targetId) {
      this.profileService.getPlayerProfile(targetId)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: (p) => {
            this.profileImageUrl = p.profileImageUrl || p.playerCard?.profileImageUrl || null;
            this.cdr.markForCheck();
          }
        });
    }

    this.cdr.markForCheck();

    if (res.categories.length > 0) {
      setTimeout(() => this.initRadarChart(), 0);
    }
  }

  private computeHeaderSummary(): void {
    if (!this.data) {
      this.aboveAvgCount = 0;
      this.avgRating = '0.0';
      this.playerInitials = '?';
      return;
    }

    this.aboveAvgCount = this.data.categories.filter(c => c.difference > 0).length;

    if (this.data.categories.length > 0) {
      const sum = this.data.categories.reduce((s, c) => s + c.playerAverage, 0);
      this.avgRating = (sum / this.data.categories.length).toFixed(1);
    } else {
      this.avgRating = '0.0';
    }

    const parts = this.data.playerName.trim().split(/\s+/);
    if (parts.length >= 2) {
      this.playerInitials = (parts[0][0] + parts[parts.length - 1][0]).toUpperCase();
    } else {
      this.playerInitials = (parts[0]?.[0] ?? '?').toUpperCase();
    }
  }

  private computeDisplayCategories(): void {
    if (!this.data) {
      this.displayCategories = [];
      return;
    }

    this.displayCategories = this.data.categories.map(c => {
      const isAbove = c.difference >= 0;
      let diffLabel = `= ${c.difference.toFixed(1)}`;
      if (c.difference > 0) diffLabel = `▲ +${c.difference.toFixed(1)}`;
      else if (c.difference < 0) diffLabel = `▼ ${c.difference.toFixed(1)}`;

      const key = 'PLAYER.CAT_' + c.categoryName.toUpperCase();
      const translated = this.translate.instant(key);

      return {
        ...c,
        isAbove,
        diffLabel,
        playerPercent: this.clampPercent(c.playerAverage),
        academyPercent: this.clampPercent(c.academyAverage),
        translatedCategoryName: translated !== key ? translated : c.categoryName,
      };
    });
  }

  goToProfile(): void {
    if (this.playerId) {
      this.router.navigate(['/player/profile', this.playerId]);
    } else {
      this.router.navigate(['/player/profile']);
    }
  }

  private initRadarChart(): void {
    if (!this.data || !this.radarCanvas?.nativeElement) return;
    if (this.data.categories.length <= 1) return;

    if (this.radarChart) {
      this.radarChart.destroy();
      this.radarChart = undefined;
    }

    const labels = this.data.categories.map(c => {
      const key = 'PLAYER.CAT_' + c.categoryName.toUpperCase();
      const translated = this.translate.instant(key);
      return translated !== key ? translated : c.categoryName;
    });

    const playerData = this.data.categories.map(c => c.playerAverage);
    const academyData = this.data.categories.map(c => c.academyAverage);

    const ctx = this.radarCanvas.nativeElement.getContext('2d');
    if (!ctx) return;

    const isMobile = window.innerWidth <= 600;

    this.radarChart = new Chart(ctx, {
      type: 'radar',
      data: {
        labels,
        datasets: [
          {
            label: this.data.playerName,
            data: playerData,
            backgroundColor: 'rgba(255, 215, 0, 0.15)',
            borderColor: '#ffd700',
            borderWidth: isMobile ? 2 : 2.5,
            pointBackgroundColor: '#ffd700',
            pointRadius: isMobile ? 3 : 4
          },
          {
            label: this.translate.instant('PLAYER.ACADEMY_AVG'),
            data: academyData,
            backgroundColor: 'rgba(0, 229, 255, 0.1)',
            borderColor: '#00e5ff',
            borderWidth: isMobile ? 1.5 : 2,
            borderDash: [4, 4],
            pointBackgroundColor: '#00e5ff',
            pointRadius: isMobile ? 2.5 : 3
          }
        ]
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: { legend: { display: false } },
        scales: {
          r: {
            angleLines: { color: 'rgba(255, 255, 255, 0.08)' },
            grid: { color: 'rgba(255, 255, 255, 0.08)' },
            pointLabels: {
              color: '#808a9d',
              font: { size: isMobile ? 9 : 11, weight: 'bold', family: 'Inter' },
              padding: isMobile ? 3 : 8
            },
            ticks: { display: false },
            suggestedMin: 40,
            suggestedMax: 90
          }
        }
      }
    });
  }

  isAboveAvg(cat: CategoryComparisonModel): boolean {
    return cat.difference >= 0;
  }

  categoryDiffLabel(cat: CategoryComparisonModel): string {
    if (cat.difference > 0) return `▲ +${cat.difference.toFixed(1)}`;
    if (cat.difference < 0) return `▼ ${cat.difference.toFixed(1)}`;
    return `= ${cat.difference.toFixed(1)}`;
  }

  clampPercent(value: number): number {
    return Math.min(Math.max(value, 0), 100);
  }

  private decodeTokenPayload(token: string): { userId: number; academyId: number | null } | null {
    try {
      const parts = token.split('.');
      if (parts.length !== 3) return null;
      let payload = parts[1].replace(/-/g, '+').replace(/_/g, '/');
      while (payload.length % 4) payload += '=';
      const decoded = JSON.parse(atob(payload));

      const userId = parseInt(
        decoded['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'] ?? '0', 10
      );

      const academyIdRaw = decoded['AcademyId'] ?? decoded['academyId'];
      const academyId = academyIdRaw ? parseInt(academyIdRaw, 10) : null;

      return { userId, academyId };
    } catch {
      return null;
    }
  }
}
