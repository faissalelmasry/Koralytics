import { Component, OnInit, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { SystemAdminService } from '../../../../../core/services/system-admin/system-admin.service';
import { ToastService } from '../../../../../core/services/Toast/toast';
import { AcademyDetailPanelComponent } from '../academy-detail-panel/academy-detail-panel';
import { LoadingSpinnerComponent } from '../../../../../shared/components/loading-spinner/loading-spinner';
import { CustomButtonComponent } from '../../../../../shared/components/custom-button/custom-button';
import { Pagination } from '../../../../../shared/components/pagination/pagination';
import { TranslatePipe } from '@ngx-translate/core';
import { LocalizedDatePipe } from '../../../../../shared/pipes/localized-date.pipe';
import { ScrollRevealDirective } from '../../../../../shared/directives/scroll-reveal.directive';

@Component({
  selector: 'app-active-academies-section',
  standalone: true,
  imports: [CommonModule, FormsModule, AcademyDetailPanelComponent, LoadingSpinnerComponent, CustomButtonComponent, Pagination, TranslatePipe, LocalizedDatePipe, ScrollRevealDirective],
  templateUrl: './active-academies-section.html',
  styleUrls: ['./active-academies-section.css']
})
export class ActiveAcademiesSectionComponent implements OnInit {
  private systemAdminService = inject(SystemAdminService);
  private toast = inject(ToastService);
  private router = inject(Router);

  viewAcademyProfile(academyId: number) {
    this.router.navigate(['/academy/profile', academyId]);
  }

  academies: any[] = [];
  isLoading = true;

  searchQuery = '';
  pageNumber = 1;
  pageSize = 10;
  totalCount = 0;

  get totalPages(): number {
    return Math.ceil(this.totalCount / this.pageSize) || 1;
  }

  expandedAcademyId = signal<number | null>(null);

  showStatusModal = false;
  selectedAcademyForStatus: any = null;
  selectedStatus = 'Active';
  isUpdatingStatus = false;

  ngOnInit() {
    this.loadAcademies();
  }

  loadAcademies() {
    this.isLoading = true;
    this.systemAdminService.getAllAcademies({
      pageNumber: this.pageNumber,
      pageSize: this.pageSize,
      searchQuery: this.searchQuery
    }).subscribe({
      next: (res) => {
        this.isLoading = false;
        if (res.isSuccess && res.data) {
          const data: any = res.data;
          const list = Array.isArray(data) ? data : (data.academies || data.items || data.data || []);
          this.academies = list;
          this.totalCount = data.totalCount || list.length;
        }
      },
      error: () => {
        this.isLoading = false;
        this.toast.show('Failed to load active academies', 'error');
      }
    });
  }

  onSearch() {
    this.pageNumber = 1;
    this.loadAcademies();
  }

  toggleExpand(academyId: number) {
    if (this.expandedAcademyId() === academyId) {
      this.expandedAcademyId.set(null);
    } else {
      this.expandedAcademyId.set(academyId);
    }
  }

  nextPage() {
    if (this.pageNumber * this.pageSize < this.totalCount) {
      this.pageNumber++;
      this.loadAcademies();
    }
  }

  prevPage() {
    if (this.pageNumber > 1) {
      this.pageNumber--;
      this.loadAcademies();
    }
  }

  onPageChange(page: number) {
    this.pageNumber = page;
    this.loadAcademies();
  }

  getStatusClass(status: any): string {
    const s = String(status || 'Active').toLowerCase();
    if (s === 'suspended' || s === '2') return 'suspended';
    if (s === 'inactive' || s === '3') return 'inactive';
    return 'active';
  }

  getStatusLabel(status: any): string {
    const s = String(status || 'Active');
    if (s === '2' || s.toLowerCase() === 'suspended') return 'Suspended';
    if (s === '3' || s.toLowerCase() === 'inactive') return 'Inactive';
    return 'Active';
  }

  openStatusModal(academy: any) {
    this.selectedAcademyForStatus = academy;
    this.selectedStatus = this.getStatusLabel(academy.status);
    this.showStatusModal = true;
  }

  closeStatusModal() {
    this.showStatusModal = false;
    this.selectedAcademyForStatus = null;
  }

  selectStatusOption(status: string) {
    this.selectedStatus = status;
  }

  onSaveStatus() {
    if (!this.selectedAcademyForStatus || this.isUpdatingStatus) return;
    this.isUpdatingStatus = true;
    const acadId = this.selectedAcademyForStatus.id;

    this.systemAdminService.updateAcademyStatus(acadId, this.selectedStatus).subscribe({
      next: (res) => {
        this.isUpdatingStatus = false;
        if (res.isSuccess || res.statusCode === 200 || res.statusCode === 204) {
          this.toast.show(`Academy #${acadId} status updated to ${this.selectedStatus}!`, 'success');
          this.selectedAcademyForStatus.status = this.selectedStatus;
          this.closeStatusModal();
        } else {
          this.toast.show(res.message || 'Failed to update status', 'error');
        }
      },
      error: (err) => {
        this.isUpdatingStatus = false;
        this.toast.show(err.error?.message || 'Error updating status', 'error');
      }
    });
  }
}
