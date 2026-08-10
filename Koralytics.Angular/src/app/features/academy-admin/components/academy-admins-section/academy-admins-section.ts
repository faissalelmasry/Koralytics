import { Component, Input, OnInit, inject, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { AcademyService } from '../../../../../core/services/academy/academy.service';
import { ToastService } from '../../../../../core/services/Toast/toast';
import { AcademyAdminResponseDto } from '../../../../../core/interfaces/academy.models';
import { CustomButtonComponent } from '../../../../../shared/components/custom-button/custom-button';
import { DataTable, TableColumn } from '../../../../../shared/components/data-table/data-table';
import { Pagination } from '../../../../../shared/components/pagination/pagination';
import { LoadingSpinnerComponent } from '../../../../../shared/components/loading-spinner/loading-spinner';
import { ConfirmDialogComponent } from '../../../../../shared/components/confirm-dialog/confirm-dialog';
import { Router } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { LocalizedDatePipe } from '../../../../../shared/pipes/localized-date.pipe';

@Component({
  selector: 'app-academy-admins-section',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, CustomButtonComponent, DataTable, Pagination, LoadingSpinnerComponent, ConfirmDialogComponent, TranslatePipe, LocalizedDatePipe],
  templateUrl: './academy-admins-section.html',
  styleUrls: ['./academy-admins-section.css']
})
export class AcademyAdminsSectionComponent implements OnInit, OnChanges {
  @Input() academyId!: number;
  @Input() isOwner: boolean = false; // from dashboard

  private academyService = inject(AcademyService);
  private toast = inject(ToastService);
  private fb = inject(FormBuilder);
  private router = inject(Router);
  private translate = inject(TranslateService);

  admins: AcademyAdminResponseDto[] = [];
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
      { key: 'fullName', label: 'ACADEMY_ADMIN.ADMINS_SECTION.COL_ADMIN_NAME', type: 'user' },
      { key: 'adminRole', label: 'ACADEMY_ADMIN.ADMINS_SECTION.COL_ROLE', type: 'badge', translate: true },
      { key: 'actions', label: 'ACADEMY_ADMIN.ADMINS_SECTION.COL_TRACKING_HUB', type: 'action' }
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

    this.academyService.getAcademyAdmins(this.academyId, { pageNumber: this.pageNumber, pageSize: this.pageSize }).subscribe({
      next: (res) => {
        if (res.isSuccess && res.data) {
          this.totalCount = res.data.totalCount || res.data.items.length;
          this.admins = res.data.items.map((m: any) => {
            return {
              ...m,
              adminRole: m.isOwner ? 'ACADEMY_ADMIN.ADMINS_SECTION.OWNER' : 'ACADEMY_ADMIN.ADMINS_SECTION.ADMIN',
              adminRoleRaw: m.isOwner ? 'owner' : 'admin',
              hideDelete: m.isOwner || !this.isOwner, // Only owner can remove others
              hideAnalyze: m.isOwner
            };
          });
        }
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
      }
    });

