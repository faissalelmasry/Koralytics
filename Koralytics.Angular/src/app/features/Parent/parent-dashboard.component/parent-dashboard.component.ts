import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ParentService, ParentChild } from '../../../../core/services/parent/parent.service';
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

  isLoading = true;
  errorMessage = '';

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
    private cdr: ChangeDetectorRef
  ) { }

  ngOnInit(): void {
    this.loadChildren();
  }

  loadChildren(): void {
    this.isLoading = true;
    this.errorMessage = '';

    this.parentService.getMyChildren().subscribe({
      next: (res: any) => {
        // Unwraps ApiBaseController response envelope if present
        const data = res.data || res;
        this.children = Array.isArray(data) ? data : [];

        if (this.children.length > 0) {
          this.selectedChild = this.children[0];
        }

        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Failed to load children', err);
        this.errorMessage = err.error?.message || 'Could not fetch your linked players.';
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  onChildSelect(child: ParentChild): void {
    this.selectedChild = child;
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
    this.router.navigate(['/drills/players', this.selectedChild.playerId, 'progression']);
  }

  navigateToSubscriptions(): void {
    this.router.navigate(['/parent/subscriptions']);
  }
}