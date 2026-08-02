import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CoachSquadService } from '../../../../../core/services/coach/coach-squad.service';
import { DrillSessionService } from '../../../../../core/services/drill/drill-session.service';
import { TrainingTeamSplitDto, SquadPlayerDto } from '../../../../../core/interfaces/coach.interfaces';
import { DrillSessionDto, SessionFilterDto } from '../../../../../core/interfaces/drill-session.model';

@Component({
  selector: 'app-training-split',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './training-split.component.html',
  styleUrls: ['./training-split.component.css']
})
export class TrainingSplitComponent implements OnInit {
  private squadService = inject(CoachSquadService);
  private sessionService = inject(DrillSessionService);
  private destroyRef = inject(DestroyRef);

  // Sessions loaded from API
  availableSessions = signal<DrillSessionDto[]>([]);
  selectedSessionId = 0;
  loadingSessions = signal(false);

  splitResult = signal<TrainingTeamSplitDto | null>(null);
  loading = signal(false);
  error = signal('');

  // Computed average ratings
  teamAAverage = computed(() => this.calculateAverage(this.splitResult()?.teamA));
  teamBAverage = computed(() => this.calculateAverage(this.splitResult()?.teamB));

  // Balance Indicator
  ratingDifference = computed(() => Math.abs(this.teamAAverage() - this.teamBAverage()));
  
  balanceStatus = computed(() => {
    const diff = this.ratingDifference();
    if (diff <= 3.0) return 'Balanced';
    if (diff <= 6.0) return 'Warning';
    return 'Unbalanced';
  });

  balancePercentage = computed(() => {
    const avgA = this.teamAAverage();
    const avgB = this.teamBAverage();
    if (avgA === 0 && avgB === 0) return 50;
    const total = avgA + avgB;
    return (avgA / total) * 100;
  });

  ngOnInit(): void {
    this.loadSessions();
  }

  loadSessions(): void {
    this.loadingSessions.set(true);
    const filter: SessionFilterDto = { pageNumber: 1, pageSize: 50 };
    this.sessionService.getCoachSessions(filter)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (sessions) => {
          this.availableSessions.set(sessions);
          if (sessions.length > 0) {
            this.selectedSessionId = sessions[0].id;
          }
          this.loadingSessions.set(false);
        },
        error: () => {
          this.error.set('Failed to load training sessions.');
          this.loadingSessions.set(false);
        }
      });
  }

  generateSplit(): void {
    if (!this.selectedSessionId) return;

    this.loading.set(true);
    this.error.set('');
    this.splitResult.set(null);

    this.squadService.splitTrainingTeams(this.selectedSessionId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (data) => {
          this.splitResult.set(data);
          this.loading.set(false);
        },
        error: (err) => {
          this.error.set(err?.error?.message || 'Failed to split teams.');
          this.loading.set(false);
        }
      });
  }

  private calculateAverage(players?: SquadPlayerDto[]): number {
    if (!players || players.length === 0) return 0;
    const total = players.reduce((sum, p) => sum + p.overallRating, 0);
    return total / players.length;
  }
}
