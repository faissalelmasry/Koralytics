import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatchService } from '../../../../../core/services/match/match.service';
import { AcademyService } from '../../../../../core/services/academy/academy.service';
import { TokenStorageService } from '../../../../../core/services/auth/token-storage.service';
import { MatchCardModel } from '../../../../../core/models/Match/match-card.model';
import { MatchCardComponent } from '../../match-card/match-card.component';
import { Pagination } from '../../../../../shared/components/pagination/pagination';
import { LoadingSpinnerComponent } from '../../../../../shared/components/loading-spinner/loading-spinner';
import { EmptyStateComponent } from '../../../../../shared/components/empty-state/empty-state';
import { NavbarComponent } from '../../../../../shared/components/navbar/navbar';
import { Footer } from '../../../../../shared/components/footer/footer';
import { CustomSelect, SelectOption } from '../../../../../shared/components/custom-select/custom-select';
import { CustomDatePicker } from '../../../../../shared/components/custom-date-picker/custom-date-picker';
import { CustomButtonComponent } from '../../../../../shared/components/custom-button/custom-button';
import { ScrollRevealDirective } from '../../../../../shared/directives/scroll-reveal.directive';

@Component({
  selector: 'app-academy-match-list',
  standalone: true,
  imports: [
    CommonModule, MatchCardComponent, Pagination, LoadingSpinnerComponent,
    EmptyStateComponent, NavbarComponent, Footer, CustomSelect,
    CustomDatePicker, CustomButtonComponent, ScrollRevealDirective
  ],
  templateUrl: './academy-match-list.component.html',
  styleUrls: ['./academy-match-list.component.css']
})
export class AcademyMatchListComponent implements OnInit {
  private matchService = inject(MatchService);
  private academyService = inject(AcademyService);
  private tokenStorage = inject(TokenStorageService);
  private cdr = inject(ChangeDetectorRef);

  academyId: number = 0;
  matches: MatchCardModel[] = [];
  isLoading = false;
  error = '';
  filterError = '';

  currentPage = 1;
  pageSize = 20;
  totalItems = 0;

  selectedTeamId: number | null = null;
  selectedAgeGroupId: number | null = null;

  get isTeamLocked(): boolean {
    return this.selectedAgeGroupId != null && this.selectedAgeGroupId !== '' as any;
  }

  get isAgeGroupLocked(): boolean {
    return this.selectedTeamId != null && this.selectedTeamId !== '' as any;
  }

  get isDateLocked(): boolean {
    return this.selectedStatus === 'Live';
  }

  selectedStatus: string | null = null;
  selectedMatchType: string | null = null;
  selectedDateFrom = '';
  selectedDateTo = '';

  teamOptions: SelectOption[] = [];
  ageGroupOptions: SelectOption[] = [];

  statusOptions: SelectOption[] = [
    { value: '', label: 'All' },
    { value: 'Scheduled', label: 'Scheduled' },
    { value: 'Live', label: 'Live' },
    { value: 'Completed', label: 'Completed' }
  ];

  matchTypeOptions: SelectOption[] = [
    { value: '', label: 'All' },
    { value: 'Friendly', label: 'Friendly' },
    { value: 'Tournament', label: 'Tournament' },
    { value: 'Session', label: 'Session' }
  ];

  ngOnInit(): void {
    const user = this.tokenStorage.getUser();
    this.academyId = user?.academyId ?? 0;

    if (this.academyId) {
      this.loadDropdowns();
      this.loadMatches();
    }
  }

  loadDropdowns(): void {
    this.academyService.getTeams(this.academyId).subscribe({
      next: (res) => {
        const teams = res.data ?? [];
        this.teamOptions = [
          { value: '', label: 'All Teams' },
          ...teams.map(t => ({ value: t.id, label: t.name }))
        ];
      }
    });

    this.academyService.getAgeGroups(this.academyId).subscribe({
      next: (res) => {
        const groups = res.data ?? [];
        this.ageGroupOptions = [
          { value: '', label: 'All Age Groups' },
          ...groups.map(g => ({ value: g.id, label: g.name }))
        ];
      }
    });
  }

  loadMatches(): void {
    this.isLoading = true;
    this.error = '';

    this.matchService.getAcademyMatches(
      this.academyId,
      this.selectedTeamId ?? undefined,
      this.selectedAgeGroupId ?? undefined,
      this.selectedStatus ?? undefined,
      this.selectedMatchType ?? undefined,
      this.selectedDateFrom || undefined,
      this.selectedDateTo || undefined,
      this.currentPage,
      this.pageSize
    ).subscribe({
      next: (res) => {
        const academyTeamIds: number[] = res.data?.academyTeamIds ?? [];
        this.matches = (res.data?.matches ?? []).map(m => {
          if (m.status !== 'Completed') return m;
          if (m.homeTeamId === m.awayTeamId) return m;

          const isCoachHome = academyTeamIds.includes(m.homeTeamId);
          const isCoachAway = academyTeamIds.includes(m.awayTeamId);
          if (isCoachHome && isCoachAway) return m;

          let side: 'home' | 'away' | undefined;
          if (isCoachHome) side = 'home';
          else if (isCoachAway) side = 'away';
          else return m;

          m.coachSide = side;

          if (side === 'home') {
            if (m.homeScore > m.awayScore) m.coachOutcome = 'win';
            else if (m.homeScore < m.awayScore) m.coachOutcome = 'loss';
            else {
              const hasPens = m.homePenaltyScore != null && m.awayPenaltyScore != null;
              if (hasPens && m.homePenaltyScore! > m.awayPenaltyScore!) m.coachOutcome = 'win';
              else if (hasPens && m.homePenaltyScore! < m.awayPenaltyScore!) m.coachOutcome = 'loss';
              else m.coachOutcome = 'draw';
            }
          } else {
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
        this.error = 'Failed to load matches.';
        this.cdr.detectChanges();
      }
    });
  }

  applyFilters(): void {
    this.filterError = '';

    if (this.selectedDateFrom && this.selectedDateTo && this.selectedDateFrom > this.selectedDateTo) {
      this.filterError = '"From" date must be earlier than "To" date.';
      return;
    }

    this.currentPage = 1;
    this.loadMatches();
  }

  clearFilters(): void {
    this.selectedTeamId = null;
    this.selectedAgeGroupId = null;
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

  onTeamChange(): void {
    if (this.selectedTeamId != null && this.selectedTeamId !== ('' as any)) {
      this.selectedAgeGroupId = null;
    }
  }

  onAgeGroupChange(): void {
    if (this.selectedAgeGroupId != null && this.selectedAgeGroupId !== ('' as any)) {
      this.selectedTeamId = null;
    }
  }

  onMatchClick(matchId: number): void {
  }
}
