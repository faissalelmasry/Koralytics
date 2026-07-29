import { Component, Input, OnInit, OnChanges, SimpleChanges, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { MatchService, PostMatchAnalysisResponseDto } from '../../../../core/services/match/match.service';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner';
import { EmptyStateComponent } from '../../../../shared/components/empty-state/empty-state';

@Component({
  selector: 'app-match-analysis',
  standalone: true,
  imports: [
    CommonModule,
    LoadingSpinnerComponent,
    EmptyStateComponent
  ],
  templateUrl: './match-analysis.component.html',
  styleUrls: ['./match-analysis.component.css']
})
export class MatchAnalysisComponent implements OnInit, OnChanges {
  private router = inject(Router);
  private matchService = inject(MatchService);
  private cdr = inject(ChangeDetectorRef);

  @Input() homeTeamId?: number;
  @Input() homeTeamName?: string;
  @Input() awayTeamId?: number;
  @Input() awayTeamName?: string;
  @Input() teamId?: number;

  selectedSide: 'home' | 'away' = 'home';
  isLoading = false;
  error = '';
  analysisData: PostMatchAnalysisResponseDto | null = null;

  onMatchClick(matchId: number): void {
    if (matchId) {
      this.router.navigate(['/match', matchId]);
    }
  }

  private analysisCache = new Map<number, PostMatchAnalysisResponseDto>();

  ngOnInit(): void {
    this.loadAnalysis();
  }

  ngOnChanges(changes: SimpleChanges): void {
    const homeChanged = changes['homeTeamId'] && changes['homeTeamId'].currentValue !== changes['homeTeamId'].previousValue;
    const awayChanged = changes['awayTeamId'] && changes['awayTeamId'].currentValue !== changes['awayTeamId'].previousValue;
    const teamChanged = changes['teamId'] && changes['teamId'].currentValue !== changes['teamId'].previousValue;

    if (homeChanged || awayChanged || teamChanged) {
      this.loadAnalysis();
    }
  }

  get activeTeamId(): number | undefined {
    if (this.teamId) return this.teamId;
    return this.selectedSide === 'home' ? this.homeTeamId : this.awayTeamId;
  }

  get activeTeamName(): string {
    if (this.teamId) return this.analysisData?.teamName || 'Team';
    return this.selectedSide === 'home' 
      ? (this.homeTeamName || 'Home Team') 
      : (this.awayTeamName || 'Away Team');
  }

  get hasTeamToggle(): boolean {
    return !!(this.homeTeamId && this.awayTeamId && !this.teamId);
  }

  selectSide(side: 'home' | 'away'): void {
    if (this.selectedSide === side) return;
    this.selectedSide = side;
    this.loadAnalysis();
  }

  loadAnalysis(force: boolean = false): void {
    const targetId = this.activeTeamId;
    if (!targetId) return;

    if (!force && this.analysisCache.has(targetId)) {
      this.analysisData = this.analysisCache.get(targetId)!;
      this.isLoading = false;
      this.error = '';
      this.cdr.detectChanges();
      return;
    }

    this.isLoading = true;
    this.error = '';

    this.matchService.getPostMatchAnalysis(targetId).subscribe({
      next: (res: any) => {
        const data = res?.data ?? res;
        this.analysisData = data as PostMatchAnalysisResponseDto;
        if (this.analysisData) {
          this.analysisCache.set(targetId, this.analysisData);
        }
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.isLoading = false;
        this.error = 'Failed to load team post-match analysis.';
        this.cdr.detectChanges();
      }
    });
  }

  get totalMatches(): number {
    if (!this.analysisData) return 0;
    return this.analysisData.wins + this.analysisData.draws + this.analysisData.losses;
  }

  get winRate(): number {
    if (this.totalMatches === 0) return 0;
    return (this.analysisData!.wins / this.totalMatches) * 100;
  }

  get drawRate(): number {
    if (this.totalMatches === 0) return 0;
    return (this.analysisData!.draws / this.totalMatches) * 100;
  }

  get lossRate(): number {
    if (this.totalMatches === 0) return 0;
    return (this.analysisData!.losses / this.totalMatches) * 100;
  }

  get goalDifference(): number {
    if (!this.analysisData) return 0;
    return this.analysisData.goalsFor - this.analysisData.goalsAgainst;
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

  getTeamInitials(name?: string): string {
    if (!name) return '?';
    const parts = name.trim().split(' ');
    if (parts.length >= 2) {
      return (parts[0][0] + parts[1][0]).toUpperCase();
    }
    return name.slice(0, 2).toUpperCase();
  }
}
