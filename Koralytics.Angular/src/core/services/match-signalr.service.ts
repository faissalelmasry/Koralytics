import { Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { Subject } from 'rxjs';
import { environment } from '../../environments/environment';

export interface LiveMatchScoreUpdateDto {
  matchId: number;
  homeScore: number;
  awayScore: number;
  homePenaltyScore: number | null;
  awayPenaltyScore: number | null;
  status: string;
}

export interface LiveMatchEventUpdateDto {
  matchId: number;
  event: any; // Ideally MatchEventResponseDto type
}

export interface LiveMatchEventDeletedDto {
  matchId: number;
  eventId: number;
}

@Injectable({
  providedIn: 'root'
})
export class MatchSignalrService {
  private hubConnection: signalR.HubConnection | null = null;

  private matchScoreUpdateSource = new Subject<LiveMatchScoreUpdateDto>();
  public matchScoreUpdate$ = this.matchScoreUpdateSource.asObservable();

  private matchEventUpdateSource = new Subject<LiveMatchEventUpdateDto>();
  public matchEventUpdate$ = this.matchEventUpdateSource.asObservable();

  private matchEventDeletedSource = new Subject<LiveMatchEventDeletedDto>();
  public matchEventDeleted$ = this.matchEventDeletedSource.asObservable();

  /** Groups queued to join before the connection was ready */
  private pendingJoins = new Set<string>();

  constructor() {
    this.startConnection();
  }

  private startConnection(): void {
    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(`${environment.apiUrl}/hubs/match`)
      .withAutomaticReconnect()
      .build();

    // After an automatic reconnect, re-join every group we were in
    this.hubConnection.onreconnected(() => {
      console.log('[MatchHub] Reconnected – rejoining groups:', [...this.pendingJoins]);
      this.pendingJoins.forEach(id => this.invokeJoin(id));
    });

    this.hubConnection
      .start()
      .then(() => {
        console.log('[MatchHub] Connection started');
        this.addListeners();
        // Flush any groups that were requested before the connection was ready
        if (this.pendingJoins.size > 0) {
          console.log('[MatchHub] Flushing pending joins:', [...this.pendingJoins]);
          this.pendingJoins.forEach(id => this.invokeJoin(id));
        }
      })
      .catch(err => console.error('[MatchHub] Connection error:', err));
  }

  private addListeners(): void {
    if (!this.hubConnection) return;

    this.hubConnection.on('ReceiveMatchScoreUpdate', (update: LiveMatchScoreUpdateDto) => {
      this.matchScoreUpdateSource.next(update);
    });

    this.hubConnection.on('ReceiveMatchEventUpdate', (update: LiveMatchEventUpdateDto) => {
      this.matchEventUpdateSource.next(update);
    });

    this.hubConnection.on('ReceiveMatchEventDeleted', (update: LiveMatchEventDeletedDto) => {
      this.matchEventDeletedSource.next(update);
    });
  }

  private invokeJoin(matchId: string): void {
    this.hubConnection!.invoke('JoinLiveMatch', matchId)
      .then(() => console.log(`[MatchHub] Joined group: ${matchId}`))
      .catch(err => console.error('[MatchHub] JoinLiveMatch error:', err));
  }

  private invokeLeave(matchId: string): void {
    this.hubConnection!.invoke('LeaveLiveMatch', matchId)
      .then(() => console.log(`[MatchHub] Left group: ${matchId}`))
      .catch(err => console.error('[MatchHub] LeaveLiveMatch error:', err));
  }

  public joinMatchGroup(matchId: number | string): void {
    const id = matchId.toString();
    this.pendingJoins.add(id);

    if (this.hubConnection?.state === signalR.HubConnectionState.Connected) {
      this.invokeJoin(id);
    }
    // If not connected yet: the join will be flushed in startConnection().then()
    // or in onreconnected() — no need for a fragile setTimeout
  }

  public leaveMatchGroup(matchId: number | string): void {
    const id = matchId.toString();
    this.pendingJoins.delete(id);

    if (this.hubConnection?.state === signalR.HubConnectionState.Connected) {
      this.invokeLeave(id);
    }
  }
}