    if (this.isOwner) {
      this.academyService.getPendingAdminRequests(this.academyId).subscribe({
        next: (res) => {
          if (res.isSuccess && res.data) {
            this.pendingRequests = res.data;
          }
        }
      });
    }
  }

  onSearch() {
    if (!this.isOwner) return;

    const term = this.searchForm.getRawValue().searchTerm;
    if (term === undefined || term === null) return;

    this.isSearching = true;
    this.academyService.searchAdmins(this.academyId, term).subscribe({
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

  onSendRequest(adminId: number) {
    if (!this.isOwner) return;

    this.isSending = true;
    this.academyService.sendAdminJoinRequest(this.academyId, adminId).subscribe({
      next: (res) => {
        this.isSending = false;
        if (res.isSuccess) {
          this.toast.show(this.translate.instant('ACADEMY_ADMIN.ADMINS_SECTION.REQUEST_SENT_SUCCESS') || 'Admin join request sent!', 'success');
          this.loadData(); // Reload pending requests
          this.searchResults = [];
          this.searchForm.reset();
        } else {
          this.toast.show(res.message || this.translate.instant('ACADEMY_ADMIN.MESSAGES.SEND_REQUEST_ERROR'), 'error');
        }
      },
      error: (err) => {
        this.isSending = false;
        this.toast.show(err.error?.detail || err.error?.message || this.translate.instant('ACADEMY_ADMIN.MESSAGES.SEND_REQUEST_ERROR'), 'error');
      }
    });
  }

  isConfirmDialogOpen = false;
  confirmDialogTitle = '';
  confirmDialogMessage = '';
  confirmActionType: 'cancelRequest' | 'removeAdmin' | null = null;
  targetIdForConfirm: number | null = null;

  onCancelRequest(requestId: number) {
    if (!this.isOwner) return;
    this.confirmActionType = 'cancelRequest';
    this.targetIdForConfirm = requestId;
    this.confirmDialogTitle = this.translate.instant('ACADEMY_ADMIN.ADMINS_SECTION.CANCEL_REQUEST_TITLE') || 'Cancel Join Request';
    this.confirmDialogMessage = this.translate.instant('ACADEMY_ADMIN.ADMINS_SECTION.CANCEL_REQUEST_MSG') || 'Are you sure you want to cancel this pending join request?';
    this.isConfirmDialogOpen = true;
  }

  isUserPending(userId: number): boolean {
    return this.pendingRequests.some(r => r.adminId === userId);
  }

  isUserMember(userId: number): boolean {
    return this.admins.some(m => m.userId === userId);
  }

  onRemoveAdmin(adminId: number) {
    if (!this.isOwner) return;
    this.confirmActionType = 'removeAdmin';
    this.targetIdForConfirm = adminId;
    this.confirmDialogTitle = this.translate.instant('ACADEMY_ADMIN.ADMINS_SECTION.REMOVE_ADMIN_TITLE') || 'Remove Academy Admin';
    this.confirmDialogMessage = this.translate.instant('ACADEMY_ADMIN.ADMINS_SECTION.REMOVE_ADMIN_MSG') || 'Are you sure you want to revoke admin privileges from this account?';
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
      this.academyService.cancelAdminJoinRequest(targetId).subscribe({
        next: (res: any) => {
          if (res.isSuccess) {
            this.toast.show(this.translate.instant('ACADEMY_ADMIN.ADMINS_SECTION.CANCEL_SUCCESS') || 'Request cancelled', 'success');
            this.loadData();
          } else {
            this.toast.show(res.message || this.translate.instant('ACADEMY_ADMIN.MESSAGES.CANCEL_REQUEST_ERROR'), 'error');
          }
        }
      });
    } else if (action === 'removeAdmin') {
      this.academyService.removeAdmin(this.academyId, targetId).subscribe({
        next: (res) => {
          if (res.isSuccess) {
            this.toast.show(this.translate.instant('ACADEMY_ADMIN.ADMINS_SECTION.REMOVE_SUCCESS') || 'Admin removed successfully', 'success');
            this.loadData();
          } else {
            this.toast.show(res.message || this.translate.instant('ACADEMY_ADMIN.MESSAGES.REMOVE_ADMIN_ERROR'), 'error');
          }
        },
        error: () => {
          this.toast.show(this.translate.instant('ACADEMY_ADMIN.MESSAGES.REMOVE_ADMIN_ERROR'), 'error');
        }
      });
    }
  }

  onActionClick(event: { row: any, action: string }) {
    if (event.action === 'delete') {
      this.onRemoveAdmin(event.row.id || event.row.userId || event.row.adminUserId);
    } else if (event.action === 'view' || event.action === 'viewProfile') {
      const targetId = event.row.adminId || event.row.userId || event.row.id;
      if (targetId) {
        this.router.navigate(['/admin/profile', targetId]);
      }
    }
  }

  viewMemberProfile(userId: number | undefined) {
    if (userId) {
      this.router.navigate(['/admin/profile', userId]);
    }
  }

  onPageChange(page: number) {
    this.pageNumber = page;
    this.loadData();
  }
}
