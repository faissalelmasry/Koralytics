import { Component, OnInit, inject, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { TournamentService } from '../../../../../core/services/tournament/tournament.service';
import { Tournament, TournamentStatus, MatchFormat, TournamentStructure } from '../../../../../core/interfaces/tournament.models';
import { CardComponent } from '../../../../../shared/components/card/card';
import { CustomButtonComponent } from '../../../../../shared/components/custom-button/custom-button';
import { SearchBarComponent } from '../../../../../shared/components/search-bar/search-bar';
import { CustomSelect } from '../../../../../shared/components/custom-select/custom-select';
import { EmptyStateComponent } from '../../../../../shared/components/empty-state/empty-state';
import { StatusChipComponent } from '../../../../../shared/components/status-chip/status-chip';
import { Pagination } from '../../../../../shared/components/pagination/pagination';
import { LoadingSpinnerComponent } from '../../../../../shared/components/loading-spinner/loading-spinner';
import { ScrollRevealDirective } from '../../../../../shared/directives/scroll-reveal.directive';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { LocalizedDatePipe } from '../../../../../shared/pipes/localized-date.pipe';

import { AuthService } from '../../../../../core/services/auth/auth.service';

@Component({
  selector: 'app-tournament-list',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    FormsModule,
    CustomButtonComponent,
    SearchBarComponent,
    CustomSelect,
    EmptyStateComponent,
    StatusChipComponent,
    Pagination,
    LoadingSpinnerComponent,
    ScrollRevealDirective,
    TranslatePipe,
    LocalizedDatePipe
  ],
  templateUrl: './tournament-list.component.html',
  styleUrls: ['./tournament-list.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TournamentListComponent implements OnInit {
  private tournamentService = inject(TournamentService);
  private authService = inject(AuthService);
  private router = inject(Router);
  private cdr = inject(ChangeDetectorRef);
  private translate = inject(TranslateService);

  isSystemAdmin = false;
  tournaments: Tournament[] = [];
  filteredTournaments: Tournament[] = [];
  paginatedTournaments: Tournament[] = [];
  isLoading = true;

  // Search & Filter state
  searchText = '';
  selectedFormat = '';
  selectedStructure = '';
  selectedStatus = '';

  // Pagination state
  currentPage = 1;
  pageSize = 6;

  get formatOptions() {
    return [
      { value: '', label: this.translate.instant('TOURNAMENT.LIST.FILTERS.ALL_FORMATS') },
      { value: 'FiveSide', label: this.translate.instant('TOURNAMENT.FORMAT.FIVE_SIDE') },
      { value: 'SevenSide', label: this.translate.instant('TOURNAMENT.FORMAT.SEVEN_SIDE') },
      { value: 'ElevenSide', label: this.translate.instant('TOURNAMENT.FORMAT.ELEVEN_SIDE') }
    ];
  }

  get structureOptions() {
    return [
      { value: '', label: this.translate.instant('TOURNAMENT.LIST.FILTERS.ALL_STATUSES') }, // Can reuse or need specific 'all structures' if we had one
      { value: 'Knockout', label: this.translate.instant('TOURNAMENT.STRUCTURE.KNOCKOUT') },
      { value: 'GroupAndKnockout', label: this.translate.instant('TOURNAMENT.STRUCTURE.GROUP_AND_KNOCKOUT') },
      { value: 'League', label: this.translate.instant('TOURNAMENT.STRUCTURE.LEAGUE') }
    ];
  }

  get statusOptions() {
    return [
      { value: '', label: this.translate.instant('TOURNAMENT.LIST.FILTERS.ALL_STATUSES') },
      { value: 'Draft', label: this.translate.instant('TOURNAMENT.STATUS.DRAFT') },
      { value: 'Registration', label: this.translate.instant('TOURNAMENT.STATUS.REGISTRATION') },
      { value: 'InProgress', label: this.translate.instant('TOURNAMENT.STATUS.IN_PROGRESS') },
      { value: 'Completed', label: this.translate.instant('TOURNAMENT.STATUS.COMPLETED') }
    ];
  }

  ngOnInit() {
    this.authService.currentUser$.subscribe(user => {
      this.isSystemAdmin = user?.roles?.includes('SystemAdmin') ?? false;
      this.cdr.markForCheck();
    });
    this.loadTournaments();
  }

  loadTournaments() {
    this.isLoading = true;
    this.tournamentService.getTournaments().subscribe({
      next: (response: any) => {
        // Handle custom API envelope structure: response.data
        const data = response?.data || response;
        this.tournaments = Array.isArray(data) ? data : [];
        this.applyFilters();
        this.isLoading = false;
        this.cdr.markForCheck();
      },
      error: (err) => {
        console.error('Failed to load tournaments from API', err);
        this.tournaments = [];
        this.applyFilters();
        this.isLoading = false;
        this.cdr.markForCheck();
      }
    });
  }

  applyFilters() {
    const query = (this.searchText || '').toLowerCase().trim();
    this.filteredTournaments = this.tournaments.filter(t => {
      const name = (t.name || '').toLowerCase();
      const ageGroup = (t.ageGroupName || '').toLowerCase();
      const matchesSearch = !query || name.includes(query) || ageGroup.includes(query);
      const matchesFormat = !this.selectedFormat || t.format === this.selectedFormat;
      const matchesStructure = !this.selectedStructure || t.structure === this.selectedStructure;
      const matchesStatus = !this.selectedStatus || t.status === this.selectedStatus;

      return matchesSearch && matchesFormat && matchesStructure && matchesStatus;
    });
    this.currentPage = 1;
    this.updatePagination();
  }

  onSearchChange(search: string) {
    this.searchText = search;
    this.applyFilters();
  }

  onFormatChange(format: string) {
    this.selectedFormat = format;
    this.applyFilters();
  }

  onStructureChange(structure: string) {
    this.selectedStructure = structure;
    this.applyFilters();
  }

  onStatusChange(status: string) {
    this.selectedStatus = status;
    this.applyFilters();
  }

  onPageChange(page: number) {
    this.currentPage = page;
    this.updatePagination();
  }

  updatePagination() {
    const startIndex = (this.currentPage - 1) * this.pageSize;
    this.paginatedTournaments = this.filteredTournaments.slice(startIndex, startIndex + this.pageSize);
  }

  goToDetails(tournamentId: number) {
    this.router.navigate(['/tournament/details', tournamentId]);
  }

  /* ── Summary stats for the overview cards ── */
  get totalCount(): number {
    return this.tournaments.length;
  }

  get activeCount(): number {
    return this.tournaments.filter(t => t.status === TournamentStatus.InProgress).length;
  }

  get registrationCount(): number {
    return this.tournaments.filter(t => t.status === TournamentStatus.Registration).length;
  }

  get completedCount(): number {
    return this.tournaments.filter(t => t.status === TournamentStatus.Completed).length;
  }

  /* ── Helpers ── */
  getChipType(status: TournamentStatus): 'success' | 'danger' | 'info' | 'warning' {
    switch (status) {
      case TournamentStatus.InProgress:
        return 'info';
      case TournamentStatus.Completed:
        return 'success';
      case TournamentStatus.Registration:
        return 'warning';
      default:
        return 'danger';
    }
  }

  getFormatIcon(format: MatchFormat): string {
    switch (format) {
      case MatchFormat.FiveSide: return '5v5';
      case MatchFormat.SevenSide: return '7v7';
      case MatchFormat.ElevenSide: return '11v11';
      default: return '—';
    }
  }

  getFormatLabel(format: MatchFormat): string {
    switch (format) {
      case MatchFormat.FiveSide: return this.translate.instant('TOURNAMENT.FORMAT.FIVE_SIDE');
      case MatchFormat.SevenSide: return this.translate.instant('TOURNAMENT.FORMAT.SEVEN_SIDE');
      case MatchFormat.ElevenSide: return this.translate.instant('TOURNAMENT.FORMAT.ELEVEN_SIDE');
      default: return format;
    }
  }

  getStructureLabel(structure: TournamentStructure): string {
    switch (structure) {
      case TournamentStructure.GroupAndKnockout: return this.translate.instant('TOURNAMENT.STRUCTURE.GROUP_AND_KNOCKOUT');
      case TournamentStructure.Knockout: return this.translate.instant('TOURNAMENT.STRUCTURE.KNOCKOUT');
      case TournamentStructure.League: return this.translate.instant('TOURNAMENT.STRUCTURE.LEAGUE');
      default: return structure;
    }
  }

  hasActiveFilters(): boolean {
    return !!this.searchText || !!this.selectedFormat || !!this.selectedStructure || !!this.selectedStatus;
  }

  clearFilters() {
    this.searchText = '';
    this.selectedFormat = '';
    this.selectedStructure = '';
    this.selectedStatus = '';
    this.applyFilters();
  }
}
