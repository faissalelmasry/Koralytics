import { Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpErrorResponse } from '@angular/common/http';
import { SignalRService } from '../../../../../core/services/SignalR/signalrservice';
import { CachedNotification } from '../../../../../core/interfaces/CachedNotification';
import { ToastService } from '../../../../../core/services/Toast/toast';
import { NotificationService } from '../../../../../core/services/SignalR/notificationservice';
import { extractErrorMessage } from '../../../../../core/utils/http-error.util';


const PAGE_SIZE = 50;

@Component({
  selector: 'app-notifications-list',
  imports: [CommonModule],
  templateUrl: './notifications-list.html',
  styleUrl: './notifications-list.css',
})
export class NotificationsList implements OnInit {
  private notificationApi = inject(NotificationService);
  private signalRService = inject(SignalRService);
  private toastService = inject(ToastService);
  private destroyRef = inject(DestroyRef);

  public notifications = signal<CachedNotification[]>([]);
  public isLoading = signal<boolean>(true);
  public isLoadingMore = signal<boolean>(false);
  public hasMore = signal<boolean>(true);

  private currentSkip = 0;

  ngOnInit(): void {
    this.loadMyNotifications();

    // notification$ now actually emits (previously the service only showed a
    // toast and never pushed into this Subject, so live items never arrived
    // here without a manual refresh).
    this.signalRService.notification$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((newNotif: CachedNotification) => {
      this.notifications.update((currentList) => [newNotif, ...currentList]);
    });
  }

  public loadMyNotifications(): void {
    this.isLoading.set(true);
    this.currentSkip = 0;

    this.notificationApi
      .getMyNotifications(this.currentSkip, PAGE_SIZE)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (data) => {
          this.notifications.set(data);
          this.hasMore.set(data.length === PAGE_SIZE);
          this.isLoading.set(false);
        },
        error: (err: HttpErrorResponse) => {
          console.error('Failed to load active notification feed:', err);
          this.toastService.show(extractErrorMessage(err, 'Failed to load notifications.'), 'error');
          this.isLoading.set(false);
        },
      });
  }

  public loadMore(): void {
    if (this.isLoadingMore() || !this.hasMore()) return;

    this.isLoadingMore.set(true);
    const nextSkip = this.currentSkip + PAGE_SIZE;

    this.notificationApi
      .getMyNotifications(nextSkip, PAGE_SIZE)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (data) => {
          this.notifications.update((current) => [...current, ...data]);
          this.currentSkip = nextSkip;
          this.hasMore.set(data.length === PAGE_SIZE);
          this.isLoadingMore.set(false);
        },
        error: (err: HttpErrorResponse) => {
          console.error('Failed to load more notifications:', err);
          this.toastService.show(extractErrorMessage(err, 'Failed to load more notifications.'), 'error');
          this.isLoadingMore.set(false);
        },
      });
  }

  public markAsRead(notificationId: string): void {
    // Optimistic update so the UI feels instant; rolled back on failure.
    const previous = this.notifications();
    this.notifications.update((list) => list.map((n) => (n.id === notificationId ? { ...n, isRead: true } : n)));

    this.notificationApi
      .markAsRead(notificationId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        error: (err: HttpErrorResponse) => {
          console.error('Error marking notification as read:', err);
          this.notifications.set(previous);
          this.toastService.show(extractErrorMessage(err, 'Failed to mark notification as read.'), 'error');
        },
      });
  }
}