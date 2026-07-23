import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { CoachSquadService } from '../../../../../core/services/coach/coach-squad.service';
import { TrainingTeamSplitDto, SquadPlayerDto } from '../../../../../core/interfaces/coach.interfaces';

@Component({
  selector: 'app-training-split',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './training-split.component.html',
  styleUrls: ['./training-split.component.css']
})
export class TrainingSplitComponent implements OnInit {
  private squadService = inject(CoachSquadService);

  // In a real app, we'd load active sessions from a session service. 
  // For now, we mock some sessions to select from.
  availableSessions = [
    { id: 101, title: 'Morning Tactics & Passing', date: new Date().toISOString() },
    { id: 102, title: 'Afternoon Match Practice', date: new Date().toISOString() }
  ];
  selectedSessionId: number = this.availableSessions[0].id;

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
    // We don't load initially, we wait for the user to trigger the split
  }

  generateSplit(): void {
    if (!this.selectedSessionId) return;

    this.loading.set(true);
    this.error.set('');
    this.splitResult.set(null);

    this.squadService.splitTrainingTeams(this.selectedSessionId).subscribe({
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
