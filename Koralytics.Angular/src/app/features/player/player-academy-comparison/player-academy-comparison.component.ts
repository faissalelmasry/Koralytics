import { Component, OnInit, AfterViewInit, inject, ViewChild, ElementRef, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Chart, registerables } from 'chart.js';
import { NavbarComponent } from '../../../../shared/components/navbar/navbar';
import { Footer } from '../../../../shared/components/footer/footer';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner';
import { PlayerProfileService } from '../../../../core/services/player/player-profile.service';
import { TokenStorageService } from '../../../../core/services/auth/token-storage.service';
import { PlayerVsAcademyModel, CategoryComparisonModel } from '../../../../core/models/Player/player-vs-academy-model';

Chart.register(...registerables);

@Component({
  selector: 'app-player-academy-comparison',
  standalone: true,
  imports: [CommonModule, NavbarComponent, Footer, LoadingSpinnerComponent],
  templateUrl: './player-academy-comparison.component.html',
  styleUrls: ['./player-academy-comparison.component.css']
})
export class PlayerAcademyComparisonComponent implements OnInit, AfterViewInit {

  private profileService = inject(PlayerProfileService);
  private tokenStorage = inject(TokenStorageService);
  private cdr = inject(ChangeDetectorRef);

  @ViewChild('radarCanvas') radarCanvas!: ElementRef<HTMLCanvasElement>;

  data: PlayerVsAcademyModel | null = null;
  isLoading = true;
  error = '';

  private radarChart?: Chart<'radar'>;

  get aboveAvgCount(): number {
    return this.data?.categories.filter(c => c.difference > 0).length ?? 0;
  }

  get avgRating(): string {
    if (!this.data?.categories.length) return '0.0';
    return (this.data.categories.reduce((s, c) => s + c.playerAverage, 0) / this.data.categories.length).toFixed(1);
  }

  ngOnInit(): void {
    const token = this.tokenStorage.getAccessToken();
    if (!token) {
      this.error = 'Not authenticated';
      this.isLoading = false;
      return;
    }

    const claims = this.decodeTokenPayload(token);
    if (!claims || !claims.academyId) {
      this.error = 'No academy association found';
      this.isLoading = false;
      return;
    }

    this.profileService.getPlayerVsAcademyAverage().subscribe({
      next: (res) => {
        this.data = res;
        this.isLoading = false;
        this.cdr.detectChanges();
        if (res.categories.length > 0) {
          this.initRadarChart();
        }
      },
      error: (err) => {
        this.error = err?.error?.message || 'Failed to load comparison data';
        this.isLoading = false;
      }
    });
  }

  ngAfterViewInit(): void {
    if (this.data?.categories.length) {
      this.initRadarChart();
    }
  }

  private initRadarChart(): void {
    if (!this.data || !this.radarCanvas) return;
    if (this.data.categories.length <= 1) return;

    if (this.radarChart) {
      this.radarChart.destroy();
    }

    const labels = this.data.categories.map(c => c.categoryName);
    const playerData = this.data.categories.map(c => c.playerAverage);
    const academyData = this.data.categories.map(c => c.academyAverage);

    this.radarChart = new Chart(this.radarCanvas.nativeElement, {
      type: 'radar',
      data: {
        labels,
        datasets: [
          {
            label: this.data.playerName,
            data: playerData,
            backgroundColor: 'rgba(255, 215, 0, 0.15)',
            borderColor: '#ffd700',
            borderWidth: 2.5,
            pointBackgroundColor: '#ffd700',
            pointRadius: 4
          },
          {
            label: 'Academy Avg',
            data: academyData,
            backgroundColor: 'rgba(0, 229, 255, 0.1)',
            borderColor: '#00e5ff',
            borderWidth: 2,
            borderDash: [4, 4],
            pointBackgroundColor: '#00e5ff',
            pointRadius: 3
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
              font: { size: 10, weight: 'bold', family: 'Inter' }
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
