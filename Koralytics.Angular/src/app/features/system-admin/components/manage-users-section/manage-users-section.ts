import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { SystemAdminService, UserSummaryDto } from '../../../../../core/services/system-admin/system-admin.service';
import { ToastService } from '../../../../../core/services/Toast/toast';
import { CustomButtonComponent } from '../../../../../shared/components/custom-button/custom-button';
import { LoadingSpinnerComponent } from '../../../../../shared/components/loading-spinner/loading-spinner';
import { CustomSelect, SelectOption } from '../../../../../shared/components/custom-select/custom-select';
import { Pagination } from '../../../../../shared/components/pagination/pagination';
import { ConfirmDialogComponent } from '../../../../../shared/components/confirm-dialog/confirm-dialog';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { LocalizedDatePipe } from '../../../../../shared/pipes/localized-date.pipe';
import { ScrollRevealDirective } from '../../../../../shared/directives/scroll-reveal.directive';

@Component({
  selector: 'app-manage-users-section',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    FormsModule,
    CustomButtonComponent,
    LoadingSpinnerComponent,
    CustomSelect,
    Pagination,
    ConfirmDialogComponent,
    TranslatePipe,
    LocalizedDatePipe,
    ScrollRevealDirective
  ],
  templateUrl: './manage-users-section.html',
  styleUrls: ['./manage-users-section.css']
})
export class ManageUsersSectionComponent implements OnInit {
  private systemAdminService = inject(SystemAdminService);
  private toast = inject(ToastService);
  private translate = inject(TranslateService);
  private fb = inject(FormBuilder);
  private router = inject(Router);

  viewMemberProfile(userId: number) {
    this.router.navigate(['/player/profile', userId]);
  }

  users: UserSummaryDto[] = [];
  isLoading = true;

  searchTerm = '';
  roleFilter = 'All';
  isDeletedFilter: string = 'All'; // 'All' | 'false' | 'true'

  pageNumber = 1;
  pageSize = 10;
  totalCount = 0;

  get totalPages(): number {
    return Math.ceil(this.totalCount / this.pageSize) || 1;
  }

  availableRoles = [
    'SystemAdmin',
    'AcademyAdmin',
    'Coach',
    'Player',
    'Scouter',
    'Parent'
  ];

  get roleOptions(): SelectOption[] {
    return [
      { label: 'SYSTEM_ADMIN.MANAGE_USERS.ROLE_ALL', value: 'All' },
      ...this.availableRoles.map(r => ({ label: `SYSTEM_ADMIN.MANAGE_USERS.ROLE_${r.toUpperCase()}`, value: r }))
    ];
  }

  get statusOptions(): SelectOption[] {
    return [
      { label: 'SYSTEM_ADMIN.MANAGE_USERS.STATUS_ALL', value: 'All' },
      { label: 'SYSTEM_ADMIN.MANAGE_USERS.STATUS_ACTIVE', value: 'false' },
      { label: 'SYSTEM_ADMIN.MANAGE_USERS.STATUS_DEACTIVATED', value: 'true' }
    ];
  }

  onPageChange(page: number) {
    this.pageNumber = page;
    this.loadUsers();
  }

  selectedUserForRoles: UserSummaryDto | null = null;
  selectedRoles: { [key: string]: boolean } = {};
  isUpdatingRoles = false;

  isConfirmDialogOpen = false;
  userToToggle: UserSummaryDto | null = null;
  confirmDialogTitle = '';
  confirmDialogMessage = '';
  confirmDialogActionName = '';

  ngOnInit() {
    this.loadUsers();
  }

  loadUsers() {
    this.isLoading = true;
    this.systemAdminService.getUsers({
      searchTerm: this.searchTerm,
      roleFilter: this.roleFilter === 'All' ? undefined : this.roleFilter,
      isDeletedFilter: this.isDeletedFilter === 'All' ? undefined : (this.isDeletedFilter === 'true'),
      pageNumber: this.pageNumber,
      pageSize: this.pageSize
    }).subscribe({
      next: (res) => {
        this.isLoading = false;
        if (res.isSuccess && res.data) {
          const data: any = res.data;
          this.users = data.items || data.users || data.data || (Array.isArray(data) ? data : []);
          this.totalCount = data.totalCount || this.users.length;
        }
      },
      error: () => {
        this.isLoading = false;
        this.toast.show(this.translate.instant('SYSTEM_ADMIN.MESSAGES.LOAD_USERS_FAILED'), 'error');
      }
    });
  }

