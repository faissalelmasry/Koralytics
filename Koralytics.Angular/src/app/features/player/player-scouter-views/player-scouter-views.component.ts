import { Component, OnInit, OnDestroy, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NavbarComponent } from '../../../../shared/components/navbar/navbar';
import { Footer } from '../../../../shared/components/footer/footer';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner';
import { ScrollRevealDirective } from '../../../../shared/directives/scroll-reveal.directive';
import { PlayerProfileService } from '../../../../core/services/player/player-profile.service';
import { TokenStorageService } from '../../../../core/services/auth/token-storage.service';
import { ProfileViewerDetailDto } from '../../../../core/models/Player/profile-views-model';

@Component({
  selector: 'app-player-scouter-views',
  standalone: true,
  imports: [
    CommonModule,
    NavbarComponent,
    Footer,
    LoadingSpinnerComponent,
    ScrollRevealDirective
  ],
  templateUrl: './player-scouter-views.component.html',
  styleUrls: ['./player-scouter-views.component.css']
})
export class PlayerScouterViewsComponent implements OnInit, OnDestroy {
  private profileService = inject(PlayerProfileService);
  private tokenStorage = inject(TokenStorageService);
  private cdr = inject(ChangeDetectorRef);

  playerId: number | null = null;
  playerName = '';
  isLoading = false;
  error = '';

  totalViewsCount = 0;
  recentViews: ProfileViewerDetailDto[] = [];
  displayTotalViews = 0;
  displayRecentCount = 0;

  private counterFrames: number[] = [];

  ngOnInit() {
    const user = this.tokenStorage.getUser();
    if (!user?.userId) {
      this.error = 'Invalid session';
      return;
    }
    this.playerId = user.userId;
    this.loadViews();
  }

  ngOnDestroy() {
    this.counterFrames.forEach(id => cancelAnimationFrame(id));
  }

  loadViews() {
    if (!this.playerId) return;

    this.isLoading = true;
    this.error = '';

    this.profileService.getProfileViews(this.playerId).subscribe({
      next: (res: any) => {
        const data = res?.data ?? res;
        this.totalViewsCount = data?.totalViewsCount ?? data?.TotalViewsCount ?? 0;
        this.recentViews = this.mapViews(data?.recentViews ?? data?.RecentViews ?? []);
        this.isLoading = false;
        this.cdr.detectChanges();
        this.startCounters();
      },
      error: () => {
        this.isLoading = false;
        this.error = 'Failed to load scouter views data.';
        this.cdr.detectChanges();
      }
    });
  }

  private startCounters() {
    this.animateCounter('total', this.totalViewsCount);
    this.animateCounter('recent', this.recentViews.length);
  }

  private animateCounter(key: 'total' | 'recent', target: number) {
    const duration = 1500;
    const startTime = performance.now();

    const frame = (currentTime: number) => {
      const elapsed = currentTime - startTime;
      const progress = Math.min(elapsed / duration, 1);
      const eased = 1 - Math.pow(1 - progress, 3);

      const display = Math.round(eased * target);

      if (key === 'total') {
        this.displayTotalViews = display;
      } else {
        this.displayRecentCount = display;
      }

      if (progress < 1) {
        this.counterFrames.push(requestAnimationFrame(frame));
      } else {
        if (key === 'total') {
          this.displayTotalViews = target;
        } else {
          this.displayRecentCount = target;
        }
      }
      this.cdr.detectChanges();
    };

    this.counterFrames.push(requestAnimationFrame(frame));
  }

  getAvatarInitials(name: string): string {
    if (!name) return 'SC';
    return name.substring(0, 2).toUpperCase();
  }

  getTimeAgo(dateStr: string): string {
    if (!dateStr) return 'Recently';
    const date = new Date(dateStr);
    const now = new Date();
    const diffMs = now.getTime() - date.getTime();
    const diffMins = Math.floor(diffMs / 60000);
    const diffHrs = Math.floor(diffMs / 3600000);
    const diffDays = Math.floor(diffMs / 86400000);

    if (diffMins < 1) return 'Just now';
    if (diffMins < 60) return `${diffMins}m ago`;
    if (diffHrs < 24) return `${diffHrs}h ago`;
    if (diffDays < 7) return `${diffDays}d ago`;
    return date.toLocaleDateString('en-US', { month: 'short', day: 'numeric' });
  }

  private mapViews(raw: any[]): ProfileViewerDetailDto[] {
    return raw.map((v: any) => ({
      scouterId: v.scouterId ?? v.ScouterId ?? 0,
      scouterName: v.scouterName ?? v.ScouterName ?? '',
      isScouterVerified: v.isScouterVerified ?? v.IsScouterVerified ?? false,
      viewedAt: v.viewedAt ?? v.ViewedAt ?? ''
    }));
  }
}
