import { Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { BehaviorSubject, Subject } from 'rxjs';
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

@Injectable({
  providedIn: 'root'
})
export class MatchSignalrService {
  private hubConnection: signalR.HubConnection | null = null;
  
  private matchScoreUpdateSource = new Subject<LiveMatchScoreUpdateDto>();
  public matchScoreUpdate$ = this.matchScoreUpdateSource.asObservable();

  private matchEventUpdateSource = new Subject<LiveMatchEventUpdateDto>();
  public matchEventUpdate$ = this.matchEventUpdateSource.asObservable();

  constructor() {
    this.startConnection();
  }

  private startConnection() {
    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(`${environment.apiUrl}/hubs/match`) // Assuming environment.apiUrl exists, otherwise hardcode or get from another service
      .withAutomaticReconnect()
      .build();

    this.hubConnection
      .start()
      .then(() => {
        console.log('MatchHub Connection Started');
        this.addListeners();
      })
      .catch(err => console.error('Error while starting MatchHub connection: ', err));
  }

  private addListeners() {
    if (!this.hubConnection) return;

    this.hubConnection.on('ReceiveMatchScoreUpdate', (update: LiveMatchScoreUpdateDto) => {
      this.matchScoreUpdateSource.next(update);
    });

    this.hubConnection.on('ReceiveMatchEventUpdate', (update: LiveMatchEventUpdateDto) => {
      this.matchEventUpdateSource.next(update);
    });
  }

  public joinMatchGroup(matchId: number | string) {
    if (this.hubConnection?.state === signalR.HubConnectionState.Connected) {
      this.hubConnection.invoke('JoinLiveMatch', matchId.toString())
        .catch(err => console.error(err));
    } else {
      // If not connected yet, we could wait or retry. 
      // For simplicity, we assume startConnection finishes fast enough or use a queue.
      setTimeout(() => {
        if (this.hubConnection?.state === signalR.HubConnectionState.Connected) {
          this.hubConnection.invoke('JoinLiveMatch', matchId.toString()).catch(err => console.error(err));
        }
      }, 1000);
    }
  }

  public leaveMatchGroup(matchId: number | string) {
    if (this.hubConnection?.state === signalR.HubConnectionState.Connected) {
      this.hubConnection.invoke('LeaveLiveMatch', matchId.toString())
        .catch(err => console.error(err));
    }
  }
}
