import { DestroyRef, inject, Injectable, signal, NgZone } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { Subject } from 'rxjs';
import { CachedNotification } from '../../interfaces/CachedNotification';
import { ToastService, ToastType } from '../Toast/toast';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root',
})
export class SignalRService {
  private hubConnection!: signalR.HubConnection;
  private toastService = inject(ToastService);
  private destroyRef = inject(DestroyRef);
  private ngZone = inject(NgZone); 
  
  public notification$ = new Subject<CachedNotification>();
  public announcement$ = new Subject<CachedNotification>();
  public connectionState = signal<signalR.HubConnectionState>(signalR.HubConnectionState.Disconnected);

  private retryAttempts = 0;
  private readonly maxRetryAttempts = 6;
  private readonly maxRetryDelayMs = 30000;
  private retryTimeoutId: ReturnType<typeof setTimeout> | null = null;
  private manuallyStopped = false;
  private getToken: (() => string) | null = null;

  constructor() {
    this.destroyRef.onDestroy(() => this.stopConnection());
  }

  public startConnection(tokenProvider: string | (() => string)): void {
    this.manuallyStopped = false;
    this.retryAttempts = 0;
    this.getToken = typeof tokenProvider === 'function' ? tokenProvider : () => tokenProvider;

    this.buildConnection();
    this.attemptStart();
  }

  private buildConnection(): void {
    // Matches the backend's app.MapHub<NotificationHub>("/hubs/notifications").
    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(`${environment.apiUrl}/hubs/notifications`, {
        accessTokenFactory: () => (this.getToken ? this.getToken() : ''),
      })
      .withAutomaticReconnect([0, 2000, 10000, 30000])
      .configureLogging(signalR.LogLevel.Information)
      .build();

    this.hubConnection.onreconnecting(() => {
      this.connectionState.set(signalR.HubConnectionState.Reconnecting);
    });

    this.hubConnection.onreconnected(() => {
      this.retryAttempts = 0;
      this.connectionState.set(signalR.HubConnectionState.Connected);
    });

    this.hubConnection.onclose(() => {
      this.connectionState.set(signalR.HubConnectionState.Disconnected);
      if (!this.manuallyStopped) {
        this.scheduleRetry();
      }
    });
    
    this.registerServerEvents();
  }

  private attemptStart(): void {
    if (this.manuallyStopped) return;

    this.connectionState.set(signalR.HubConnectionState.Connecting);

    this.hubConnection
      .start()
      .then(() => {
        console.log('Successfully connected to Koralytics NotificationHub via SignalR.');
        this.retryAttempts = 0;
        this.connectionState.set(signalR.HubConnectionState.Connected);
      })
      .catch((err: any) => {
        console.error('SignalR Hub Connection Initiation Failed: ', err);
        this.connectionState.set(signalR.HubConnectionState.Disconnected);
        this.scheduleRetry();
      });
  }

  private scheduleRetry(): void {
    if (this.manuallyStopped || this.retryTimeoutId) return;

    if (this.retryAttempts >= this.maxRetryAttempts) {
      console.error(
        `SignalR: giving up after ${this.retryAttempts} failed attempts. ` +
        `Real-time notifications will stay off until the page is reloaded or startConnection() is called again.`
      );
      return;
    }

    const delay = Math.min(1000 * 2 ** this.retryAttempts, this.maxRetryDelayMs);
    this.retryAttempts++;

    this.retryTimeoutId = setTimeout(() => {
      this.retryTimeoutId = null;
      this.buildConnection();
      this.attemptStart();
    }, delay);
  }

  private registerServerEvents(): void {
    // 👈 تغليف جميع الأحداث بـ ngZone.run
    this.hubConnection.on('ReceiveAnnouncement', (data: CachedNotification) => {
      this.ngZone.run(() => {
        this.triggerToastNotification(data);
        this.announcement$.next(data);
        this.notification$.next(data);
      });
    });

    this.hubConnection.on('ReceiveMilestoneNotification', (data: CachedNotification) => {
      this.ngZone.run(() => {
        this.triggerToastNotification(data);
        this.notification$.next(data);
      });
    });

    this.hubConnection.on('ReceiveParentNotification', (data: CachedNotification) => {
      this.ngZone.run(() => {
        this.triggerToastNotification(data);
        this.notification$.next(data);
      });
    });

    this.hubConnection.on('ReceiveSubscriptionGraceNotification', (data: CachedNotification) => {
      this.ngZone.run(() => {
        this.triggerToastNotification(data);
        this.notification$.next(data);
      });
    });

    this.hubConnection.on('ReceiveScouterNotification', (data: CachedNotification) => {
      this.ngZone.run(() => {
        this.triggerToastNotification(data);
        this.notification$.next(data);
      });
    });
    
    this.hubConnection.on('ReceiveMatchEventNotification', (data: CachedNotification) => {
      this.ngZone.run(() => {
        this.triggerToastNotification(data);
        this.notification$.next(data);
      });
    });
  }

  private triggerToastNotification(notification: CachedNotification): void {
    const fullMessage = `${notification.title}: ${notification.content}`;
    this.toastService.show(fullMessage, this.resolveToastType(notification.type));
  }

  private resolveToastType(type: string): ToastType {
    switch (type) {
      case 'AcademyAnnouncement':
        return 'info';
      case 'PlayerMilestone':
      case 'MatchStarted':
      case 'GoalScored':
        return 'success';
      case 'SubscriptionGrace':
      case 'ParentNotification':
        return 'warning';
      case 'ScouterNotification':
      case 'MatchEnded':
        return 'info';
      case 'PlayerSentOff':
      case 'PenaltyMissed':
        return 'error';
      default:
        return 'info';
    }
  }

  public stopConnection(): void {
    this.manuallyStopped = true;

    if (this.retryTimeoutId) {
      clearTimeout(this.retryTimeoutId);
      this.retryTimeoutId = null;
    }

    if (this.hubConnection) {
      this.hubConnection
        .stop()
        .then(() => {
          console.log('SignalR Connection stopped successfully.');
          this.connectionState.set(signalR.HubConnectionState.Disconnected);
        })
        .catch((err: any) => console.error('Error stopping SignalR connection: ', err));
    }
  }
}