import { Component, Input, OnInit, OnChanges, SimpleChanges, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { MatchService, HeadToHeadResponseDto } from '../../../../core/services/match/match.service';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner';
import { EmptyStateComponent } from '../../../../shared/components/empty-state/empty-state';

@Component({
  selector: 'app-match-h2h',
  standalone: true,
  imports: [
    CommonModule,
    LoadingSpinnerComponent,
    EmptyStateComponent
  ],
  templateUrl: './match-h2h.component.html',
  styleUrls: ['./match-h2h.component.css']
})
export class MatchH2hComponent implements OnInit, OnChanges {
  private router = inject(Router);
  private matchService = inject(MatchService);
  private cdr = inject(ChangeDetectorRef);

  @Input() teamAId!: number;
  @Input() teamBId!: number;
  @Input() teamAName?: string;
  @Input() teamBName?: string;

  isLoading = false;
  error = '';
  h2hData: HeadToHeadResponseDto | null = null;

  onMatchClick(matchId: number): void {
    if (matchId) {
      this.router.navigate(['/match', matchId]);
    }
  }

  ngOnInit(): void {
    if (this.teamAId && this.teamBId) {
      this.loadHeadToHead();
    }
  }

  ngOnChanges(changes: SimpleChanges): void {
    if ((changes['teamAId'] || changes['teamBId']) && this.teamAId && this.teamBId) {
      this.loadHeadToHead();
    }
  }

  loadHeadToHead(): void {
    if (!this.teamAId || !this.teamBId) return;

    this.isLoading = true;
    this.error = '';

    this.matchService.getHeadToHead(this.teamAId, this.teamBId).subscribe({
      next: (res: any) => {
        const data = res?.data ?? res;
        this.h2hData = data as HeadToHeadResponseDto;
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.isLoading = false;
        this.error = 'Failed to load Head to Head data.';
        this.cdr.detectChanges();
      }
    });
  }

  getTeamInitials(name?: string): string {
    if (!name) return '?';
    const parts = name.trim().split(' ');
    if (parts.length >= 2) {
      return (parts[0][0] + parts[1][0]).toUpperCase();
    }
    return name.slice(0, 2).toUpperCase();
  }

  formatDate(dateStr?: string): string {
    if (!dateStr) return '';
    const date = new Date(dateStr);
    return date.toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric'
    });
  }

  isHomeWin(m: any): boolean {
    const homeScore = m.homeScore ?? m.HomeScore ?? 0;
    const awayScore = m.awayScore ?? m.AwayScore ?? 0;
    if (homeScore !== awayScore) {
      return homeScore > awayScore;
    }
    const homePen = m.homePenaltyScore ?? m.HomePenaltyScore;
    const awayPen = m.awayPenaltyScore ?? m.AwayPenaltyScore;
    if (homePen != null && awayPen != null) {
      return homePen > awayPen;
    }
    return false;
  }

  isAwayWin(m: any): boolean {
    const homeScore = m.homeScore ?? m.HomeScore ?? 0;
    const awayScore = m.awayScore ?? m.AwayScore ?? 0;
    if (homeScore !== awayScore) {
      return awayScore > homeScore;
    }
    const homePen = m.homePenaltyScore ?? m.HomePenaltyScore;
    const awayPen = m.awayPenaltyScore ?? m.AwayPenaltyScore;
    if (homePen != null && awayPen != null) {
      return awayPen > homePen;
    }
    return false;
  }

  hasPenalties(m: any): boolean {
    const homePen = m.homePenaltyScore ?? m.HomePenaltyScore;
    const awayPen = m.awayPenaltyScore ?? m.AwayPenaltyScore;
    return homePen != null && awayPen != null;
  }

  getPenaltyText(m: any): string {
    const homePen = m.homePenaltyScore ?? m.HomePenaltyScore;
    const awayPen = m.awayPenaltyScore ?? m.AwayPenaltyScore;
    return `${homePen} - ${awayPen} Pen`;
  }

  getTeamWinnerClass(m: any, isHome: boolean): string {
    const isWin = isHome ? this.isHomeWin(m) : this.isAwayWin(m);
    if (!isWin) return '';

    const teamId = isHome ? (m.homeTeamId ?? m.HomeTeamId) : (m.awayTeamId ?? m.AwayTeamId);
    const targetTeamAId = this.h2hData?.teamAId || this.teamAId;
    const targetTeamBId = this.h2hData?.teamBId || this.teamBId;

    if (teamId && targetTeamAId && teamId === targetTeamAId) {
      return 'team-a-winner';
    }
    if (teamId && targetTeamBId && teamId === targetTeamBId) {
      return 'team-b-winner';
    }

    return isHome ? 'team-a-winner' : 'team-b-winner';
  }
}
