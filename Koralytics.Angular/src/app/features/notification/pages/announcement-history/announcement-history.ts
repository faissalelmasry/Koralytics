import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AnnouncementResponseDto } from '../../../../../core/interfaces/AnnouncementResponse';
import { LoadingSpinnerComponent } from '../../../../../shared/components/loading-spinner/loading-spinner';
import { EmptyStateComponent } from '../../../../../shared/components/empty-state/empty-state';
import { StatusChipComponent } from '../../../../../shared/components/status-chip/status-chip';
import { Pagination } from '../../../../../shared/components/pagination/pagination';

@Component({
  selector: 'app-announcement-history',
  standalone: true,
  imports: [
    CommonModule,
    StatusChipComponent,
    EmptyStateComponent,
    LoadingSpinnerComponent,
    Pagination,
  ],
  templateUrl: './announcement-history.html',
  styleUrl: './announcement-history.css',
})
export class AnnouncementHistory {
  @Input({ required: true }) announcements: AnnouncementResponseDto[] = [];
  @Input() isLoading: boolean = false;

  currentPage: number = 1;
  pageSize: number = 5;

  get paginatedAnnouncements(): AnnouncementResponseDto[] {
    const startIndex = (this.currentPage - 1) * this.pageSize;
    const endIndex = startIndex + this.pageSize;
    return this.announcements.slice(startIndex, endIndex);
  }

  onPageChange(page: number): void {
    this.currentPage = page;
  }

  // The API returns targetType as its string enum name ("All" | "Team" |
  // "AgeGroup" | "Role"), confirmed from swagger -- NOT the numeric value
  // used when *sending* an announcement. Compare against the strings here.
  getTargetTypeLabel(announcement: AnnouncementResponseDto): string {
    switch (announcement.targetType) {
      case 'Team':
        return `team #${announcement.targetId}`;
      case 'AgeGroup':
        return `age group #${announcement.targetId}`;
      case 'Role':
        return this.getRoleName(announcement.targetId);
      default:
        return 'everyone';
    }
  }

  // targetId IS numeric here -- when targetType is Role, the backend still
  // sends the role as a numeric id (4/5/6), matching what the compose form
  // now submits.
  private getRoleName(roleId: number): string {
    switch (roleId) {
      case 4: return 'players';
      case 5: return 'parents';
      case 6: return 'coaches';
      default: return 'specific role';
    }
  }

  getTargetTypeChip(targetType: string): 'success' | 'danger' | 'warning' | 'info' {
    switch (targetType) {
      case 'Team':
        return 'success';
      case 'AgeGroup':
        return 'warning';
      case 'Role':
        return 'danger';
      default:
        return 'info';
    }
  }
}