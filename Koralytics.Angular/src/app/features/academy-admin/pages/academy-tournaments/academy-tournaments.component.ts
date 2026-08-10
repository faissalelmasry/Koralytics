import { Component, OnInit, inject, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { TournamentService } from '../../../../../core/services/tournament/tournament.service';
import { AcademyService } from '../../../../../core/services/academy/academy.service';
import { TokenStorageService } from '../../../../../core/services/auth/token-storage.service';
import { ToastService } from '../../../../../core/services/Toast/toast';
import { SearchBarComponent } from '../../../../../shared/components/search-bar/search-bar';
import { StatusChipComponent } from '../../../../../shared/components/status-chip/status-chip';
import { EmptyStateComponent } from '../../../../../shared/components/empty-state/empty-state';
import { Pagination } from '../../../../../shared/components/pagination/pagination';
import { CustomButtonComponent } from '../../../../../shared/components/custom-button/custom-button';
import { ScrollRevealDirective } from '../../../../../shared/directives/scroll-reveal.directive';

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
    ScrollRevealDirective
  ],
  templateUrl: './academy-tournaments.component.html',
  styleUrls: ['./academy-tournaments.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AcademyTournamentsComponent implements OnInit {
  private tournamentService = inject(TournamentService);
  private academyService = inject(AcademyService);
  private tokenStorage = inject(TokenStorageService);
  private toast = inject(ToastService);
  private router = inject(Router);
  private cdr = inject(ChangeDetectorRef);

  academyId: number | null = null;
  invitations: any[] = [];
  filteredInvitations: any[] = [];
  paginatedInvitations: any[] = [];
  isLoading = true;
  acceptingIds = new Set<number>(); // track which invitations are being accepted

  // Modal State for Team Selection
  showTeamModal = false;
  selectedInvite: any = null;
  modalTeams: any[] = [];
  isLoadingModalTeams = false;
  selectedTeamId: number | null = null;
  modalError = '';

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
    this.openTeamSelectionModal(invite, event);
  }

  openTeamSelectionModal(invite: any, event: Event) {
    event.stopPropagation();
    if (!this.academyId || invite.status === 'Accepted') return;

    this.selectedInvite = invite;
    this.showTeamModal = true;
    this.isLoadingModalTeams = true;
    this.modalError = '';
    this.selectedTeamId = invite.teamId || null;
    this.modalTeams = [];
    this.cdr.markForCheck();

    forkJoin({
      teamsRes: this.academyService.getTeams(this.academyId).pipe(catchError(() => of(null))),
      tournamentRes: this.tournamentService.getTournamentById(invite.tournamentId).pipe(catchError(() => of(null)))
    }).subscribe({
      next: (res) => {
        const teamsData = res.teamsRes?.data || res.teamsRes || [];
        const allTeams = Array.isArray(teamsData) ? teamsData : [];
        const tournament = res.tournamentRes?.data || res.tournamentRes || null;

        const ageGroupId = tournament?.ageGroupId;
        if (ageGroupId) {
          this.modalTeams = allTeams.filter((t: any) => t.ageGroupId === ageGroupId);
        }

        // Fallback if no matching ageGroup teams or if filter returns empty
        if (!this.modalTeams.length) {
          this.modalTeams = allTeams;
        }

        if (this.modalTeams.length > 0) {
          const exists = this.modalTeams.some((t: any) => t.id === this.selectedTeamId);
          if (!exists) {
            this.selectedTeamId = this.modalTeams[0].id;
          }
        }

        this.isLoadingModalTeams = false;
        this.cdr.markForCheck();
      },
      error: () => {
        this.isLoadingModalTeams = false;
        this.modalError = 'Unable to load academy teams. Please try again.';
        this.cdr.markForCheck();
      }
    });
  }

  selectModalTeam(teamId: number) {
    this.selectedTeamId = teamId;
    this.cdr.markForCheck();
  }

  confirmAcceptInvitation() {
    if (!this.selectedInvite || !this.academyId || !this.selectedTeamId) return;

    const invite = this.selectedInvite;
    const teamIdToAssign = this.selectedTeamId;
    const selectedTeamObj = this.modalTeams.find((t: any) => t.id === teamIdToAssign);
    const newTeamName = selectedTeamObj?.name || invite.teamName;

    this.acceptingIds.add(invite.tournamentTeamId);
    this.cdr.markForCheck();

    this.tournamentService.acceptInvitation(invite.tournamentId, this.academyId, teamIdToAssign).subscribe({
      next: () => {
        invite.status = 'Accepted';
        invite.teamId = teamIdToAssign;
        invite.teamName = newTeamName;

        this.acceptingIds.delete(invite.tournamentTeamId);
        this.toast.show(`Invitation accepted with ${newTeamName}!`, 'success');
        this.closeTeamModal();
        this.applyFilters();
        this.cdr.markForCheck();
      },
      error: (err: any) => {
        this.acceptingIds.delete(invite.tournamentTeamId);
        const errMsg = err?.error?.message || err?.error || 'Unable to accept invitation. Please try again.';
        this.modalError = typeof errMsg === 'string' ? errMsg : 'Unable to accept invitation.';
        this.toast.show(this.modalError, 'error');
        this.cdr.markForCheck();
      }
    });
  }

  closeTeamModal() {
    this.showTeamModal = false;
    this.selectedInvite = null;
    this.modalTeams = [];
    this.selectedTeamId = null;
    this.modalError = '';
    this.cdr.markForCheck();
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
