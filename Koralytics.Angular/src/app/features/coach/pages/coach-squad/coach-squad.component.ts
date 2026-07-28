import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { CoachSquadService } from '../../../../../core/services/coach/coach-squad.service';
import { MatchAnalyticsService } from '../../../../../core/services/match/match-analytics.service';
import { AuthService } from '../../../../../core/services/auth/auth.service';
import {
  SquadOverviewDto,
  SquadPlayerDto,
  SquadComparisonDto,
} from '../../../../../core/interfaces/coach.interfaces';
import { PlayerReadinessDto } from '../../../../../core/interfaces/match-request.interfaces';

@Component({
  selector: 'app-coach-squad',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './coach-squad.component.html',
  styleUrls: ['./coach-squad.component.css'],
})
export class CoachSquadComponent implements OnInit {
  private squadService = inject(CoachSquadService);
  private analyticsService = inject(MatchAnalyticsService);
  private authService = inject(AuthService);
  private destroyRef = inject(DestroyRef);

  squad = signal<SquadOverviewDto | null>(null);
  readinessMap = signal<Record<number, PlayerReadinessDto>>({});
  comparison = signal<SquadComparisonDto | null>(null);
  loading = signal(false);
  error = signal('');

  // Resolved from auth context
  coachId = 0;
  teamId = 0; // TODO: resolve from coach profile API or route params

  // Comparison selection
  selectedPlayerA: number | null = null;
  selectedPlayerB: number | null = null;
  showCompareModal = false;

  ngOnInit(): void {
    const user = this.authService.getCurrentUserSync();
    if (user) {
      this.coachId = user.userId;
    }
    this.loadSquad();
  }

  loadSquad(): void {
    this.loading.set(true);
    this.error.set('');
    this.squadService.getSquad(this.teamId, this.coachId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (data) => {
          this.squad.set(data);
          this.loading.set(false);
          this.loadReadinessForSquad(data.players);
        },
        error: (err) => {
          this.error.set(err?.error?.message || 'Failed to load squad');
          this.loading.set(false);
        },
      });
  }

  /** Batch-load readiness for all players using forkJoin instead of N+1 calls */
  private loadReadinessForSquad(players: SquadPlayerDto[]): void {
    if (!players.length) return;

    const requests = players.map(p =>
      this.analyticsService.getPlayerReadiness(p.playerId).pipe(
        catchError(() => of(null))
      )
    );

    forkJoin(requests)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(results => {
        const map: Record<number, PlayerReadinessDto> = {};
        results.forEach((data, i) => {
          if (data) {
            map[players[i].playerId] = data;
          }
        });
        this.readinessMap.set(map);
      });
  }

  getAvailabilityClass(status: string): string {
    switch (status?.toLowerCase()) {
      case 'available':
        return 'status-available';
      case 'injured':
        return 'status-injured';
      case 'loaned':
        return 'status-loaned';
      case 'suspended':
        return 'status-suspended';
      default:
        return 'status-default';
    }
  }

  getReadinessColor(score: number): string {
    if (score >= 80) return 'var(--accent-lime, #c8ff4d)';
    if (score >= 50) return '#ffa726';
    return '#ef5350';
  }

  togglePlayerSelection(playerId: number): void {
    if (this.selectedPlayerA === playerId) {
      this.selectedPlayerA = null;
    } else if (this.selectedPlayerB === playerId) {
      this.selectedPlayerB = null;
    } else if (!this.selectedPlayerA) {
      this.selectedPlayerA = playerId;
    } else if (!this.selectedPlayerB) {
      this.selectedPlayerB = playerId;
    }
  }

  isSelected(playerId: number): boolean {
    return this.selectedPlayerA === playerId || this.selectedPlayerB === playerId;
  }

  canCompare(): boolean {
    return this.selectedPlayerA !== null && this.selectedPlayerB !== null;
  }

  openComparison(): void {
    if (!this.canCompare()) return;
    this.squadService
      .compareSquadPlayers(this.selectedPlayerA!, this.selectedPlayerB!)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (data) => {
          this.comparison.set(data);
          this.showCompareModal = true;
        },
        error: (err) => {
          this.error.set(err?.error?.message || 'Failed to compare players');
        },
      });
  }

  closeComparison(): void {
    this.showCompareModal = false;
    this.comparison.set(null);
    this.selectedPlayerA = null;
    this.selectedPlayerB = null;
  }

  /** Safely get a numeric rating from a SquadPlayerDto by property name */
  getRating(player: SquadPlayerDto, key: string): number {
    return (player as Record<string, any>)[key] ?? 0;
  }

  /** Extract the label from a rating key, e.g. 'paceRating' → 'PACE' */
  getCategoryLabel(key: string): string {
    return key.replace('Rating', '').toUpperCase();
  }
}
