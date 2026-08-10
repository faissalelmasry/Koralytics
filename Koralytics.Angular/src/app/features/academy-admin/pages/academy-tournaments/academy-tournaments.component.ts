import { Component, OnInit, inject, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { TournamentService } from '../../../../../core/services/tournament/tournament.service';
import { TokenStorageService } from '../../../../../core/services/auth/token-storage.service';
import { ToastService } from '../../../../../core/services/Toast/toast';
import { SearchBarComponent } from '../../../../../shared/components/search-bar/search-bar';
import { StatusChipComponent } from '../../../../../shared/components/status-chip/status-chip';
import { EmptyStateComponent } from '../../../../../shared/components/empty-state/empty-state';
import { Pagination } from '../../../../../shared/components/pagination/pagination';
import { CustomButtonComponent } from '../../../../../shared/components/custom-button/custom-button';
import { ScrollRevealDirective } from '../../../../../shared/directives/scroll-reveal.directive';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';

@Component({
  selector: 'app-academy-tournaments',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    FormsModule,
    SearchBarComponent,
    StatusChipComponent,
    EmptyStateComponent,
    Pagination,
    CustomButtonComponent,
    ScrollRevealDirective,
    TranslatePipe
  ],
  templateUrl: './academy-tournaments.component.html',
  styleUrls: ['./academy-tournaments.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AcademyTournamentsComponent implements OnInit {
  private tournamentService = inject(TournamentService);
  private tokenStorage = inject(TokenStorageService);
  private toast = inject(ToastService);
  private router = inject(Router);
  private cdr = inject(ChangeDetectorRef);
  private translate = inject(TranslateService);

  academyId: number | null = null;
  invitations: any[] = [];
  filteredInvitations: any[] = [];
  paginatedInvitations: any[] = [];
  isLoading = true;
  acceptingIds = new Set<number>(); // track which invitations are being accepted

  // Search & Filter
  searchText = '';
  selectedStatus = '';

  // Pagination
  currentPage = 1;
  pageSize = 6;

  statusOptions = [
    { value: '', label: 'All Status' },
    { value: 'Invited', label: 'Invited' },
    { value: 'Accepted', label: 'Accepted' }
  ];

  ngOnInit() {
    const user = this.tokenStorage.getUser();
    this.academyId = user?.academyId || null;

    if (this.academyId) {
      this.loadInvitations();
    } else {
      this.isLoading = false;
      this.cdr.markForCheck();
    }
  }

  loadInvitations() {
    if (!this.academyId) return;
    this.isLoading = true;
    this.cdr.markForCheck();

    this.tournamentService.getTournamentInvitationsForAcademy(this.academyId).subscribe({
      next: (response: any) => {
        const data = response?.data || response;
        this.invitations = Array.isArray(data) ? data : [];
        this.applyFilters();
        this.isLoading = false;
        this.cdr.markForCheck();
      },
      error: () => {
        this.invitations = [];
        this.applyFilters();
        this.isLoading = false;
        this.cdr.markForCheck();
      }
    });
  }

  applyFilters() {
    this.filteredInvitations = this.invitations.filter(inv => {
      const matchesSearch =
        (inv.tournamentName || '').toLowerCase().includes(this.searchText.toLowerCase()) ||
        (inv.teamName || '').toLowerCase().includes(this.searchText.toLowerCase());
      const matchesStatus = !this.selectedStatus || inv.status === this.selectedStatus;
      return matchesSearch && matchesStatus;
    });
    this.currentPage = 1;
    this.updatePagination();
  }

  onSearchChange(search: string) {
    this.searchText = search;
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
    const start = (this.currentPage - 1) * this.pageSize;
    this.paginatedInvitations = this.filteredInvitations.slice(start, start + this.pageSize);
  }

  goToDetails(tournamentId: number) {
    this.router.navigate(['/tournament/details', tournamentId]);
  }

  goToSquadRegistration(tournamentId: number, teamId: number, event: Event) {
    event.stopPropagation();
    this.router.navigate(['/tournament', tournamentId, 'squad-registration'], {
      queryParams: { teamId }
    });
  }

  acceptInvitation(invite: any, event: Event) {
    event.stopPropagation();
    if (!this.academyId) return;
    this.acceptingIds.add(invite.tournamentTeamId);
    this.cdr.markForCheck();

    this.tournamentService.acceptInvitation(invite.tournamentId, this.academyId).subscribe({
      next: () => {
        invite.status = 'Accepted';
        this.acceptingIds.delete(invite.tournamentTeamId);
        this.toast.show(this.translate.instant('ACADEMY_ADMIN.MESSAGES.TOURNAMENT_ACCEPTED'), 'success');
        this.applyFilters();
        this.cdr.markForCheck();
      },
      error: () => {
        this.acceptingIds.delete(invite.tournamentTeamId);
        this.toast.show(this.translate.instant('ACADEMY_ADMIN.MESSAGES.ACCEPT_TOURNAMENT_ERROR'), 'error');
        this.cdr.markForCheck();
      }
    });
  }

  isAccepting(id: number): boolean {
    return this.acceptingIds.has(id);
  }

  /* ── Summary stats ── */
  get totalCount(): number {
    return this.invitations.length;
  }

  get invitedCount(): number {
    return this.invitations.filter(i => i.status === 'Invited').length;
  }

  get acceptedCount(): number {
    return this.invitations.filter(i => i.status === 'Accepted').length;
  }

  get uniqueTournamentCount(): number {
    return new Set(this.invitations.map(i => i.tournamentId)).size;
  }

  /* ── Helpers ── */
  getChipType(status: string): 'success' | 'danger' | 'info' | 'warning' {
    switch (status) {
      case 'Accepted': return 'success';
      case 'Invited': return 'warning';
      case 'Rejected': return 'danger';
      default: return 'info';
    }
  }

  hasActiveFilters(): boolean {
    return !!this.searchText || !!this.selectedStatus;
  }

  clearFilters() {
    this.searchText = '';
    this.selectedStatus = '';
    this.applyFilters();
  }
}
