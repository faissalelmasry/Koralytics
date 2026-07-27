import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { CoachSquadService } from '../../../../../core/services/coach/coach-squad.service';
import { MatchAnalyticsService } from '../../../../../core/services/match/match-analytics.service';
import { AuthService } from '../../../../../core/services/auth/auth.service';
import { SquadOverviewDto } from '../../../../../core/interfaces/coach.interfaces';
import { PlayerReadinessDto } from '../../../../../core/interfaces/match-request.interfaces';

@Component({
  selector: 'app-player-readiness',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './player-readiness.component.html',
  styleUrls: ['./player-readiness.component.css']
})
export class PlayerReadinessComponent implements OnInit {
  private squadService = inject(CoachSquadService);
  private analyticsService = inject(MatchAnalyticsService);
  private authService = inject(AuthService);
  private destroyRef = inject(DestroyRef);

  // Resolved from auth context
  coachId = 0;
  teamId = 0; // TODO: resolve from coach profile API or route params

  squad = signal<SquadOverviewDto | null>(null);
  readinessData = signal<PlayerReadinessDto[]>([]);
  loading = signal(false);
  error = signal('');
  
  // Filtering and Sorting
  sortKey = signal<'score' | 'name' | 'matches' | 'availability'>('score');
  sortDirection = signal<'asc' | 'desc'>('desc');

  sortedReadiness = computed(() => {
    let data = [...this.readinessData()];
    
    data.sort((a, b) => {
      let valA: any = a.readinessScore;
      let valB: any = b.readinessScore;
      
      if (this.sortKey() === 'name') {
        valA = a.playerName.toLowerCase();
        valB = b.playerName.toLowerCase();
      } else if (this.sortKey() === 'matches') {
        valA = a.matchesPlayedLast7Days;
        valB = b.matchesPlayedLast7Days;
      } else if (this.sortKey() === 'availability') {
        valA = a.availabilityStatus?.toLowerCase() || '';
        valB = b.availabilityStatus?.toLowerCase() || '';
      }

      if (valA < valB) return this.sortDirection() === 'asc' ? -1 : 1;
      if (valA > valB) return this.sortDirection() === 'asc' ? 1 : -1;
      return 0;
    });

    return data;
  });

  ngOnInit(): void {
    const user = this.authService.getCurrentUserSync();
    if (user) {
      this.coachId = user.userId;
    }
    this.loadData();
  }

  loadData(): void {
    this.loading.set(true);
    this.error.set('');
    this.squadService.getSquad(this.coachId, this.teamId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (squadData) => {
          this.squad.set(squadData);
          this.fetchReadinessForSquad(squadData);
        },
        error: (err) => {
          this.error.set(err?.error?.message || 'Failed to load squad.');
          this.loading.set(false);
        }
      });
  }

  /** Batch-load readiness for all players using forkJoin instead of N+1 calls */
  fetchReadinessForSquad(squadData: SquadOverviewDto): void {
    if (!squadData.players.length) {
      this.loading.set(false);
      return;
    }

    const requests = squadData.players.map(player =>
      this.analyticsService.getPlayerReadiness(player.playerId).pipe(
        catchError(() => of(null))
      )
    );

    forkJoin(requests)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(results => {
        const data: PlayerReadinessDto[] = [];
        results.forEach((result, i) => {
          if (result) {
            const player = squadData.players[i];
            // Attach the name from the squad data if backend doesn't provide it
            result.playerName = result.playerName || player.fullName;
            // Enrich with availability status from squad player data
            result.availabilityStatus = result.availabilityStatus || player.availabilityStatus;
            data.push(result);
          }
        });
        this.readinessData.set(data);
        this.loading.set(false);
      });
  }

  sortBy(key: 'score' | 'name' | 'matches' | 'availability'): void {
    if (this.sortKey() === key) {
      this.sortDirection.set(this.sortDirection() === 'asc' ? 'desc' : 'asc');
    } else {
      this.sortKey.set(key);
      this.sortDirection.set('desc');
    }
  }

  getScoreColor(score: number): string {
    if (score >= 80) return 'var(--accent-lime, #c8ff4d)'; // Green/Lime
    if (score >= 50) return '#ffa726'; // Orange
    return '#ef5350'; // Red
  }

  getAvailabilityClass(status: string): string {
    switch (status?.toLowerCase()) {
      case 'available': return 'avail-available';
      case 'injured': return 'avail-injured';
      case 'suspended': return 'avail-suspended';
      case 'resting': return 'avail-resting';
      case 'loaned': return 'avail-loaned';
      default: return 'avail-default';
    }
  }
}