  onFilterChange() {
    this.pageNumber = 1;
    this.loadUsers();
  }

  openEditRolesModal(user: UserSummaryDto) {
    this.selectedUserForRoles = user;
    this.selectedRoles = {};
    this.availableRoles.forEach(r => {
      this.selectedRoles[r] = user.roles.includes(r);
    });
  }

  closeEditRolesModal() {
    this.selectedUserForRoles = null;
    this.selectedRoles = {};
  }

  toggleRoleSelection(role: string) {
    this.selectedRoles[role] = !this.selectedRoles[role];
  }

  saveRoles() {
    if (!this.selectedUserForRoles) return;

    const newRoles = Object.keys(this.selectedRoles).filter(r => this.selectedRoles[r]);
    this.isUpdatingRoles = true;

    this.systemAdminService.updateUserRoles(this.selectedUserForRoles.id, newRoles).subscribe({
      next: (res) => {
        this.isUpdatingRoles = false;
        if (res.isSuccess && res.data) {
          this.toast.show(this.translate.instant('SYSTEM_ADMIN.MESSAGES.UPDATE_ROLES_SUCCESS'), 'success');
          this.closeEditRolesModal();
          this.loadUsers();
        } else {
          this.toast.show(res.message || this.translate.instant('SYSTEM_ADMIN.MESSAGES.UPDATE_ROLES_FAILED'), 'error');
        }
      },
      error: (err) => {
        this.isUpdatingRoles = false;
        this.toast.show(err.error?.message || this.translate.instant('SYSTEM_ADMIN.MESSAGES.UPDATE_ROLES_FAILED'), 'error');
      }
    });
  }

  toggleStatus(user: UserSummaryDto) {
    this.userToToggle = user;
    const newStatus = !user.isDeleted;
    this.confirmDialogActionName = newStatus ? 'deactivate' : 'activate';
    
    this.confirmDialogTitle = newStatus 
      ? this.translate.instant('SYSTEM_ADMIN.MANAGE_USERS.MODAL_DEACTIVATE_TITLE')
      : this.translate.instant('SYSTEM_ADMIN.MANAGE_USERS.MODAL_ACTIVATE_TITLE');
      
    this.confirmDialogMessage = newStatus
      ? this.translate.instant('SYSTEM_ADMIN.MANAGE_USERS.MODAL_DEACTIVATE_MSG', { user: user.fullName || user.email })
      : this.translate.instant('SYSTEM_ADMIN.MANAGE_USERS.MODAL_ACTIVATE_MSG', { user: user.fullName || user.email });
      
    this.isConfirmDialogOpen = true;
  }

  onConfirmStatusToggle() {
    if (!this.userToToggle) {
      this.isConfirmDialogOpen = false;
      return;
    }

    const user = this.userToToggle;
    const newStatus = !user.isDeleted;
    this.isConfirmDialogOpen = false;
    this.userToToggle = null;

    this.systemAdminService.toggleUserStatus(user.id, newStatus).subscribe({
      next: (res) => {
        if (res.isSuccess || res.statusCode === 200) {
          const successMsg = newStatus 
            ? this.translate.instant('SYSTEM_ADMIN.MANAGE_USERS.TOAST_DEACTIVATE_SUCCESS')
            : this.translate.instant('SYSTEM_ADMIN.MANAGE_USERS.TOAST_ACTIVATE_SUCCESS');
          this.toast.show(successMsg, 'success');
          this.loadUsers();
        } else {
          this.toast.show(res.message || this.translate.instant('SYSTEM_ADMIN.MANAGE_USERS.TOAST_UPDATE_ERROR'), 'error');
        }
      },
      error: (err) => {
        this.toast.show(err.error?.message || this.translate.instant('SYSTEM_ADMIN.MANAGE_USERS.TOAST_UPDATE_ERROR'), 'error');
      }
    });
  }

  nextPage() {
    if (this.pageNumber * this.pageSize < this.totalCount) {
      this.pageNumber++;
      this.loadUsers();
    }
  }

  prevPage() {
    if (this.pageNumber > 1) {
      this.pageNumber--;
      this.loadUsers();
    }
  }
}
