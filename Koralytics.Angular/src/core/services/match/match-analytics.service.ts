import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { PlayerReadinessDto } from '../../../core/interfaces/match-request.interfaces';

@Injectable({
  providedIn: 'root',
})
export class MatchAnalyticsService {
  private http = inject(HttpClient);
  private baseUrl = `${environment.apiUrl}/api/Match`;

  /** GET /api/Match/player/{playerId}/readiness */
  getPlayerReadiness(playerId: number): Observable<PlayerReadinessDto> {
    return this.http.get<PlayerReadinessDto>(
      `${this.baseUrl}/player/${playerId}/readiness`
    );
  }
}
