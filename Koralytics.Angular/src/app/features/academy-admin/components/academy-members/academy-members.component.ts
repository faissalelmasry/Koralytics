import { Component, Input, OnInit, inject, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { AcademyService } from '../../../../../core/services/academy/academy.service';
import { ToastService } from '../../../../../core/services/Toast/toast';
import { AcademyMemberResponseDto, PagedResponseDto } from '../../../../../core/interfaces/academy.models';
import { CustomInputComponent } from '../../../../../shared/components/custom-input-component/custom-input-component';
import { CustomButtonComponent } from '../../../../../shared/components/custom-button/custom-button';
import { DataTable, TableColumn } from '../../../../../shared/components/data-table/data-table';
import { Pagination } from '../../../../../shared/components/pagination/pagination';
import { LoadingSpinnerComponent } from '../../../../../shared/components/loading-spinner/loading-spinner';

@Component({
  selector: 'app-academy-members',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, CustomInputComponent, CustomButtonComponent, DataTable, Pagination, LoadingSpinnerComponent],
  templateUrl: './academy-members.component.html',
  styleUrls: ['./academy-members.component.css']
})
export class AcademyMembersComponent implements OnInit, OnChanges {
  Math = Math;
  @Input() academyId!: number;
  
  private academyService = inject(AcademyService);
  private toast = inject(ToastService);
  private fb = inject(FormBuilder);

  membersData: PagedResponseDto<AcademyMemberResponseDto> | null = null;
  pendingRequests: any[] = [];
  searchResults: any[] = [];

  isLoading = true;
  isSearching = false;
  isSending = false;
  pageNumber = 1;
  pageSize = 10;
  
  searchForm = this.fb.nonNullable.group({
    searchTerm: ['']
  });

  tableColumns: TableColumn[] = [
    { key: 'fullName', label: 'player name', type: 'user' },
    { key: 'position', label: 'position', type: 'text' },
    { key: 'squadStatus', label: 'squad status', type: 'badge' },
    { key: 'joinedAtFormatted', label: 'joined at', type: 'text' },
    { key: 'actions', label: 'tracking hub', type: 'action' }
  ];

  ngOnInit(): void {
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
    this.academyService.getAcademyMembers(this.academyId, { pageNumber: this.pageNumber, pageSize: this.pageSize }).subscribe({
      next: (res) => {
        if (res.isSuccess && res.data) {
          // Map SquadStatus for CSS, position capitalization, and joinedAt
          const itemsWithMappedStatus = res.data.items.filter(m => m.role !== 'Coach').map(m => {
            let status = m.squadStatus?.toLowerCase() || 'pending';
            if (status === 'available') status = 'active';
            if (status === 'resting' || status === 'suspended') status = 'pending';
            
            let pos = m.position || 'Unknown';
            pos = pos.split(' ').map(word => word.charAt(0).toUpperCase() + word.slice(1).toLowerCase()).join(' ');

            const dateStr = new Date(m.joinedAt).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });

            return { ...m, squadStatus: status, position: pos, joinedAtFormatted: dateStr };
          });

          this.membersData = {
            ...res.data,
            items: itemsWithMappedStatus
          };
        }
        this.isLoading = false;
      },
      error: () => this.isLoading = false
    });

    this.academyService.getPendingPlayerRequests(this.academyId).subscribe({
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
    this.academyService.searchPlayers(this.academyId, term).subscribe({
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

  onSendRequest(playerId: number) {
    this.isSending = true;
    this.academyService.sendPlayerJoinRequest(this.academyId, playerId).subscribe({
      next: (res) => {
        this.isSending = false;
        if (res.isSuccess) {
          this.toast.show('Player join request sent!', 'success');
          this.loadData();
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
      this.academyService.cancelPlayerJoinRequest(requestId).subscribe({
        next: (res) => {
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
    return this.pendingRequests.some(r => r.playerId === userId);
  }

  isUserMember(userId: number): boolean {
    return this.membersData?.items.some(m => m.userId === userId) || false;
  }

  onRemoveMember(playerId: number) {
    if (confirm('Are you sure you want to remove this member?')) {
      this.academyService.removePlayer(this.academyId, playerId).subscribe({
        next: (res) => {
          if (res.isSuccess) {
            this.toast.show('Member removed successfully', 'success');
            this.loadData();
          } else {
            this.toast.show(res.message || 'Error removing member', 'error');
          }
        }
      });
    }
  }

  onActionClick(event: { row: any, action: string }) {
    if (event.action === 'delete') {
      this.onRemoveMember(event.row.userId);
    } else if (event.action === 'view') {
      // Analyze logic here
    }
  }

  onPageChange(page: number) {
    this.pageNumber = page;
    this.loadData();
  }
}
