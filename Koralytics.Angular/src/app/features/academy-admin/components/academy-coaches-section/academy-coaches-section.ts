import { Component, Input, OnInit, inject, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { AcademyService } from '../../../../../core/services/academy/academy.service';
import { ToastService } from '../../../../../core/services/Toast/toast';
import { AcademyMemberResponseDto } from '../../../../../core/interfaces/academy.models';
import { AuthService } from '../../../../../core/services/auth/auth.service';
import { CustomButtonComponent } from '../../../../../shared/components/custom-button/custom-button';
import { DataTable, TableColumn } from '../../../../../shared/components/data-table/data-table';
import { LoadingSpinnerComponent } from '../../../../../shared/components/loading-spinner/loading-spinner';
import { Pagination } from '../../../../../shared/components/pagination/pagination';
import { ConfirmDialogComponent } from '../../../../../shared/components/confirm-dialog/confirm-dialog';
import { Router } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { LocalizedDatePipe } from '../../../../../shared/pipes/localized-date.pipe';

@Component({
  selector: 'app-academy-coaches-section',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, CustomButtonComponent, DataTable, LoadingSpinnerComponent, Pagination, ConfirmDialogComponent, TranslatePipe, LocalizedDatePipe],
  templateUrl: './academy-coaches-section.html',
  styleUrls: ['./academy-coaches-section.css']
})
export class AcademyCoachesSectionComponent implements OnInit, OnChanges {
  @Input() academyId!: number;

  private academyService = inject(AcademyService);
  private toast = inject(ToastService);
  private fb = inject(FormBuilder);
  private router = inject(Router);
  private authService = inject(AuthService);
  private translate = inject(TranslateService);

  coaches: AcademyMemberResponseDto[] = [];
  pendingRequests: any[] = [];
  searchResults: any[] = [];

  isLoading = true;
  isSearching = false;
  isSending = false;

  searchForm = this.fb.nonNullable.group({
    searchTerm: ['']
  });

  get tableColumns(): TableColumn[] {
    return [
      { key: 'fullName', label: 'ACADEMY_ADMIN.COACHES_SECTION.COL_COACH_NAME', type: 'user' },
      { key: 'joinedAt', label: 'ACADEMY_ADMIN.COACHES_SECTION.COL_JOINED_AT', type: 'date' },
      { key: 'actions', label: 'ACADEMY_ADMIN.COACHES_SECTION.COL_TRACKING_HUB', type: 'action' }
    ];
  }

  pageSize = 10;
  pageNumber = 1;
  totalCount = 0;

  ngOnInit() {
    this.loadData();
  }

  ngOnChanges(changes: SimpleChanges) {
    if (changes['academyId'] && !changes['academyId'].isFirstChange()) {
      this.loadData();
    }
  }

  loadData() {
    if (!this.academyId) return;
    this.isLoading = true;

    // Load members and filter coaches
    this.academyService.getAcademyMembers(this.academyId, { pageNumber: this.pageNumber, pageSize: 100 }).subscribe({
      next: (res) => {
        if (res.isSuccess && res.data) {
          const filtered = res.data.items.filter(m => m.role === 'Coach');
          this.totalCount = filtered.length; // Fake pagination for now

          this.coaches = filtered.slice((this.pageNumber - 1) * this.pageSize, this.pageNumber * this.pageSize).map(m => {
            
            return { 
              ...m, 
              joinedAt: m.joinedAt,
              hideDelete: m.userId === this.authService.getCurrentUserSync()?.userId
            };
          });
        }
        this.isLoading = false;
      },
      error: () => this.isLoading = false
    });

    // Load pending coach requests
    this.academyService.getPendingCoachRequests(this.academyId).subscribe({
      next: (res) => {
        if (res.isSuccess && res.data) {
          this.pendingRequests = res.data;
        }
      }
    });
  }

  onSearch() {
    const term = this.searchForm.getRawValue().searchTerm;
    if (term === undefined || term === null) return;

    this.isSearching = true;
    this.academyService.searchCoaches(this.academyId, term).subscribe({
      next: (res) => {
        this.isSearching = false;
        if (res.isSuccess && res.data) {
          this.searchResults = res.data;
        }
      },
      error: () => {
        this.isSearching = false;
      }
    });
  }

  onSendRequest(coachId: number) {
    this.isSending = true;
    this.academyService.sendCoachJoinRequest(this.academyId, coachId).subscribe({
      next: (res) => {
        this.isSending = false;
        if (res.isSuccess) {
          this.toast.show(this.translate.instant('ACADEMY_ADMIN.COACHES_SECTION.REQUEST_SENT_SUCCESS') || 'Coach join request sent!', 'success');
          this.loadData(); // Reload pending requests
          this.searchResults = [];
          this.searchForm.reset();
        } else {
          this.toast.show(res.message || 'Error sending request', 'error');
        }
      },
      error: (err) => {
        this.isSending = false;
        this.toast.show(err.error?.detail || err.error?.message || 'Error sending request', 'error');
      }
    });
  }

