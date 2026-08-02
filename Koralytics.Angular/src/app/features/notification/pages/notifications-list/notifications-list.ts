import { Component, DestroyRef, computed, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpErrorResponse } from '@angular/common/http';
import { forkJoin, of } from 'rxjs';
import { catchError, map } from 'rxjs/operators';

// Core Services
import { SignalRService } from '../../../../../core/services/SignalR/signalrservice';
import { CachedNotification } from '../../../../../core/interfaces/CachedNotification';
import { ToastService } from '../../../../../core/services/Toast/toast';
import { NotificationService } from '../../../../../core/services/SignalR/notificationservice';
import { extractErrorMessage } from '../../../../../core/utils/http-error.util';
import { TokenStorageService } from '../../../../../core/services/auth/token-storage.service';

// Shared System Components & Directives
import { NavbarComponent } from '../../../../../shared/components/navbar/navbar';
import { Footer } from '../../../../../shared/components/footer/footer';
import { Pagination } from '../../../../../shared/components/pagination/pagination';
import { CustomButtonComponent } from '../../../../../shared/components/custom-button/custom-button';
import { LoadingSpinnerComponent } from '../../../../../shared/components/loading-spinner/loading-spinner';
import { ScrollRevealDirective } from '../../../../../shared/directives/scroll-reveal.directive';

interface NotificationVisual {
  icon: 'megaphone' | 'trophy' | 'bell' | 'clock' | 'eye';
  color: string;
}

// Mirrors the type -> category mapping already established in
// SignalRService.resolveToastType(), just extended with an icon + accent
// color per type instead of just a toast severity, so the list itself is
// scannable at a glance instead of every row looking identical.
const TYPE_VISUALS: Record<string, NotificationVisual> = {
  AcademyAnnouncement: { icon: 'megaphone', color: '#4fd8ff' },
  PlayerMilestone: { icon: 'trophy', color: '#c8ff4d' },
  ParentNotification: { icon: 'bell', color: '#f59e0b' },
  SubscriptionGrace: { icon: 'clock', color: '#fb7185' },
  ScouterNotification: { icon: 'eye', color: '#b58cff' },
};
const DEFAULT_VISUAL: NotificationVisual = { icon: 'bell', color: '#8b909a' };

@Component({
  selector: 'app-notifications-list',
  standalone: true,
  imports: [
    CommonModule,
    NavbarComponent,
    Footer,
    Pagination,
    CustomButtonComponent,
    LoadingSpinnerComponent,
    ScrollRevealDirective
  ],
  templateUrl: './notifications-list.html',
  styleUrl: './notifications-list.css',
})
export class NotificationsList implements OnInit {
  private notificationApi = inject(NotificationService);
  private signalRService = inject(SignalRService);
  private toastService = inject(ToastService);
  private destroyRef = inject(DestroyRef);
  private tokenStorage = inject(TokenStorageService);

  public allNotifications = signal<CachedNotification[]>([]);
  public isLoading = signal<boolean>(true);

  // Quick access filter signal for unread messages
  public showUnreadOnly = signal<boolean>(false);

  public pageSizeValue = 10;
  public pageNumber = signal<number>(1);

  // How many items we've actually asked the server for. Starts at 50 and
  // grows if the user pages past what's currently loaded -- see
  // loadMyNotifications()/goToPage() for why this exists.
  private loadedTake = 50;

  // Filter notifications based on quick access toggle
  public filteredNotifications = computed(() => {
    const list = this.allNotifications();
    if (this.showUnreadOnly()) {
      return list.filter(n => !n.isRead);
    }
    return list;
  });

  // NOTE: GetMyNotifications on the backend only accepts skip/take and
  // returns a bare array -- no total count. So this can only be a lower
  // bound: if we've loaded exactly `loadedTake` items, there may be more
  // beyond that we haven't fetched yet, and we nudge the count up so the
  // pagination control still lets the user page forward instead of
  // silently capping at whatever the first fetch happened to return.
  // The correct long-term fix is for the backend to return
  // { items, totalCount } like every other paginated endpoint in this app.
  public totalCount = computed(() => {
    const loaded = this.filteredNotifications().length;
    const mayHaveMore = !this.showUnreadOnly() && this.allNotifications().length >= this.loadedTake;
    return mayHaveMore ? loaded + this.pageSizeValue : loaded;
  });

  public unreadCount = computed(() => this.allNotifications().filter((n) => !n.isRead).length);

  public notifications = computed(() => {
    const start = (this.pageNumber() - 1) * this.pageSizeValue;
    return this.filteredNotifications().slice(start, start + this.pageSizeValue);
  });

