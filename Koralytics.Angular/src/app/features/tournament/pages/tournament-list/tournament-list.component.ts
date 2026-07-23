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
import { ScrollRevealDirective } from '../../../../../shared/directives/scroll-reveal.directive';

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
    ScrollRevealDirective
  ],
  templateUrl: './tournament-list.component.html',
  styleUrls: ['./tournament-list.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TournamentListComponent implements OnInit {
  private tournamentService = inject(TournamentService);
  private router = inject(Router);
  private cdr = inject(ChangeDetectorRef);

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

  formatOptions = [
    { value: '', label: 'all formats' },
    { value: 'FiveSide', label: '5 vs 5' },
    { value: 'SevenSide', label: '7 vs 7' },
    { value: 'ElevenSide', label: '11 vs 11' }
  ];

  structureOptions = [
    { value: '', label: 'all structures' },
    { value: 'Knockout', label: 'Knockout' },
    { value: 'GroupAndKnockout', label: 'Group & Knockout' },
    { value: 'League', label: 'League' }
  ];

  statusOptions = [
    { value: '', label: 'all status' },
    { value: 'Draft', label: 'Draft' },
    { value: 'Registration', label: 'Registration' },
    { value: 'InProgress', label: 'In Progress' },
    { value: 'Completed', label: 'Completed' }
  ];

  ngOnInit() {
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
    this.filteredTournaments = this.tournaments.filter(t => {
      const matchesSearch = t.name.toLowerCase().includes(this.searchText.toLowerCase()) ||
                            t.ageGroupName.toLowerCase().includes(this.searchText.toLowerCase());
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
      case MatchFormat.FiveSide: return '5 vs 5';
      case MatchFormat.SevenSide: return '7 vs 7';
      case MatchFormat.ElevenSide: return '11 vs 11';
      default: return format;
    }
  }

  getStructureLabel(structure: TournamentStructure): string {
    switch (structure) {
      case TournamentStructure.GroupAndKnockout: return 'Group & Knockout';
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
