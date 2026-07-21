import { DestroyRef, inject, Injectable, signal } from '@angular/core';
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

  // Live feed of every notification type (announcements, milestones, parent
  // alerts, subscription grace, scouter alerts) — used by any component that
  // wants to prepend new items to a list in real time.
  public notification$ = new Subject<CachedNotification>();

  // Narrower stream for academy-announcement-specific listeners (e.g. an
  // announcements history view that wants to show a just-sent broadcast
  // without a full page refresh).
  public announcement$ = new Subject<CachedNotification>();

  // Exposed so components can show a connection indicator instead of only
  // finding out something's wrong when a toast never appears.
  public connectionState = signal<signalR.HubConnectionState>(signalR.HubConnectionState.Disconnected);

  constructor() {
    this.destroyRef.onDestroy(() => this.stopConnection());
  }

  /**
   * Starts the SignalR connection.
   *
   * @param tokenProvider Either a static token string, or (preferred) a
   * function that returns the current access token on demand. SignalR calls
   * accessTokenFactory fresh on every negotiate/reconnect attempt, so passing
   * a function here (e.g. `() => this.authService.getAccessToken()`) means a
   * rotated/refreshed token is picked up automatically. Passing a plain
   * string keeps working but freezes the token at connection-start time,
   * which will fail reconnects after the original token expires.
   */
  public startConnection(tokenProvider: string | (() => string)): void {
    const getToken = typeof tokenProvider === 'function' ? tokenProvider : () => tokenProvider;

    // Matches the backend's app.MapHub<NotificationHub>("/hubs/notifications").
    // The previous "/notificationHub" path doesn't exist and was causing every
    // connection attempt to fail its negotiate request.
    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(`${environment.apiUrl}/hubs/notifications`, {
        accessTokenFactory: () => getToken(),
      })
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Information)
      .build();

    this.hubConnection.onreconnecting(() => {
      this.connectionState.set(signalR.HubConnectionState.Reconnecting);
    });

    this.hubConnection.onreconnected(() => {
      this.connectionState.set(signalR.HubConnectionState.Connected);
    });

    this.hubConnection.onclose(() => {
      this.connectionState.set(signalR.HubConnectionState.Disconnected);
    });

    this.registerServerEvents();

    this.hubConnection
      .start()
      .then(() => {
        console.log('Successfully connected to Koralytics NotificationHub via SignalR.');
        this.connectionState.set(signalR.HubConnectionState.Connected);
      })
      .catch((err) => {
        console.error('SignalR Hub Connection Initiation Failed: ', err);
        this.connectionState.set(signalR.HubConnectionState.Disconnected);
      });
  }

  private registerServerEvents(): void {
    // Client method names must match the strings the backend passes to
    // SendAsync/SendAndCacheToUserAsync in RealTimeBridge / the notification
    // services exactly:
    //   ReceiveAnnouncement, ReceiveMilestoneNotification,
    //   ReceiveParentNotification, ReceiveSubscriptionGraceNotification,
    //   ReceiveScouterNotification.
    // There is no "ReceiveNotification" method sent by the backend today —
    // the old handler for it was dead code and has been removed.

    this.hubConnection.on('ReceiveAnnouncement', (data: CachedNotification) => {
      this.triggerToastNotification(data);
      this.announcement$.next(data);
      this.notification$.next(data);
    });

    this.hubConnection.on('ReceiveMilestoneNotification', (data: CachedNotification) => {
      this.triggerToastNotification(data);
      this.notification$.next(data);
    });

    this.hubConnection.on('ReceiveParentNotification', (data: CachedNotification) => {
      this.triggerToastNotification(data);
      this.notification$.next(data);
    });

    this.hubConnection.on('ReceiveSubscriptionGraceNotification', (data: CachedNotification) => {
      this.triggerToastNotification(data);
      this.notification$.next(data);
    });

    this.hubConnection.on('ReceiveScouterNotification', (data: CachedNotification) => {
      this.triggerToastNotification(data);
      this.notification$.next(data);
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
        return 'success';
      case 'SubscriptionGrace':
        return 'warning';
      case 'ParentNotification':
        return 'warning';
      case 'ScouterNotification':
        return 'info';
      default:
        return 'info';
    }
  }

  public stopConnection(): void {
    if (this.hubConnection) {
      this.hubConnection
        .stop()
        .then(() => {
          console.log('SignalR Connection stopped successfully.');
          this.connectionState.set(signalR.HubConnectionState.Disconnected);
        })
        .catch((err) => console.error('Error stopping SignalR connection: ', err));
    }
  }
}