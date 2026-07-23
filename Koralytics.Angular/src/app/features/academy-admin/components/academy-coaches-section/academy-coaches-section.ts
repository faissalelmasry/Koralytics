import { Component, Input, OnInit, inject, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { AcademyService } from '../../../../../core/services/academy/academy.service';
import { ToastService } from '../../../../../core/services/Toast/toast';
import { AcademyMemberResponseDto } from '../../../../../core/interfaces/academy.models';
import { CustomInputComponent } from '../../../../../shared/components/custom-input-component/custom-input-component';
import { CustomButtonComponent } from '../../../../../shared/components/custom-button/custom-button';
import { DataTable, TableColumn } from '../../../../../shared/components/data-table/data-table';
import { LoadingSpinnerComponent } from '../../../../../shared/components/loading-spinner/loading-spinner';
import { Pagination } from '../../../../../shared/components/pagination/pagination';

@Component({
  selector: 'app-academy-coaches-section',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, CustomInputComponent, CustomButtonComponent, DataTable, LoadingSpinnerComponent, Pagination],
  templateUrl: './academy-coaches-section.html',
  styleUrls: ['./academy-coaches-section.css']
})
export class AcademyCoachesSectionComponent implements OnInit, OnChanges {
  @Input() academyId!: number;
  
  private academyService = inject(AcademyService);
  private toast = inject(ToastService);
  private fb = inject(FormBuilder);
  
  coaches: AcademyMemberResponseDto[] = [];
  pendingRequests: any[] = [];
  searchResults: any[] = [];
  
  isLoading = true;
  isSearching = false;
  isSending = false;
  
  searchForm = this.fb.nonNullable.group({
    searchTerm: ['']
  });

  tableColumns: TableColumn[] = [
    { key: 'fullName', label: 'coach name', type: 'user' },
    { key: 'joinedAtFormatted', label: 'joined at', type: 'text' },
    { key: 'actions', label: 'tracking hub', type: 'action' }
  ];

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
            const dateStr = new Date(m.joinedAt).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
            return { ...m, joinedAtFormatted: dateStr };
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
          this.toast.show('Coach join request sent!', 'success');
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

  onCancelRequest(requestId: number) {
    if (confirm('Are you sure you want to cancel this request?')) {
      this.academyService.cancelCoachJoinRequest(requestId).subscribe({
        next: (res: any) => {
          if (res.isSuccess) {
            this.toast.show('Request cancelled', 'success');
            this.loadData();
          } else {
            this.toast.show(res.message || 'Error cancelling request', 'error');
          }
        }
      });
    }
  }

  isUserPending(userId: number): boolean {
    return this.pendingRequests.some(r => r.coachId === userId);
  }

  isUserMember(userId: number): boolean {
    return this.coaches.some(m => m.userId === userId);
  }

  onRemoveCoach(coachId: number) {
    if (confirm('Are you sure you want to remove this coach?')) {
      this.academyService.removeCoach(this.academyId, coachId).subscribe({
        next: (res: any) => {
          if (res.isSuccess) {
            this.toast.show('Coach removed successfully', 'success');
            this.loadData();
          } else {
            this.toast.show(res.message || 'Error removing coach', 'error');
          }
        }
      });
    }
  }

  onActionClick(event: { row: any, action: string }) {
    if (event.action === 'delete') {
      this.onRemoveCoach(event.row.userId);
    } else if (event.action === 'view') {
      // Analyze logic here
    }
  }

  onPageChange(page: number) {
    this.pageNumber = page;
    this.loadData();
  }
}