  isConfirmDialogOpen = false;
  confirmDialogTitle = '';
  confirmDialogMessage = '';
  confirmActionType: 'cancelRequest' | 'removeCoach' | null = null;
  targetIdForConfirm: number | null = null;

  onCancelRequest(requestId: number) {
    this.confirmActionType = 'cancelRequest';
    this.targetIdForConfirm = requestId;
    this.confirmDialogTitle = this.translate.instant('ACADEMY_ADMIN.COACHES_SECTION.CANCEL_REQUEST_TITLE') || 'Cancel Join Request';
    this.confirmDialogMessage = this.translate.instant('ACADEMY_ADMIN.COACHES_SECTION.CANCEL_REQUEST_MSG') || 'Are you sure you want to cancel this pending coach join request?';
    this.isConfirmDialogOpen = true;
  }

  isUserPending(userId: number): boolean {
    return this.pendingRequests.some(r => r.coachId === userId);
  }

  isUserMember(userId: number): boolean {
    return this.coaches.some(m => m.userId === userId);
  }

  onRemoveCoach(coachId: number) {
    if (coachId === this.authService.getCurrentUserSync()?.userId) {
      this.toast.show('You cannot remove yourself from the academy.', 'error');
      return;
    }
    this.confirmActionType = 'removeCoach';
    this.targetIdForConfirm = coachId;
    this.confirmDialogTitle = this.translate.instant('ACADEMY_ADMIN.COACHES_SECTION.REMOVE_COACH_TITLE') || 'Remove Coach';
    this.confirmDialogMessage = this.translate.instant('ACADEMY_ADMIN.COACHES_SECTION.REMOVE_COACH_MSG') || 'Are you sure you want to remove this coach from the academy?';
    this.isConfirmDialogOpen = true;
  }

  onConfirmDialogExecute() {
    if (!this.confirmActionType || !this.targetIdForConfirm) {
      this.isConfirmDialogOpen = false;
      return;
    }

    const action = this.confirmActionType;
    const targetId = this.targetIdForConfirm;
    this.isConfirmDialogOpen = false;
    this.confirmActionType = null;
    this.targetIdForConfirm = null;

    if (action === 'cancelRequest') {
      this.academyService.cancelCoachJoinRequest(targetId).subscribe({
        next: (res: any) => {
          if (res.isSuccess) {
            this.toast.show(this.translate.instant('ACADEMY_ADMIN.COACHES_SECTION.CANCEL_SUCCESS') || 'Request cancelled', 'success');
            this.loadData();
          } else {
            this.toast.show(res.message || 'Error cancelling request', 'error');
          }
        }
      });
    } else if (action === 'removeCoach') {
      this.academyService.removeCoach(this.academyId, targetId).subscribe({
        next: (res: any) => {
          if (res.isSuccess) {
            this.toast.show(this.translate.instant('ACADEMY_ADMIN.COACHES_SECTION.REMOVE_SUCCESS') || 'Coach removed successfully', 'success');
            this.loadData();
          } else {
            this.toast.show(res.message || 'Error removing coach', 'error');
          }
        }
      });
    }
  }

  isAnalyticsModalOpen = false;
  isLoadingAnalytics = false;
  coachesAnalytics: any[] = [];
  topCoach: any = null;
  avgAcademyImprovement = 0;

  openCoachesAnalyticsModal() {
    this.isAnalyticsModalOpen = true;
    this.isLoadingAnalytics = true;
    this.academyService.getCoachPerformance(this.academyId).subscribe({
      next: (res) => {
        this.isLoadingAnalytics = false;
        if (res.isSuccess && res.data) {
          this.coachesAnalytics = Array.isArray(res.data) ? res.data : [];
          if (this.coachesAnalytics.length > 0) {
            this.coachesAnalytics.sort((a, b) => (a.rank || 999) - (b.rank || 999));
            this.topCoach = this.coachesAnalytics[0];
            const totalImp = this.coachesAnalytics.reduce((sum, c) => sum + (c.averagePlayerImprovementRate || 0), 0);
            this.avgAcademyImprovement = parseFloat((totalImp / this.coachesAnalytics.length).toFixed(1));
          } else {
            this.topCoach = null;
            this.avgAcademyImprovement = 0;
          }
        }
      },
      error: (err) => {
        this.isLoadingAnalytics = false;
        this.toast.show(err.error?.message || 'Failed to load coaches analytics.', 'error');
      }
    });
  }

  closeCoachesAnalyticsModal() {
    this.isAnalyticsModalOpen = false;
  }

  onActionClick(event: { row: any, action: string }) {
    if (event.action === 'delete') {
      this.onRemoveCoach(event.row.userId);
    } else if (event.action === 'view' || event.action === 'viewProfile') {
      const targetId = event.row.coachId || event.row.userId || event.row.id;
      if (targetId) {
        this.router.navigate(['/Coach/profile', targetId]);
      }
    }
  }

  viewMemberProfile(userId: number | undefined) {
    if (userId) {
      this.router.navigate(['/Coach/profile', userId]);
    }
  }

  onPageChange(page: number) {
    this.pageNumber = page;
    this.loadData();
  }
}
