import { Component, OnInit, OnDestroy, inject, ChangeDetectorRef, ChangeDetectionStrategy, NgZone, DestroyRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NavbarComponent } from '../../../../shared/components/navbar/navbar';
import { Footer } from '../../../../shared/components/footer/footer';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner';
import { ScrollRevealDirective } from '../../../../shared/directives/scroll-reveal.directive';
import { PlayerProfileService } from '../../../../core/services/player/player-profile.service';
import { TokenStorageService } from '../../../../core/services/auth/token-storage.service';
import { ProfileViewerDetailDto } from '../../../../core/models/Player/profile-views-model';
import { Router } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';

export interface DisplayProfileViewerDetailDto extends ProfileViewerDetailDto {
  avatarInitials: string;
  timeAgo: string;
}

@Component({
  selector: 'app-player-scouter-views',
  standalone: true,
  imports: [
    CommonModule,
    NavbarComponent,
    Footer,
    LoadingSpinnerComponent,
    ScrollRevealDirective,
    TranslatePipe
  ],
  templateUrl: './player-scouter-views.component.html',
  styleUrls: ['./player-scouter-views.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class PlayerScouterViewsComponent implements OnInit, OnDestroy {
  private profileService = inject(PlayerProfileService);
  private tokenStorage = inject(TokenStorageService);
  private cdr = inject(ChangeDetectorRef);
  private router = inject(Router);
  private ngZone = inject(NgZone);
  private destroyRef = inject(DestroyRef);

  playerId: number | null = null;
  playerName = '';
  isLoading = false;
  error = '';

  totalViewsCount = 0;
  recentViews: DisplayProfileViewerDetailDto[] = [];
  displayTotalViews = 0;
  displayRecentCount = 0;

  private counterFrames: number[] = [];

  ngOnInit() {
    const user = this.tokenStorage.getUser();
    if (!user?.userId) {
      this.error = 'Invalid session';
      this.cdr.markForCheck();
      return;
    }
    this.playerId = user.userId;
    this.loadViews();
  }

  ngOnDestroy() {
    this.counterFrames.forEach(id => cancelAnimationFrame(id));
    this.counterFrames = [];
  }

  loadViews() {
    if (!this.playerId) return;

    this.isLoading = true;
    this.error = '';
    this.cdr.markForCheck();

    this.profileService.getProfileViews(this.playerId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (res: any) => {
          const data = res?.data ?? res;
          this.totalViewsCount = data?.totalViewsCount ?? data?.TotalViewsCount ?? 0;
          this.recentViews = this.mapViews(data?.recentViews ?? data?.RecentViews ?? []);
          this.isLoading = false;
          this.cdr.markForCheck();
          this.startCounters();
        },
        error: () => {
          this.isLoading = false;
          this.error = 'Failed to load scouter views data.';
          this.cdr.markForCheck();
        }
      });
  }

  private startCounters() {
    this.counterFrames.forEach(id => cancelAnimationFrame(id));
    this.counterFrames = [];

    this.animateCounter('total', this.totalViewsCount);
    this.animateCounter('recent', this.recentViews.length);
  }

  private animateCounter(key: 'total' | 'recent', target: number) {
    this.ngZone.runOutsideAngular(() => {
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
          const frameId = requestAnimationFrame(frame);
          this.counterFrames.push(frameId);
        } else {
          if (key === 'total') {
            this.displayTotalViews = target;
          } else {
            this.displayRecentCount = target;
          }
        }
        this.cdr.markForCheck();
      };

      const frameId = requestAnimationFrame(frame);
      this.counterFrames.push(frameId);
    });
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

  private mapViews(raw: any[]): DisplayProfileViewerDetailDto[] {
    return raw.map((v: any) => {
      const scouterName = v.scouterName ?? v.ScouterName ?? '';
      const viewedAt = v.viewedAt ?? v.ViewedAt ?? '';
      return {
        scouterId: v.scouterId ?? v.ScouterId ?? 0,
        scouterName,
        isScouterVerified: v.isScouterVerified ?? v.IsScouterVerified ?? false,
        viewedAt,
        avatarInitials: this.getAvatarInitials(scouterName),
        timeAgo: this.getTimeAgo(viewedAt),
      };
    });
  }

  goToScouterProfile(scouterId: number): void {
    if (!scouterId) return;
    this.router.navigate(['/scouter/dashboard', scouterId]);
  }
}
