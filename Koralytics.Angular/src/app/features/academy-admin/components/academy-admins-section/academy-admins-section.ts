import { Component, Input, OnInit, inject, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { AcademyService } from '../../../../../core/services/academy/academy.service';
import { ToastService } from '../../../../../core/services/Toast/toast';
import { AcademyAdminResponseDto } from '../../../../../core/interfaces/academy.models';
import { CustomInputComponent } from '../../../../../shared/components/custom-input-component/custom-input-component';
import { CustomButtonComponent } from '../../../../../shared/components/custom-button/custom-button';
import { DataTable, TableColumn } from '../../../../../shared/components/data-table/data-table';
import { Pagination } from '../../../../../shared/components/pagination/pagination';
import { LoadingSpinnerComponent } from '../../../../../shared/components/loading-spinner/loading-spinner';

@Component({
  selector: 'app-academy-admins-section',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, CustomInputComponent, CustomButtonComponent, DataTable, Pagination, LoadingSpinnerComponent],
  templateUrl: './academy-admins-section.html',
  styleUrls: ['./academy-admins-section.css']
})
export class AcademyAdminsSectionComponent implements OnInit, OnChanges {
  @Input() academyId!: number;
  
  private academyService = inject(AcademyService);
  private toast = inject(ToastService);
  private fb = inject(FormBuilder);
  
  admins: AcademyAdminResponseDto[] = [];
  isLoading = true;
  isAdding = false;
  
  addAdminForm = this.fb.nonNullable.group({
    userId: [null as number | null, [Validators.required, Validators.min(1)]]
  });

  tableColumns: TableColumn[] = [
    { key: 'fullName', label: 'admin name', type: 'user' },
    { key: 'adminRole', label: 'role', type: 'badge' },
    { key: 'actions', label: 'tracking hub', type: 'action' }
  ];

  pageSize = 10;
  pageNumber = 1;
  totalCount = 0;

  ngOnInit() {
    this.loadAdmins();
  }

  ngOnChanges(changes: SimpleChanges) {
    if (changes['academyId'] && !changes['academyId'].isFirstChange()) {
      this.loadAdmins();
    }
  }

  loadAdmins() {
    if (!this.academyId) return;
    
    this.isLoading = true;
    this.academyService.getAcademyAdmins(this.academyId, { pageNumber: this.pageNumber, pageSize: this.pageSize }).subscribe({
      next: (res) => {
        if (res.isSuccess && res.data) {
          this.totalCount = res.data.totalCount || res.data.items.length;
          this.admins = res.data.items.map((m: any) => {
            return { 
              ...m, 
              adminRole: m.isOwner ? 'owner' : 'admin',
              hideDelete: m.isOwner,
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
  }

  onAddAdmin() {
    if (this.addAdminForm.invalid) {
      this.addAdminForm.markAllAsTouched();
      return;
    }

    const userId = this.addAdminForm.getRawValue().userId;
    if (!userId) return;

    this.isAdding = true;
    this.academyService.assignAdmin(this.academyId, userId).subscribe({
      next: (res) => {
        this.isAdding = false;
        if (res.isSuccess) {
          this.toast.show('Admin assigned successfully', 'success');
          this.addAdminForm.reset();
          this.loadAdmins();
        } else {
          this.toast.show(res.message || 'Error assigning admin', 'error');
        }
      },
      error: () => {
        this.isAdding = false;
        this.toast.show('Error assigning admin', 'error');
      }
    });
  }

  onRemoveAdmin(adminId: number) {
    if (confirm('Are you sure you want to remove this admin?')) {
      this.academyService.removeAdmin(this.academyId, adminId).subscribe({
        next: (res) => {
          if (res.isSuccess) {
            this.toast.show('Admin removed successfully', 'success');
            this.loadAdmins();
          } else {
            this.toast.show(res.message || 'Error removing admin', 'error');
          }
        },
        error: () => {
          this.toast.show('Error removing admin', 'error');
        }
      });
    }
  }

  onActionClick(event: { row: any, action: string }) {
    if (event.action === 'delete') {
      this.onRemoveAdmin(event.row.id || event.row.userId || event.row.adminUserId);
    } else if (event.action === 'view') {
      // Analyze logic here
    }
  }

  onPageChange(page: number) {
    this.pageNumber = page;
    this.loadAdmins();
  }
}