  ngOnInit(): void {
    this.loadMyNotifications();

    this.signalRService.startConnection(() => this.tokenStorage.getAccessToken() || '');

    this.signalRService.notification$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((newNotif: CachedNotification) => {
        this.allNotifications.update((list) => [newNotif, ...list]);
        this.pageNumber.set(1);
      });

    this.destroyRef.onDestroy(() => {
      this.signalRService.stopConnection();
    });
  }

  public loadMyNotifications(): void {
    this.isLoading.set(true);
    // Grow the fetch window to cover whatever page we're currently on, in
    // case goToPage() triggered this reload because the user paged past
    // what was previously loaded.
    this.loadedTake = Math.max(50, this.pageNumber() * this.pageSizeValue);

    this.notificationApi
      .getMyNotifications(0, this.loadedTake)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (data: any) => {
          const items = Array.isArray(data) ? data : (data.items || []);
          this.allNotifications.set(items);
          this.isLoading.set(false);
        },
        error: (err: HttpErrorResponse) => {
          console.error('Failed to load notifications:', err);
          this.toastService.show(extractErrorMessage(err, 'Failed to load notifications.'), 'error');
          this.isLoading.set(false);
        },
      });
  }

  public goToPage(page: number): void {
    if (page === this.pageNumber() || this.isLoading()) return;
    this.pageNumber.set(page);

    // Paged beyond what we've fetched so far -- pull a bigger window rather
    // than showing an empty page for notifications that exist but were
    // never loaded.
    if (!this.showUnreadOnly() && page * this.pageSizeValue > this.loadedTake) {
      this.loadMyNotifications();
    }
  }

  public toggleUnreadFilter(): void {
    this.showUnreadOnly.update(v => !v);
    this.pageNumber.set(1);
  }

  public markAsRead(notificationId: string): void {
    const previous = this.allNotifications();
    this.allNotifications.update((list) =>
      list.map((n) => (n.id === notificationId ? { ...n, isRead: true } : n))
    );

    this.notificationApi
      .markAsRead(notificationId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        error: (err: HttpErrorResponse) => {
          console.error('Error marking notification as read:', err);
          this.allNotifications.set(previous);
          this.toastService.show(extractErrorMessage(err, 'Failed to mark notification as read.'), 'error');
        },
      });
  }

  public markAllAsRead(): void {
    const unreadList = this.allNotifications().filter(n => !n.isRead);
    if (unreadList.length === 0) return;

    // Optimistic update as before, but now each request's actual outcome is
    // tracked individually -- only items that genuinely fail get rolled
    // back, and the toast reflects what actually happened instead of
    // assuming success for everything the moment the optimistic update ran.
    this.allNotifications.update(list => list.map(n => ({ ...n, isRead: true })));

    const requests = unreadList.map(notif =>
      this.notificationApi.markAsRead(notif.id).pipe(
        map(() => ({ id: notif.id, success: true as const })),
        catchError((err: HttpErrorResponse) => {
          console.error(`Failed to mark ${notif.id} as read`, err);
          return of({ id: notif.id, success: false as const });
        })
      )
    );

    forkJoin(requests)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(results => {
        const failedIds = new Set(results.filter(r => !r.success).map(r => r.id));

        if (failedIds.size > 0) {
          this.allNotifications.update(list =>
            list.map(n => (failedIds.has(n.id) ? { ...n, isRead: false } : n))
          );
          this.toastService.show(
            `Marked ${results.length - failedIds.size} of ${results.length} as read -- ${failedIds.size} failed, please retry.`,
            'warning'
          );
        } else {
          this.toastService.show('All messages marked as read.', 'success');
        }
      });
  }

  // ── Display helpers ─────────────────────────────────────────

  public getNotifVisual(type: string): NotificationVisual {
    return TYPE_VISUALS[type] ?? DEFAULT_VISUAL;
  }

  public formatRelativeTime(dateStr: string |Date): string {
    const date = new Date(dateStr);
    const diffSec = Math.floor((Date.now() - date.getTime()) / 1000);

    if (diffSec < 5) return 'just now';
    if (diffSec < 60) return `${diffSec}s ago`;

    const diffMin = Math.floor(diffSec / 60);
    if (diffMin < 60) return `${diffMin}m ago`;

    const diffHr = Math.floor(diffMin / 60);
    if (diffHr < 24) return `${diffHr}h ago`;

    const diffDay = Math.floor(diffHr / 24);
    if (diffDay === 1) return 'yesterday';
    if (diffDay < 7) return `${diffDay}d ago`;

    const sameYear = date.getFullYear() === new Date().getFullYear();
    return date.toLocaleDateString('en-US', {
      month: 'short',
      day: 'numeric',
      year: sameYear ? undefined : 'numeric',
    });
  }
}