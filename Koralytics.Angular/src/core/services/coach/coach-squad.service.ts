import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  SquadOverviewDto,
  SquadComparisonDto,
  TrainingTeamSplitDto,
  CoachTeamDto,
} from '../../../core/interfaces/coach.interfaces';

@Injectable({
  providedIn: 'root',
})
export class CoachSquadService {
  private http = inject(HttpClient);
  private baseUrl = `${environment.apiUrl}/api/Coach`;

  /** 
   * GET /api/Coach/{coachId}/teams/{teamId}/squad or /api/Coach/teams/{teamId}/squad 
   * Supports both (teamId, coachId) and (coachId, teamId) call signatures seamlessly.
   */
  getSquad(first: number, second?: number): Observable<any> {
    if (second !== undefined && second > 0) {
      return this.http.get<any>(`${this.baseUrl}/${second}/teams/${first}/squad`);
    }
    return this.http.get<any>(`${this.baseUrl}/teams/${first}/squad`);
  }

  getCoachTeams(coachId?: number): Observable<CoachTeamDto[]> {
    if (coachId && coachId > 0) {
      return this.http.get<CoachTeamDto[]>(`${this.baseUrl}/${coachId}/teams`);
    }
    return this.http.get<CoachTeamDto[]>(`${this.baseUrl}/teams`);
  }

  /** POST /api/Coach/sessions/{sessionId}/split */
  splitTrainingTeams(sessionId: number): Observable<TrainingTeamSplitDto> {
    return this.http.post<TrainingTeamSplitDto>(
      `${this.baseUrl}/sessions/${sessionId}/split`,
      {}
    );
  }

  /** GET /api/Coach/squad/compare?playerAId=&playerBId= */
  compareSquadPlayers(
    playerAId: number,
    playerBId: number
  ): Observable<SquadComparisonDto> {
    const params = new HttpParams()
      .set('playerAId', playerAId)
      .set('playerBId', playerBId);
    return this.http.get<SquadComparisonDto>(`${this.baseUrl}/squad/compare`, {
      params,
    });
  }
}
