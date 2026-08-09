import { Component, OnInit, OnDestroy, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { MatchService } from '../../../../../core/services/match/match.service';
import { MatchCardModel } from '../../../../../core/models/Match/match-card.model';
import { MatchCardComponent } from '../../match-card/match-card.component';
import { Pagination } from '../../../../../shared/components/pagination/pagination';
import { LoadingSpinnerComponent } from '../../../../../shared/components/loading-spinner/loading-spinner';
import { EmptyStateComponent } from '../../../../../shared/components/empty-state/empty-state';
import { NavbarComponent } from '../../../../../shared/components/navbar/navbar';
import { Footer } from '../../../../../shared/components/footer/footer';
import { CustomSelect } from '../../../../../shared/components/custom-select/custom-select';
import { CustomDatePicker } from '../../../../../shared/components/custom-date-picker/custom-date-picker';
import { CustomButtonComponent } from '../../../../../shared/components/custom-button/custom-button';
import { ScrollRevealDirective } from '../../../../../shared/directives/scroll-reveal.directive';
import { MatchSignalrService } from '../../../../../core/services/match-signalr.service';
import { TranslatePipe } from '@ngx-translate/core';
import { Subscription } from 'rxjs';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';

@Component({
  selector: 'app-coach-match-list',
  standalone: true,
  imports: [
    CommonModule, MatchCardComponent, Pagination, LoadingSpinnerComponent,
    EmptyStateComponent, NavbarComponent, Footer, CustomSelect,
    CustomDatePicker, CustomButtonComponent, ScrollRevealDirective, TranslatePipe
  ],
  templateUrl: './coach-match-list.component.html',
  styleUrls: ['./coach-match-list.component.css']
})
export class CoachMatchListComponent implements OnInit, OnDestroy {
  private router = inject(Router);
  private matchService = inject(MatchService);
  private cdr = inject(ChangeDetectorRef);
  private signalrService = inject(MatchSignalrService);
  private translate = inject(TranslateService);

  private signalrSub?: Subscription;

  matches: MatchCardModel[] = [];
  isLoading = false;
  error = '';
  filterError = '';

  currentPage = 1;
  pageSize = 20;
  totalItems = 0;

  selectedStatus: string | null = null;
  selectedMatchType: string | null = null;
  selectedDateFrom = '';
  selectedDateTo = '';

  get isDateLocked(): boolean {
    return this.selectedStatus === 'Live';
  }

  get statusOptions() {
    return [
      { value: '', label: this.translate.instant('MATCH.REQUEST_STATUS.ALL') },
      { value: 'Scheduled', label: this.translate.instant('MATCH.STATUS.SCHEDULED') },
      { value: 'Live', label: this.translate.instant('MATCH.STATUS.LIVE') },
      { value: 'Completed', label: this.translate.instant('MATCH.STATUS.COMPLETED') }
    ];
  }

  get matchTypeOptions() {
    return [
      { value: '', label: this.translate.instant('MATCH.REQUEST_STATUS.ALL') },
      { value: 'Friendly', label: this.translate.instant('MATCH.TYPE.FRIENDLY') },
      { value: 'Tournament', label: this.translate.instant('MATCH.TYPE.TOURNAMENT') },
      { value: 'Session', label: this.translate.instant('MATCH.TYPE.SESSION') }
    ];
  }

  ngOnInit(): void {
    this.loadMatches();
    this.subscribeToLiveUpdates();
  }

  ngOnDestroy(): void {
    if (this.signalrSub) {
      this.signalrSub.unsubscribe();
    }
  }

  private subscribeToLiveUpdates(): void {
    this.signalrSub = this.signalrService.matchScoreUpdate$.subscribe(update => {
      const matchIndex = this.matches.findIndex(m => m.id === update.matchId);
      if (matchIndex !== -1) {
        this.matches[matchIndex].homeScore = update.homeScore;
        this.matches[matchIndex].awayScore = update.awayScore;
        this.matches[matchIndex].homePenaltyScore = update.homePenaltyScore ?? undefined;
        this.matches[matchIndex].awayPenaltyScore = update.awayPenaltyScore ?? undefined;
        this.matches[matchIndex].status = update.status;
        this.cdr.detectChanges();
      }
    });
  }

  loadMatches(): void {
    this.isLoading = true;
    this.error = '';

    this.matchService.getCoachMatches(
      this.selectedStatus ?? undefined,
      this.selectedMatchType ?? undefined,
      this.selectedDateFrom || undefined,
      this.selectedDateTo || undefined,
      this.currentPage,
      this.pageSize
    ).subscribe({
      next: (res) => {
        const coachTeamIds: number[] = res.data?.coachTeamIds ?? [];
        this.matches = (res.data?.matches ?? []).map(m => {
          if (m.status !== 'Completed') return m;
          if (m.homeTeamId === m.awayTeamId) return m;

          const isCoachHome = coachTeamIds.includes(m.homeTeamId);
          const isCoachAway = coachTeamIds.includes(m.awayTeamId);
          if (isCoachHome && isCoachAway) return m;

          if (isCoachHome) {
            m.coachSide = 'home';
            if (m.homeScore > m.awayScore) m.coachOutcome = 'win';
            else if (m.homeScore < m.awayScore) m.coachOutcome = 'loss';
            else {
              const hasPens = m.homePenaltyScore != null && m.awayPenaltyScore != null;
              if (hasPens && m.homePenaltyScore! > m.awayPenaltyScore!) m.coachOutcome = 'win';
              else if (hasPens && m.homePenaltyScore! < m.awayPenaltyScore!) m.coachOutcome = 'loss';
              else m.coachOutcome = 'draw';
            }
          } else if (isCoachAway) {
            m.coachSide = 'away';
            if (m.awayScore > m.homeScore) m.coachOutcome = 'win';
            else if (m.awayScore < m.homeScore) m.coachOutcome = 'loss';
            else {
              const hasPens = m.homePenaltyScore != null && m.awayPenaltyScore != null;
              if (hasPens && m.awayPenaltyScore! > m.homePenaltyScore!) m.coachOutcome = 'win';
              else if (hasPens && m.awayPenaltyScore! < m.homePenaltyScore!) m.coachOutcome = 'loss';
              else m.coachOutcome = 'draw';
            }
          }

          return m;
        });
        this.totalItems = res.data?.totalCount ?? 0;
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.isLoading = false;
        this.error = this.translate.instant('MATCH.ERRORS.GENERIC_LOAD');
        this.cdr.detectChanges();
      }
    });
  }

  applyFilters(): void {
    this.filterError = '';

    if (this.selectedDateFrom && this.selectedDateTo && this.selectedDateFrom > this.selectedDateTo) {
      this.filterError = this.translate.instant('MATCH.ERRORS.DATE_FROM_BEFORE_TO');
      return;
    }

    this.currentPage = 1;
    this.loadMatches();
  }

  clearFilters(): void {
    this.selectedStatus = null;
    this.selectedMatchType = null;
    this.selectedDateFrom = '';
    this.selectedDateTo = '';
    this.filterError = '';
    this.currentPage = 1;
    this.loadMatches();
  }

  onDateFromChange(): void {
    if (this.selectedDateFrom && this.selectedDateTo && this.selectedDateFrom > this.selectedDateTo) {
      this.selectedDateTo = this.selectedDateFrom;
    }
  }

  onDateToChange(): void {
    if (this.selectedDateFrom && this.selectedDateTo && this.selectedDateFrom > this.selectedDateTo) {
      this.selectedDateFrom = this.selectedDateTo;
    }
  }

  onStatusChange(): void {
    if (this.selectedStatus === 'Live') {
      this.selectedDateFrom = '';
      this.selectedDateTo = '';
    }
  }

  onPageChange(page: number): void {
    this.currentPage = page;
    this.loadMatches();
  }

  onMatchClick(matchId: number): void {
    if (matchId) {
      this.router.navigate(['/match', matchId]);
    }
  }
}
