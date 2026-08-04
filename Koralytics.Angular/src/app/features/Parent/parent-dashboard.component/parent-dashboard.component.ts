import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import {
  ParentService,
  ParentChild,
  ParentPlayerSearchResponse,
  ParentPlayerJoinRequest
} from '../../../../core/services/parent/parent.service';
import { ToastService } from '../../../../core/services/Toast/toast';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner';
import { EmptyStateComponent } from '../../../../shared/components/empty-state/empty-state';

@Component({
  selector: 'app-parent-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule, LoadingSpinnerComponent, EmptyStateComponent],
  templateUrl: './parent-dashboard.component.html',
  styleUrls: ['./parent-dashboard.component.css']
})
export class ParentDashboardComponent implements OnInit {
  children: ParentChild[] = [];
  selectedChild: ParentChild | null = null;
  pendingRequests: ParentPlayerJoinRequest[] = [];

  isLoading = true;
  errorMessage = '';

  // Search Modal state
  showSearchModal = false;
  searchQuery = '';
  searchResults: ParentPlayerSearchResponse[] = [];
  isSearching = false;
  isSendingRequest: { [key: number]: boolean } = {};

  failedImagePlayerIds: Set<number> = new Set();

  onImageError(playerId: number): void {
    this.failedImagePlayerIds.add(playerId);
  }

  hasImageError(playerId: number): boolean {
    return this.failedImagePlayerIds.has(playerId);
  }

  getInitials(name?: string): string {
    if (!name) return 'KP';
    const parts = name.trim().split(' ');
    if (parts.length >= 2) {
      return (parts[0][0] + parts[1][0]).toUpperCase();
    }
    return name.substring(0, 2).toUpperCase();
  }

  constructor(
    private parentService: ParentService,
    private router: Router,
    private cdr: ChangeDetectorRef,
    private toast: ToastService
  ) { }

  ngOnInit(): void {
    this.loadData();
  }

  loadData(): void {
    this.isLoading = true;
    this.errorMessage = '';

    this.parentService.getMyChildren().subscribe({
      next: (res: any) => {
        const data = res.data || res;
        this.children = Array.isArray(data) ? data : [];

        if (this.children.length > 0 && (!this.selectedChild || !this.children.some(c => c.playerId === this.selectedChild?.playerId))) {
          this.selectedChild = this.children[0];
        } else if (this.children.length === 0) {
          this.selectedChild = null;
        }

        this.loadPendingRequests();
      },
      error: (err) => {
        console.error('Failed to load children', err);
        this.errorMessage = err.error?.message || 'Could not fetch your linked players.';
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  loadPendingRequests(): void {
    this.parentService.getMyPendingRequests().subscribe({
      next: (res: any) => {
        const data = res.data || res;
        this.pendingRequests = Array.isArray(data) ? data : [];
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Failed to load pending requests', err);
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  onChildSelect(child: ParentChild): void {
    this.selectedChild = child;
  }

  // --- SEARCH MODAL METHODS ---
  openSearchModal(): void {
    this.showSearchModal = true;
    this.searchQuery = '';
    this.searchResults = [];
    this.onSearchPlayers();
  }

  closeSearchModal(): void {
    this.showSearchModal = false;
  }

  onSearchPlayers(): void {
    this.isSearching = true;
    this.parentService.searchPlayers(this.searchQuery).subscribe({
      next: (res: any) => {
        const data = res.data || res;
        this.searchResults = Array.isArray(data) ? data : [];
        this.isSearching = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Error searching players:', err);
        this.isSearching = false;
        this.cdr.detectChanges();
      }
    });
  }

  sendLinkRequest(player: ParentPlayerSearchResponse): void {
    if (player.isAlreadyLinked || player.hasPendingRequest) return;

    this.isSendingRequest[player.playerId] = true;
    this.parentService.sendChildRequest(player.playerId).subscribe({
      next: () => {
        this.isSendingRequest[player.playerId] = false;
        player.hasPendingRequest = true;
        this.toast.show(`Join request sent to ${player.fullName}!`, 'success');
        this.loadPendingRequests();
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.isSendingRequest[player.playerId] = false;
        const msg = err.error?.message || 'Failed to send join request.';
        this.toast.show(msg, 'error');
        this.cdr.detectChanges();
      }
    });
  }

  cancelRequest(requestId: number): void {
    if (!confirm('Are you sure you want to cancel this join request?')) return;

    this.parentService.cancelChildRequest(requestId).subscribe({
      next: () => {
        this.toast.show('Join request cancelled.', 'info');
        this.loadPendingRequests();
      },
      error: (err) => {
        const msg = err.error?.message || 'Failed to cancel join request.';
        this.toast.show(msg, 'error');
      }
    });
  }

  unlinkChild(child: ParentChild): void {
    if (!confirm(`Are you sure you want to unlink ${child.fullName} from your parent account?`)) return;

    this.parentService.unlinkChild(child.playerId).subscribe({
      next: () => {
        this.toast.show(`${child.fullName} unlinked successfully.`, 'success');
        this.loadData();
      },
      error: (err) => {
        const msg = err.error?.message || 'Failed to unlink player.';
        this.toast.show(msg, 'error');
      }
    });
  }

  // --- NAVIGATION ACTION HANDLERS ---
  navigateToProfile(): void {
    if (!this.selectedChild) return;
    this.router.navigate(['/player/profile', this.selectedChild.playerId]);
  }

  navigateToMatchTimeline(): void {
    if (!this.selectedChild) return;
    this.router.navigate(['/player/timeline', this.selectedChild.playerId]);
  }

  navigateToDrillProgression(): void {
    if (!this.selectedChild) return;
    this.router.navigate(['/player/profile', this.selectedChild.playerId]);
  }

  navigateToTeamEvents(): void {
    if (!this.selectedChild) return;
    this.router.navigate(['/player/team-events', this.selectedChild.playerId]);
  }

  navigateToSubscriptions(): void {
    this.router.navigate(['/parent/subscriptions']);
  }
}