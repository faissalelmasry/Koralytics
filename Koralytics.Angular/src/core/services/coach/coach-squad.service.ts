import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  SquadOverviewDto,
  SquadComparisonDto,
  TrainingTeamSplitDto,
} from '../../../core/interfaces/coach.interfaces';

@Injectable({
  providedIn: 'root',
})
export class CoachSquadService {
  private http = inject(HttpClient);
  private baseUrl = `${environment.apiUrl}/api/Coach`;

  /** GET /api/Coach/{coachId}/teams/{teamId}/squad */
  getSquad(teamId: number, coachId?: number): Observable<SquadOverviewDto> {
    if (coachId && coachId > 0) {
      return this.http.get<SquadOverviewDto>(
        `${this.baseUrl}/${coachId}/teams/${teamId}/squad`
      );
    }
    return this.http.get<SquadOverviewDto>(
      `${this.baseUrl}/teams/${teamId}/squad`
    );
  }

  getCoachTeams(): Observable<any[]> {
    return this.http.get<any[]>(`${this.baseUrl}/teams`);
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
