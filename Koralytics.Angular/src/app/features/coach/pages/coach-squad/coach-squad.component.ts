import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { CoachSquadService } from '../../../../../core/services/coach/coach-squad.service';
import { MatchAnalyticsService } from '../../../../../core/services/match/match-analytics.service';
import {
  SquadOverviewDto,
  SquadPlayerDto,
  SquadComparisonDto,
} from '../../../../../core/interfaces/coach.interfaces';
import { PlayerReadinessDto } from '../../../../../core/interfaces/match-request.interfaces';

@Component({
  selector: 'app-coach-squad',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './coach-squad.component.html',
  styleUrls: ['./coach-squad.component.css'],
})
export class CoachSquadComponent implements OnInit {
  private squadService = inject(CoachSquadService);
  private analyticsService = inject(MatchAnalyticsService);

  squad = signal<SquadOverviewDto | null>(null);
  readinessMap = signal<Record<number, PlayerReadinessDto>>({});
  comparison = signal<SquadComparisonDto | null>(null);
  loading = signal(false);
  error = signal('');

  // Inputs
  coachId = 0;
  teamId = 1;

  // Comparison selection
  selectedPlayerA: number | null = null;
  selectedPlayerB: number | null = null;
  showCompareModal = false;

  ngOnInit(): void {
    // coachId would typically come from auth claims
    this.loadSquad();
  }

  loadSquad(): void {
    this.loading.set(true);
    this.error.set('');
    this.squadService.getSquad(this.coachId, this.teamId).subscribe({
      next: (data) => {
        this.squad.set(data);
        this.loading.set(false);
        // Load readiness for each player
        data.players.forEach((p) => this.loadReadiness(p.playerId));
      },
      error: (err) => {
        this.error.set(err?.error?.message || 'Failed to load squad');
        this.loading.set(false);
      },
    });
  }

  loadReadiness(playerId: number): void {
    this.analyticsService.getPlayerReadiness(playerId).subscribe({
      next: (data) => {
        this.readinessMap.update((map) => ({ ...map, [playerId]: data }));
      },
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
}
