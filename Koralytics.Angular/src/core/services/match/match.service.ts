import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../../interfaces/api-response.model';
import { MatchListDto } from '../../models/Match/match-list.model';

@Injectable({
  providedIn: 'root'
})
export class MatchService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/api/Match`;

  getCoachMatches(
    status?: string,
    type?: string,
    dateFrom?: string,
    dateTo?: string,
    page: number = 1,
    pageSize: number = 20
  ): Observable<ApiResponse<MatchListDto>> {
    let params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());

    if (status)  params = params.set('status', status);
    if (type)    params = params.set('type', type);
    if (dateFrom) params = params.set('dateFrom', dateFrom);
    if (dateTo)   params = params.set('dateTo', dateTo);

    return this.http.get<ApiResponse<MatchListDto>>(`${this.apiUrl}/coach`, { params });
  }

  getAcademyMatches(
    academyId: number,
    teamId?: number,
    ageGroupId?: number,
    status?: string,
    type?: string,
    dateFrom?: string,
    dateTo?: string,
    page: number = 1,
    pageSize: number = 20
  ): Observable<ApiResponse<MatchListDto>> {
    let params = new HttpParams()
      .set('academyId', academyId.toString())
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());

    if (teamId)      params = params.set('teamId', teamId.toString());
    if (ageGroupId)  params = params.set('ageGroupId', ageGroupId.toString());
    if (status)      params = params.set('status', status);
    if (type)        params = params.set('type', type);
    if (dateFrom)    params = params.set('dateFrom', dateFrom);
    if (dateTo)      params = params.set('dateTo', dateTo);

    return this.http.get<ApiResponse<MatchListDto>>(`${this.apiUrl}/academy`, { params });
  }

  getMatch(matchId: number): Observable<ApiResponse<any>> {
    return this.http.get<ApiResponse<any>>(`${this.apiUrl}/${matchId}`);
  }

  getMatchTimeline(matchId: number): Observable<ApiResponse<any>> {
    return this.http.get<ApiResponse<any>>(`${this.apiUrl}/${matchId}/timeline`);
  }

  getLineup(matchId: number): Observable<ApiResponse<any>> {
    return this.http.get<ApiResponse<any>>(`${this.apiUrl}/${matchId}/lineup`);
  }
}
