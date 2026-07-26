import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, FormsModule } from '@angular/forms';
import { SystemAdminService, UserSummaryDto } from '../../../../../core/services/system-admin/system-admin.service';
import { ToastService } from '../../../../../core/services/Toast/toast';
import { CustomButtonComponent } from '../../../../../shared/components/custom-button/custom-button';
import { LoadingSpinnerComponent } from '../../../../../shared/components/loading-spinner/loading-spinner';
import { CustomSelect, SelectOption } from '../../../../../shared/components/custom-select/custom-select';
import { Pagination } from '../../../../../shared/components/pagination/pagination';

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
    Pagination
  ],
  templateUrl: './manage-users-section.html',
  styleUrls: ['./manage-users-section.css']
})
export class ManageUsersSectionComponent implements OnInit {
  private systemAdminService = inject(SystemAdminService);
  private toast = inject(ToastService);
  private fb = inject(FormBuilder);

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
      { label: 'All Roles', value: 'All' },
      ...this.availableRoles.map(r => ({ label: r, value: r }))
    ];
  }

  get statusOptions(): SelectOption[] {
    return [
      { label: 'All Status', value: 'All' },
      { label: 'Active Only', value: 'false' },
      { label: 'Deactivated Only', value: 'true' }
    ];
  }

  onPageChange(page: number) {
    this.pageNumber = page;
    this.loadUsers();
  }

  selectedUserForRoles: UserSummaryDto | null = null;
  selectedRoles: { [key: string]: boolean } = {};
  isUpdatingRoles = false;

  ngOnInit() {
    this.loadUsers();
  }

  loadUsers() {
    this.isLoading = true;
    let isDeletedParam: boolean | undefined = undefined;
    if (this.isDeletedFilter === 'false') isDeletedParam = false;
    if (this.isDeletedFilter === 'true') isDeletedParam = true;

    this.systemAdminService.getUsers({
      pageNumber: this.pageNumber,
      pageSize: this.pageSize,
      searchTerm: this.searchTerm,
      roleFilter: this.roleFilter === 'All' ? undefined : this.roleFilter,
      isDeletedFilter: isDeletedParam
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
        this.toast.show('Failed to load users', 'error');
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
          this.toast.show('User roles updated successfully', 'success');
          this.closeEditRolesModal();
          this.loadUsers();
        } else {
          this.toast.show(res.message || 'Failed to update roles', 'error');
        }
      },
      error: (err) => {
        this.isUpdatingRoles = false;
        this.toast.show(err.error?.message || 'Failed to update roles', 'error');
      }
    });
  }

  toggleStatus(user: UserSummaryDto) {
    const newStatus = !user.isDeleted;
    const actionName = newStatus ? 'deactivate' : 'activate';

    if (!confirm(`Are you sure you want to ${actionName} account for ${user.fullName || user.email}?`)) {
      return;
    }

    this.systemAdminService.toggleUserStatus(user.id, newStatus).subscribe({
      next: (res) => {
        if (res.isSuccess || res.statusCode === 200) {
          this.toast.show(`User ${newStatus ? 'deactivated' : 'activated'} successfully`, 'success');
          this.loadUsers();
        } else {
          this.toast.show(res.message || 'Error updating status', 'error');
        }
      },
      error: (err) => {
        this.toast.show(err.error?.message || 'Error updating status', 'error');
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
