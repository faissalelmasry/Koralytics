import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { PlayerCardModel } from '../../models/Player/player-card-model';
import { MiniPlayerCardModel } from '../../models/Player/mini-player-card-model';
import { TransferRateModel } from '../../models/Player/transfer-rate-model';

@Injectable({
  providedIn: 'root'
})
export class PlayerCardService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/api`;

  getPlayerCard(playerId: number): Observable<PlayerCardModel> {
    return this.http.get<PlayerCardModel>(`${this.apiUrl}/Player/${playerId}/card`);
  }

  recalculatePlayerCard(playerId: number): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/Player/${playerId}/card/recalculate`, {});
  }

  getMiniPlayerCards(playerIds: number[]): Observable<MiniPlayerCardModel[]> {
    let params = new HttpParams();
    playerIds.forEach(id => params = params.append('playerIds', id.toString()));
    return this.http.get<MiniPlayerCardModel[]>(`${this.apiUrl}/Player/mini-cards`, { params });
  }

  getTransferRate(playerId: number): Observable<TransferRateModel> {
    return this.http.get<TransferRateModel>(`${this.apiUrl}/Player/${playerId}/transfer-rate`);
  }
}
