import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { PlayerProfileModel } from '../../models/Player/player-profile-model';

@Injectable({
  providedIn: 'root'
})
export class PlayerProfileService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/api`;

  getPlayerProfile(playerId: number): Observable<PlayerProfileModel> {
    return this.http.get<PlayerProfileModel>(`${this.apiUrl}/Player/${playerId}/profile`);
  }
}
