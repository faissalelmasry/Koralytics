import { Component, OnInit, OnDestroy, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { Subject, Subscription } from 'rxjs';
import { debounceTime, distinctUntilChanged, finalize } from 'rxjs/operators';
import { DrillSessionService } from '../../../../core/services/drill/drill-session.service';
import {
  DrillSessionDto,
  SessionFilterDto,
} from '../../../../core/interfaces/drill-session.model';
import { Router } from '@angular/router';
import { SessionStatus, SessionType } from '../../../../core/enums/koralytics.enums';
import { Pagination } from '../../../../shared/components/pagination/pagination';

@Component({
  selector: 'app-drill-session-list',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, Pagination],
  templateUrl: './drill-session-list.component.html',
  styleUrls: ['./drill-session-list.component.css']
})
export class DrillSessionListComponent implements OnInit, OnDestroy {
  // --- Data Arrays ---
  sessions: DrillSessionDto[] = [];
  availableTeams: { id: number, name: string }[] = []; // 🟢 ADDED: Dynamic teams array

  // --- UI States ---
  isLoading = false;
  errorMessage = '';

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

  // --- RxJS Search ---
  private searchSubject = new Subject<string>();
  private searchSubscription!: Subscription;

  constructor(
    private sessionService: DrillSessionService,
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
        this.sessions = items;
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

  // 🟢 ADDED: Safely accumulates teams so they don't disappear when you apply a filter
  private populateTeamsDropdown(): void {
    this.sessions.forEach(session => {
      if (session.teamId && session.teamName) {
        // Only add the team if it isn't already in the dropdown
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

  // ==========================================
  // EVENT HANDLERS & FILTERS
  // ==========================================

  onMonthChange(event: Event): void {
    const target = event.target as HTMLInputElement;
    const val = target.value; // Format: "YYYY-MM"
    
    if (val) {
      const [year, month] = val.split('-');
      // Start of month
      this.filter.fromDate = `${year}-${month}-01T00:00:00Z`;
      
      // End of month (last day of the given month)
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

  // ==========================================
  // NAVIGATION & ACTIONS
  // ==========================================

  openCreateSession(): void {
    this.router.navigate(['/drills/sessions/new']);
  }

  viewSessionDetails(sessionId: number): void {
    this.router.navigate(['/drills/sessions', sessionId]);
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
}