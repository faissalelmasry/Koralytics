import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class CoachSquadService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/api/Coach`;

  getSquad(teamId: number, coachId?: number): Observable<any> {
    if (coachId && coachId > 0) {
      return this.http.get<any>(`${this.apiUrl}/${coachId}/teams/${teamId}/squad`);
    }
    return this.http.get<any>(`${this.apiUrl}/teams/${teamId}/squad`);
  }

  getCoachTeams(): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/teams`);
  }
}
