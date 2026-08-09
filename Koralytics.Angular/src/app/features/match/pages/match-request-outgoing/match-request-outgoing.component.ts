import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { MatchService } from '../../../../../core/services/match/match.service';
import { CoachSquadService } from '../../../../../core/services/coach/coach-squad.service';
import { MatchRequestModel } from '../../../../../core/models/Match/match-request.model';
import { Pagination } from '../../../../../shared/components/pagination/pagination';
import { LoadingSpinnerComponent } from '../../../../../shared/components/loading-spinner/loading-spinner';
import { EmptyStateComponent } from '../../../../../shared/components/empty-state/empty-state';
import { NavbarComponent } from '../../../../../shared/components/navbar/navbar';
import { Footer } from '../../../../../shared/components/footer/footer';
import { CustomSelect, SelectOption } from '../../../../../shared/components/custom-select/custom-select';
import { CustomDatePicker } from '../../../../../shared/components/custom-date-picker/custom-date-picker';
import { CustomButtonComponent } from '../../../../../shared/components/custom-button/custom-button';
import { ScrollRevealDirective } from '../../../../../shared/directives/scroll-reveal.directive';
import { MarqueeIfOverflowDirective } from '../../match-timeline/marquee-if-overflow.directive';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';

@Component({
  selector: 'app-match-request-outgoing',
  standalone: true,
  imports: [
    CommonModule, RouterLink, Pagination, LoadingSpinnerComponent,
    EmptyStateComponent, NavbarComponent, Footer, CustomSelect,
    CustomDatePicker, CustomButtonComponent, ScrollRevealDirective,
    MarqueeIfOverflowDirective, TranslatePipe
  ],
  templateUrl: './match-request-outgoing.component.html',
  styleUrls: ['./match-request-outgoing.component.css']
})
export class MatchRequestOutgoingComponent implements OnInit {
  private matchService = inject(MatchService);
  private coachSquadService = inject(CoachSquadService);
  private cdr = inject(ChangeDetectorRef);
  private translate = inject(TranslateService);

  // Teams dropdown
  coachTeams: SelectOption[] = [];
  selectedTeamId: number | null = null;
  isLoadingTeams = true;

  // List state
  requests: MatchRequestModel[] = [];
  isLoading = false;
  error = '';
  filterError = '';

  // Pagination
  currentPage = 1;
  pageSize = 20;
  totalItems = 0;

  // Filters
  selectedStatus: string | null = 'All';
  selectedDateFrom = '';
  selectedDateTo = '';

  get statusOptions(): SelectOption[] {
    return [
      { value: 'All',      label: this.translate.instant('MATCH.REQUEST_STATUS.ALL') },
      { value: 'Pending',  label: this.translate.instant('MATCH.REQUEST_STATUS.PENDING') },
      { value: 'Accepted', label: this.translate.instant('MATCH.REQUEST_STATUS.ACCEPTED') },
      { value: 'Declined', label: this.translate.instant('MATCH.REQUEST_STATUS.DECLINED') }
    ];
  }

  ngOnInit(): void {
    this.loadCoachTeams();
  }

  loadCoachTeams(): void {
    this.isLoadingTeams = true;
    this.coachSquadService.getCoachTeams().subscribe({
      next: (res: any) => {
        const teams = res?.data ?? res ?? [];
        this.coachTeams = teams.map((t: any) => ({
          value: t.teamId ?? t.TeamId,
          label: `${t.teamName ?? t.TeamName} (${t.ageGroupName ?? t.AgeGroupName})`
        }));
        if (this.coachTeams.length > 0) {
          this.selectedTeamId = this.coachTeams[0].value as number;
          this.loadRequests();
        }
        this.isLoadingTeams = false;
      },
      error: () => {
        this.error = this.translate.instant('MATCH.ERRORS.GENERIC_LOAD');
        this.isLoadingTeams = false;
        this.cdr.detectChanges();
      }
    });
  }

  loadRequests(): void {
    if (!this.selectedTeamId) return;
    this.isLoading = true;
    this.error = '';

    const statusParam = (this.selectedStatus && this.selectedStatus !== 'All') ? this.selectedStatus : undefined;

    this.matchService.getOutgoingRequests(
      this.selectedTeamId,
      this.currentPage,
      this.pageSize,
      statusParam,
      this.selectedDateFrom || undefined,
      this.selectedDateTo || undefined
    ).subscribe({
      next: (res) => {
        this.requests = res.data?.requests ?? [];
        this.totalItems = res.data?.totalCount ?? 0;
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.isLoading = false;
        this.error = this.translate.instant('MATCH.REQUESTS.ERROR_LOAD');
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
    this.loadRequests();
  }

  clearFilters(): void {
    this.selectedStatus = 'All';
    this.selectedDateFrom = '';
    this.selectedDateTo = '';
    this.filterError = '';
    this.currentPage = 1;
    this.loadRequests();
  }

  onTeamChange(): void {
    this.currentPage = 1;
    this.loadRequests();
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

  onPageChange(page: number): void {
    this.currentPage = page;
    this.loadRequests();
  }

  formatLabel(format: string): string {
    const map: Record<string, string> = {
      ElevenSide: this.translate.instant('MATCH.FORMAT.ELEVEN_SIDE'),
      SevenSide:  this.translate.instant('MATCH.FORMAT.SEVEN_SIDE'),
      FiveSide:   this.translate.instant('MATCH.FORMAT.FIVE_SIDE')
    };
    return map[format] ?? format;
  }
}
