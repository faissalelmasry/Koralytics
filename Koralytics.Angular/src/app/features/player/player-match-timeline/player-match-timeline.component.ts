import { Component, OnInit, inject, ChangeDetectorRef, ChangeDetectionStrategy, DestroyRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
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
import { TranslateService } from '@ngx-translate/core';
import { TokenStorageService } from '../../../../core/services/auth/token-storage.service';
import { MatchTimelineEventModel } from '../../../../core/models/Player/match-timeline-model';
import { TranslatePipe } from '@ngx-translate/core';
import { LocalizedDatePipe } from '../../../../shared/pipes/localized-date.pipe';

export interface DisplayMatchTimelineEvent extends MatchTimelineEventModel {
  outcomeClass: 'win' | 'draw' | 'loss';
  outcomeTranslationKey: string;
  ratingColorClass: 'score-green' | 'score-yellow' | 'score-red';
  formattedDate: string;
}

@Component({
  selector: 'app-player-match-timeline',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    NavbarComponent,
    Footer,
    LoadingSpinnerComponent,
    EmptyStateComponent,
    Pagination,
    CustomSelect,
    CustomDatePicker,
    CustomButtonComponent,
    ScrollRevealDirective,
    TranslatePipe,
    LocalizedDatePipe
  ],
  templateUrl: './player-match-timeline.component.html',
  styleUrls: ['./player-match-timeline.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class PlayerMatchTimelineComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private translate = inject(TranslateService);
  private profileService = inject(PlayerProfileService);
  private tokenStorage = inject(TokenStorageService);
  private cdr = inject(ChangeDetectorRef);
  private destroyRef = inject(DestroyRef);

  playerId: number | null = null;
  playerName = '';
  isLoading = false;
  error = '';
  filterError = '';

  currentPage = 1;
  pageSize = 10;
  totalItems = 0;

  events: DisplayMatchTimelineEvent[] = [];
  private rawEvents: any[] = [];

  selectedMatchType = '';
  selectedDateFrom = '';
  selectedDateTo = '';

  matchTypeOptions: { value: string; label: string }[] = [];

  ngOnInit() {
    this.updateMatchTypeOptions();

    const paramId = this.route.snapshot.paramMap.get('playerId');

    if (paramId) {
      this.playerId = Number(paramId);
      this.fetchPlayerDetailsAndTimeline();
    } else {
      const user = this.tokenStorage.getUser();
      if (!user?.userId) {
        this.error = 'Invalid session';
        this.cdr.markForCheck();
        return;
      }
      this.playerId = user.userId;
      this.fetchPlayerDetailsAndTimeline();
    }

    this.translate.onLangChange
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        this.updateMatchTypeOptions();
        if (this.rawEvents.length > 0) {
          this.events = this.mapEvents(this.rawEvents);
        }
        this.cdr.markForCheck();
      });

    this.translate.onTranslationChange
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        this.updateMatchTypeOptions();
        if (this.rawEvents.length > 0) {
          this.events = this.mapEvents(this.rawEvents);
        }
        this.cdr.markForCheck();
      });
  }

  private updateMatchTypeOptions() {
    this.matchTypeOptions = [
      { value: 'Session', label: 'PLAYER.MATCH_SESSION' },
      { value: 'Friendly', label: 'PLAYER.MATCH_FRIENDLY' },
      { value: 'Tournament', label: 'PLAYER.MATCH_TOURNAMENT' }
    ];
  }

  fetchPlayerDetailsAndTimeline() {
    if (!this.playerId) return;

    this.profileService.getPlayerProfile(this.playerId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (profile) => {
          this.playerName = `${profile.firstName} ${profile.lastName}`;
          this.cdr.markForCheck();
        },
        error: () => {
          this.playerName = 'Player';
          this.cdr.markForCheck();
        }
      });

    this.loadTimeline();
  }

  loadTimeline() {
    if (!this.playerId) return;

    this.isLoading = true;
    this.error = '';
    this.cdr.markForCheck();

    this.profileService.getMatchTimeline(
      this.playerId,
      this.currentPage,
      this.pageSize,
      this.selectedMatchType || undefined,
      this.selectedDateFrom || undefined,
      this.selectedDateTo || undefined
    )
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (res: any) => {
          this.rawEvents = res.events ?? res.Events ?? [];
          this.events = this.mapEvents(this.rawEvents);
          this.totalItems = res.totalCount ?? res.TotalCount ?? 0;
          this.isLoading = false;
          this.cdr.markForCheck();
        },
        error: () => {
          this.isLoading = false;
          this.error = 'Failed to load match timeline events.';
          this.cdr.markForCheck();
        }
      });
  }

  applyFilters() {
    this.filterError = '';

    if (this.selectedDateFrom && this.selectedDateTo && this.selectedDateFrom > this.selectedDateTo) {
      this.filterError = '"From" date must be earlier than "To" date.';
      this.cdr.markForCheck();
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
      this.cdr.markForCheck();
    }
  }

  onDateToChange() {
    if (this.selectedDateFrom && this.selectedDateTo && this.selectedDateFrom > this.selectedDateTo) {
      this.selectedDateFrom = this.selectedDateTo;
      this.cdr.markForCheck();
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

  private mapEvents(raw: any[]): DisplayMatchTimelineEvent[] {
    return raw.map((e: any) => {
      const date = e.date ?? e.Date ?? '';
      const homeScore = e.homeScore ?? e.HomeScore ?? 0;
      const awayScore = e.awayScore ?? e.AwayScore ?? 0;
      const rating = e.rating ?? e.Rating ?? null;

      const baseEvent: MatchTimelineEventModel = {
        date,
        title: e.title ?? e.Title ?? '',
        matchId: e.matchId ?? e.MatchId ?? 0,
        matchType: e.matchType ?? e.MatchType ?? '',
        homeTeamName: e.homeTeamName ?? e.HomeTeamName ?? null,
        awayTeamName: e.awayTeamName ?? e.AwayTeamName ?? null,
        homeScore,
        awayScore,
        homePenaltyScore: e.homePenaltyScore ?? e.HomePenaltyScore ?? null,
        awayPenaltyScore: e.awayPenaltyScore ?? e.AwayPenaltyScore ?? null,
        goals: e.goals ?? e.Goals ?? 0,
        assists: e.assists ?? e.Assists ?? 0,
        minutesPlayed: e.minutesPlayed ?? e.MinutesPlayed ?? 0,
        isMOTM: e.isMOTM ?? e.IsMOTM ?? false,
        rating,
        coachNote: e.coachNote ?? e.CoachNote ?? null,
        description: e.description ?? e.Description ?? null
      };

      const outcomeClass = this.getOutcomeClass(baseEvent);

      return {
        ...baseEvent,
        outcomeClass,
        outcomeTranslationKey: 'PLAYER.OUTCOME_' + outcomeClass.toUpperCase(),
        ratingColorClass: this.getRatingColorClass(baseEvent),
        formattedDate: this.formatDate(date),
      };
    });
  }
}
