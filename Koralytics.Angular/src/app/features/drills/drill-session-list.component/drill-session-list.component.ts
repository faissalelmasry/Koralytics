import { Component, OnInit, OnDestroy, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { Subject, Subscription } from 'rxjs';
import { debounceTime, distinctUntilChanged, finalize } from 'rxjs/operators';
import { DrillSessionService } from '../../../../core/services/drill/drill-session.service';
import { AuthService } from '../../../../core/services/auth/auth.service';
import { CustomButtonComponent } from '../../../../shared/components/custom-button/custom-button';
import {
  DrillSessionDto,
  SessionFilterDto,
  UpdateDrillSessionDto
} from '../../../../core/interfaces/drill-session.model';
import { Router } from '@angular/router';
import { SessionStatus, SessionType } from '../../../../core/enums/koralytics.enums';
import { Pagination } from '../../../../shared/components/pagination/pagination';

import { CustomSelect, SelectOption } from '../../../../shared/components/custom-select/custom-select';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner';
import { StatusChipComponent } from '../../../../shared/components/status-chip/status-chip';
import { CustomDatePicker } from '../../../../shared/components/custom-date-picker/custom-date-picker';
import { formatToLocalISO } from '../../../../core/utils/date.util';

@Component({
  selector: 'app-drill-session-list',
  standalone: true,
  imports: [
    CommonModule, 
    FormsModule, 
    ReactiveFormsModule, 
    Pagination, 
    CustomButtonComponent,
    CustomSelect,
    LoadingSpinnerComponent,
    StatusChipComponent,
    CustomDatePicker
  ],
  templateUrl: './drill-session-list.component.html',
  styleUrls: ['./drill-session-list.component.css']
})
export class DrillSessionListComponent implements OnInit, OnDestroy {
  // --- Data Arrays ---
  sessions: DrillSessionDto[] = [];
  availableTeams: { id: number, name: string }[] = [];

  // --- UI States ---
  isLoading = false;
  errorMessage = '';
  toastMessage = '';
  toastType: 'success' | 'error' = 'success';

  // --- Confirm Modal State ---
  confirmModal = {
    isOpen: false,
    title: '',
    message: '',
    confirmText: '',
    action: () => { }
  };

  // --- Edit Modal States ---
  isEditModalOpen = false;
  isSaving = false;
  editSessionData: any = {};

  // --- Filtering & Pagination ---
  filter: SessionFilterDto = {
    pageNumber: 1,
    pageSize: 6,
    teamId: null,
    status: null,
    fromDate: null,
    toDate: null
  };

  totalItems = 0;
  totalPages = 1;
  pagesArray: number[] = [];

  // --- Computed Stats ---
  scheduledCount = 0;
  inProgressCount = 0;
  completedCount = 0;

  // --- Enums for Template Access ---
  SessionStatus = SessionStatus;
  SessionType = SessionType;

  // --- Dropdown Binding ---
  selectedStatus: string = ''; // '' means All Statuses

  get teamOptions(): SelectOption[] {
    return [
      { value: 0, label: 'All Teams' },
      ...this.availableTeams.map(t => ({ value: t.id, label: t.name }))
    ];
  }

  get statusOptions(): SelectOption[] {
    return [
      { value: '', label: 'All Statuses' },
      { value: '0', label: 'Scheduled' },
      { value: '1', label: 'In Progress' },
      { value: '2', label: 'Completed' },
      { value: '3', label: 'Cancelled' }
    ];
  }

  get editStatusOptions(): SelectOption[] {
    return [
      { value: SessionStatus.Scheduled, label: 'Scheduled' },
      { value: SessionStatus.InProgress, label: 'In Progress' },
      { value: SessionStatus.Cancelled, label: 'Cancelled' }
    ];
  }

  onEditStatusChange(val: any): void {
    this.editSessionData.status = val != null ? Number(val) : SessionStatus.Scheduled;
  }

  onTeamSelect(teamIdVal: any): void {
    const teamId = Number(teamIdVal) || 0;
    this.filter.teamId = teamId === 0 ? null : teamId;
    this.filter.pageNumber = 1;
    this.fetchSessions();
  }

  onStatusSelect(statusVal: any): void {
    this.selectedStatus = statusVal != null ? String(statusVal) : '';
    this.onStatusChange(this.selectedStatus);
  }

  getStatusChipType(status: any): 'success' | 'warning' | 'danger' | 'info' {
    const s = Number(status);
    if (s === SessionStatus.Completed) return 'success';
    if (s === SessionStatus.InProgress) return 'warning';
    if (s === SessionStatus.Cancelled) return 'danger';
    return 'info';
  }

  get isAdmin(): boolean {
    const user = this.authService.getCurrentUserValue();
    if (!user || !user.roles) return false;
    return user.roles.some(r =>
      r.toLowerCase() === 'academyadmin' ||
      r.toLowerCase() === 'admin' ||
      r.toLowerCase() === 'systemadmin'
    );
  }

  get canManageDrills(): boolean {
    const user = this.authService.getCurrentUserValue();
    if (!user || !user.roles) return false;
    const isAcademyAdmin = user.roles.some(r => r.toLowerCase() === 'academyadmin');
    if (isAcademyAdmin) return false;
    return user.roles.some(r =>
      r.toLowerCase() === 'coach' ||
      r.toLowerCase() === 'systemadmin' ||
      r.toLowerCase() === 'admin'
    );
  }

  constructor(
    private sessionService: DrillSessionService,
    private authService: AuthService,
    private router: Router,
    private cdr: ChangeDetectorRef
  ) { }

  ngOnInit(): void {
    this.fetchSessions();
  }

  ngOnDestroy(): void {
  }

  // ==========================================
  // DATA FETCHING
  // ==========================================

  fetchSessions(): void {
    this.isLoading = true;
    this.errorMessage = '';

    this.sessionService.getCoachSessions(this.filter).pipe(
      finalize(() => this.isLoading = false)
    ).subscribe({
      next: (response: any) => {
        const items = Array.isArray(response) ? response : (response.items || response.data || []);
        this.sessions = items.map((s: any) => {
          let dateVal = s.sessionDate;
          if (!dateVal || dateVal.startsWith('0001') || dateVal.startsWith('0000')) {
            dateVal = s.createdAt || new Date().toISOString();
          }
          return {
            ...s,
            sessionDate: dateVal && !dateVal.endsWith('Z') && !dateVal.includes('+') ? dateVal + 'Z' : dateVal
          };
        });
        this.totalItems = response.totalCount || this.sessions.length;

        this.populateTeamsDropdown();
        this.calculateStats();
        this.calculatePagination();
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.errorMessage = err.error?.message || 'Failed to load training sessions.';
        this.cdr.detectChanges();
      }
    });
  }

  private populateTeamsDropdown(): void {
    this.sessions.forEach(session => {
      if (session.teamId && session.teamName) {
        if (!this.availableTeams.some(t => t.id === session.teamId)) {
          this.availableTeams.push({ id: session.teamId, name: session.teamName });
        }
      }
    });
  }

  private calculateStats(): void {
    this.scheduledCount = this.sessions.filter(s => s.status === SessionStatus.Scheduled).length;
    this.inProgressCount = this.sessions.filter(s => s.status === SessionStatus.InProgress).length;
    this.completedCount = this.sessions.filter(s => s.status === SessionStatus.Completed).length;
  }

  private calculatePagination(): void {
    this.totalPages = Math.ceil(this.totalItems / this.filter.pageSize) || 1;
    this.pagesArray = Array.from({ length: this.totalPages }, (_, i) => i + 1);
  }

  filterDateStr: string = '';

  onDatePickerChange(dateStr: string): void {
    this.filterDateStr = dateStr;
    if (dateStr) {
      this.filter.fromDate = `${dateStr}T00:00:00Z`;
      this.filter.toDate = `${dateStr}T23:59:59Z`;
    } else {
      this.filter.fromDate = null;
      this.filter.toDate = null;
    }
    this.filter.pageNumber = 1;
    this.fetchSessions();
  }

  onMonthChange(event: Event): void {
    const target = event.target as HTMLInputElement;
    const val = target.value;

    if (val) {
      const [year, month] = val.split('-');
      this.filter.fromDate = `${year}-${month}-01T00:00:00Z`;
      const lastDay = new Date(Number(year), Number(month), 0).getDate();
      this.filter.toDate = `${year}-${month}-${lastDay}T23:59:59Z`;
    } else {
      this.filter.fromDate = null;
      this.filter.toDate = null;
    }

    this.filter.pageNumber = 1;
    this.fetchSessions();
  }

  onStatusChange(val: string): void {
    if (val === '' || val === null || val === undefined) {
      this.filter.status = null;
    } else {
      this.filter.status = Number(val) as SessionStatus;
    }
    this.filter.pageNumber = 1;
    this.fetchSessions();
  }

  onTeamChange(event: Event): void {
    const target = event.target as HTMLSelectElement;
    const val = target.value;
    this.filter.teamId = val && val !== '0' ? Number(val) : null;
    this.filter.pageNumber = 1;
    this.fetchSessions();
  }

  changePage(page: number): void {
    if (page >= 1 && page <= this.totalPages && page !== this.filter.pageNumber) {
      this.filter.pageNumber = page;
      this.fetchSessions();
    }
  }

  // --- Cancel Modal States ---
  isCancelModalOpen = false;
  isCancelling = false;
  cancelSessionData: { id: number; teamName: string; reason: string } = { id: 0, teamName: '', reason: '' };
  cancelError = '';

  openCancelModal(session: any): void {
    this.cancelSessionData = {
      id: session.id,
      teamName: session.teamName || `Team #${session.teamId}`,
      reason: ''
    };
    this.cancelError = '';
    this.isCancelModalOpen = true;
    this.cdr.detectChanges();
  }

  closeCancelModal(): void {
    this.isCancelModalOpen = false;
    this.cancelError = '';
  }

  submitCancelSession(): void {
    if (!this.cancelSessionData.reason || !this.cancelSessionData.reason.trim()) {
      this.cancelError = 'Please specify the reason for cancelling this session.';
      this.cdr.detectChanges();
      return;
    }

    this.isCancelling = true;
    this.cancelError = '';

    const sessionToCancel = this.sessions.find(s => s.id === this.cancelSessionData.id);

    const payload: UpdateDrillSessionDto = {
      sessionDate: sessionToCancel?.sessionDate || new Date().toISOString(),
      type: sessionToCancel?.type || SessionType.Regular,
      location: sessionToCancel?.location || '',
      status: SessionStatus.Cancelled,
      notes: this.cancelSessionData.reason.trim()
    };

    this.sessionService.updateSession(this.cancelSessionData.id, payload).subscribe({
      next: () => {
        this.isCancelling = false;
        this.isCancelModalOpen = false;
        this.showToast('Session cancelled successfully.', 'success');
        this.fetchSessions();
      },
      error: (err) => {
        this.isCancelling = false;
        console.error('Failed to cancel session', err);
        this.cancelError = err.error?.message || 'Failed to cancel session.';
        this.cdr.detectChanges();
      }
    });
  }

  openCreateSession(): void {
    this.router.navigate(['/drills/sessions/new']);
  }

  viewSessionDetails(sessionId: number): void {
    this.router.navigate(['/drills/sessions', sessionId]);
  }

  closeConfirm(): void {
    this.confirmModal.isOpen = false;
  }

  executeConfirm(): void {
    if (this.confirmModal.action) {
      this.confirmModal.action();
    }
  }

  showToast(msg: string, type: 'success' | 'error' = 'success'): void {
    this.toastMessage = msg;
    this.toastType = type;
    setTimeout(() => {
      this.toastMessage = '';
    }, 3500);
  }

  // --- Edit Modal Logic ---
  openEditModal(session: any): void {
    this.editSessionData = {
      id: session.id,
      location: session.location,
      notes: session.notes,
      type: session.type,
      status: session.status,
      sessionDate: this.formatDateForInput(session.sessionDate)
    };

    this.isEditModalOpen = true;
    this.cdr.detectChanges();
  }

  closeEditModal(): void {
    this.isEditModalOpen = false;
    this.editSessionData = {};
    this.cdr.detectChanges();
  }

  saveSessionChanges(): void {
    this.isSaving = true;

    const payload = {
      location: this.editSessionData.location,
      notes: this.editSessionData.notes,
      type: Number(this.editSessionData.type),
      status: Number(this.editSessionData.status),
      sessionDate: formatToLocalISO(this.editSessionData.sessionDate)
    };

    this.sessionService.updateSession(this.editSessionData.id, payload).subscribe({
      next: () => {
        this.isSaving = false;
        this.closeEditModal();
        this.showToast('Session updated successfully.', 'success');
        this.fetchSessions();
      },
      error: (err) => {
        console.error('Failed to update session', err);
        this.showToast(err.error?.message || 'Could not update session details.', 'error');
        this.isSaving = false;
      }
    });
  }

  private formatDateForInput(dateString: string): string {
    if (!dateString) return '';
    const normalized = dateString.endsWith('Z') || dateString.includes('+') ? dateString : dateString + 'Z';
    const d = new Date(normalized);
    const year = d.getFullYear();
    const month = String(d.getMonth() + 1).padStart(2, '0');
    const day = String(d.getDate()).padStart(2, '0');
    const hours = String(d.getHours()).padStart(2, '0');
    const minutes = String(d.getMinutes()).padStart(2, '0');
    return `${year}-${month}-${day}T${hours}:${minutes}`;
  }

  // ==========================================
  // UI FORMATTING HELPERS
  // ==========================================

  getStatusClass(status: SessionStatus): string {
    switch (status) {
      case SessionStatus.Scheduled: return 'status-scheduled';
      case SessionStatus.InProgress: return 'status-inprogress';
      case SessionStatus.Completed: return 'status-completed';
      case SessionStatus.Cancelled: return 'status-cancelled';
      default: return 'status-scheduled';
    }
  }

  getBadgeClass(status: SessionStatus): string {
    switch (status) {
      case SessionStatus.Scheduled: return 'badge-cyan';
      case SessionStatus.InProgress: return 'badge-warning';
      case SessionStatus.Completed: return 'badge-success';
      case SessionStatus.Cancelled: return 'badge-coral';
      default: return 'badge-cyan';
    }
  }

  getStatusLabel(status: SessionStatus): string {
    switch (status) {
      case SessionStatus.Scheduled: return 'Scheduled';
      case SessionStatus.InProgress: return 'In Progress';
      case SessionStatus.Completed: return 'Completed';
      case SessionStatus.Cancelled: return 'Cancelled';
      default: return 'Unknown';
    }
  }

  getSessionTypeLabel(type: SessionType): string {
    const enumName = SessionType[type];
    if (!enumName) return 'Unknown';
    return enumName.replace(/([A-Z])/g, ' $1').trim();
  }
  goToWeakCategories(): void {
    this.router.navigate(['/drills/analytics/weak-categories']);
  }

  deleteSession(session: any): void {
    this.confirmModal = {
      isOpen: true,
      title: 'Delete Session',
      message: `Are you sure you want to permanently delete this cancelled session for ${session.teamName || 'this team'}? This action cannot be undone.`,
      confirmText: 'Delete Session',
      action: () => {
        this.sessionService.deleteSession(session.id).subscribe({
          next: () => {
            this.showToast('Session deleted.', 'success');
            this.closeConfirm();
            this.fetchSessions();
          },
          error: (err) => {
            this.showToast(err.error?.message || 'Could not delete session.', 'error');
            this.closeConfirm();
          }
        });
      }
    };
  }
}