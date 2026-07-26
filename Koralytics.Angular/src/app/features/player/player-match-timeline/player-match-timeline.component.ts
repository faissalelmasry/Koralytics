import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { NavbarComponent } from '../../../../shared/components/navbar/navbar';
import { Footer } from '../../../../shared/components/footer/footer';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner';
import { EmptyStateComponent } from '../../../../shared/components/empty-state/empty-state';
import { Pagination } from '../../../../shared/components/pagination/pagination';
import { CustomSelect } from '../../../../shared/components/custom-select/custom-select';
import { CustomDatePicker } from '../../../../shared/components/custom-date-picker/custom-date-picker';
import { CustomButtonComponent } from '../../../../shared/components/custom-button/custom-button';
import { ScrollRevealDirective } from '../../../../shared/directives/scroll-reveal.directive';
import { PlayerProfileService } from '../../../../core/services/player/player-profile.service';
import { TokenStorageService } from '../../../../core/services/auth/token-storage.service';
import { MatchTimelineEventModel } from '../../../../core/models/Player/match-timeline-model';

@Component({
  selector: 'app-player-match-timeline',
  standalone: true,
  imports: [
    CommonModule,
    NavbarComponent,
    Footer,
    LoadingSpinnerComponent,
    EmptyStateComponent,
    Pagination,
    CustomSelect,
    CustomDatePicker,
    CustomButtonComponent,
    ScrollRevealDirective
  ],
  templateUrl: './player-match-timeline.component.html',
  styleUrls: ['./player-match-timeline.component.css']
})
export class PlayerMatchTimelineComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private profileService = inject(PlayerProfileService);
  private tokenStorage = inject(TokenStorageService);
  private cdr = inject(ChangeDetectorRef);

  playerId: number | null = null;
  playerName = '';
  isLoading = false;
  error = '';
  filterError = '';

  currentPage = 1;
  pageSize = 10;
  totalItems = 0;

  events: MatchTimelineEventModel[] = [];

  selectedMatchType = '';
  selectedDateFrom = '';
  selectedDateTo = '';

  matchTypeOptions = [
    { value: 'Session', label: 'Session' },
    { value: 'Friendly', label: 'Friendly' },
    { value: 'Tournament', label: 'Tournament' }
  ];

  ngOnInit() {
    const paramId = this.route.snapshot.paramMap.get('playerId');

    if (paramId) {
      this.playerId = Number(paramId);
      this.fetchPlayerDetailsAndTimeline();
    } else {
      const user = this.tokenStorage.getUser();
      if (!user?.userId) {
        this.error = 'Invalid session';
        return;
      }
      this.playerId = user.userId;
      this.fetchPlayerDetailsAndTimeline();
    }
  }

  fetchPlayerDetailsAndTimeline() {
    if (!this.playerId) return;

    this.profileService.getPlayerProfile(this.playerId).subscribe({
      next: (profile) => {
        this.playerName = `${profile.firstName} ${profile.lastName}`;
        this.cdr.detectChanges();
      },
      error: () => {
        this.playerName = 'Player';
      }
    });

    this.loadTimeline();
  }

  loadTimeline() {
    if (!this.playerId) return;

    this.isLoading = true;
    this.error = '';

    this.profileService.getMatchTimeline(
      this.playerId,
      this.currentPage,
      this.pageSize,
      this.selectedMatchType || undefined,
      this.selectedDateFrom || undefined,
      this.selectedDateTo || undefined
    ).subscribe({
      next: (res: any) => {
        this.events = this.mapEvents(res.events ?? res.Events ?? []);
        this.totalItems = res.totalCount ?? res.TotalCount ?? 0;
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.isLoading = false;
        this.error = 'Failed to load match timeline events.';
        this.cdr.detectChanges();
      }
    });
  }

  applyFilters() {
    this.filterError = '';

    if (this.selectedDateFrom && this.selectedDateTo && this.selectedDateFrom > this.selectedDateTo) {
      this.filterError = '"From" date must be earlier than "To" date.';
      return;
    }

    this.currentPage = 1;
    this.loadTimeline();
  }

  clearFilters() {
    this.selectedMatchType = '';
    this.selectedDateFrom = '';
    this.selectedDateTo = '';
    this.filterError = '';
    this.currentPage = 1;
    this.loadTimeline();
  }

  onDateFromChange() {
    if (this.selectedDateFrom && this.selectedDateTo && this.selectedDateFrom > this.selectedDateTo) {
      this.selectedDateTo = this.selectedDateFrom;
    }
  }

  onDateToChange() {
    if (this.selectedDateFrom && this.selectedDateTo && this.selectedDateFrom > this.selectedDateTo) {
      this.selectedDateFrom = this.selectedDateTo;
    }
  }

  onPageChange(page: number) {
    this.currentPage = page;
    this.loadTimeline();
  }

  getOutcomeClass(event: MatchTimelineEventModel): 'win' | 'draw' | 'loss' {
    if (event.homeScore === event.awayScore) return 'draw';

    if (event.homeScore > event.awayScore) return 'win';

    return 'loss';
  }

  getRatingColorClass(event: MatchTimelineEventModel): 'score-green' | 'score-yellow' | 'score-red' {
    if (!event.rating) return 'score-yellow';
    if (event.rating >= 8.0) return 'score-green';
    if (event.rating >= 6.5) return 'score-yellow';
    return 'score-red';
  }

  formatDate(dateStr: string): string {
    if (!dateStr) return '';
    const date = new Date(dateStr);
    return date.toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric'
    });
  }

  private mapEvents(raw: any[]): MatchTimelineEventModel[] {
    return raw.map((e: any) => ({
      date: e.date ?? e.Date ?? '',
      title: e.title ?? e.Title ?? '',
      matchId: e.matchId ?? e.MatchId ?? 0,
      matchType: e.matchType ?? e.MatchType ?? '',
      homeTeamName: e.homeTeamName ?? e.HomeTeamName ?? null,
      awayTeamName: e.awayTeamName ?? e.AwayTeamName ?? null,
      homeScore: e.homeScore ?? e.HomeScore ?? 0,
      awayScore: e.awayScore ?? e.AwayScore ?? 0,
      homePenaltyScore: e.homePenaltyScore ?? e.HomePenaltyScore ?? null,
      awayPenaltyScore: e.awayPenaltyScore ?? e.AwayPenaltyScore ?? null,
      goals: e.goals ?? e.Goals ?? 0,
      assists: e.assists ?? e.Assists ?? 0,
      minutesPlayed: e.minutesPlayed ?? e.MinutesPlayed ?? 0,
      isMOTM: e.isMOTM ?? e.IsMOTM ?? false,
      rating: e.rating ?? e.Rating ?? null,
      coachNote: e.coachNote ?? e.CoachNote ?? null,
      description: e.description ?? e.Description ?? null
    }));
  }
}
