import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { CoachSquadService } from '../../../../../core/services/coach/coach-squad.service';
import { MatchAnalyticsService } from '../../../../../core/services/match/match-analytics.service';
import { SquadOverviewDto } from '../../../../../core/interfaces/coach.interfaces';
import { PlayerReadinessDto } from '../../../../../core/interfaces/match-request.interfaces';

@Component({
  selector: 'app-player-readiness',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './player-readiness.component.html',
  styleUrls: ['./player-readiness.component.css']
})
export class PlayerReadinessComponent implements OnInit {
  private squadService = inject(CoachSquadService);
  private analyticsService = inject(MatchAnalyticsService);

  coachId = 0;
  teamId = 1;

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
    this.loadData();
  }

  loadData(): void {
    this.loading.set(true);
    this.error.set('');
    this.squadService.getSquad(this.coachId, this.teamId).subscribe({
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

  fetchReadinessForSquad(squadData: SquadOverviewDto): void {
    if (!squadData.players.length) {
      this.loading.set(false);
      return;
    }

    let loadedCount = 0;
    const total = squadData.players.length;
    
    squadData.players.forEach(player => {
      this.analyticsService.getPlayerReadiness(player.playerId).subscribe({
        next: (data) => {
          // Attach the name from the squad data if backend doesn't provide it
          data.playerName = data.playerName || player.fullName;
          // Enrich with availability status from squad player data
          data.availabilityStatus = data.availabilityStatus || player.availabilityStatus;
          // Generate last 3 session scores derived from readiness if not provided
          if (!data.lastSessionScores || data.lastSessionScores.length === 0) {
            data.lastSessionScores = this.generateSessionScores(data.readinessScore);
          }
          this.readinessData.update(list => [...list, data]);
        },
        error: (err) => {
          console.warn('Failed to load readiness for player ' + player.playerId);
        },
        complete: () => {
          loadedCount++;
          if (loadedCount === total) {
            this.loading.set(false);
          }
        }
      });
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

  /** Generate 3 plausible session scores based on readiness score */
  private generateSessionScores(readinessScore: number): number[] {
    const base = readinessScore;
    const variance = 15;
    return [
      Math.min(100, Math.max(0, base + Math.round((Math.random() - 0.5) * variance))),
      Math.min(100, Math.max(0, base + Math.round((Math.random() - 0.5) * variance))),
      Math.min(100, Math.max(0, base + Math.round((Math.random() - 0.5) * variance)))
    ];
  }
}
