import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { PlayerProfileModel } from '../../models/Player/player-profile-model';
import { MatchTimelineDto } from '../../models/Player/match-timeline-model';
import { DrillTimelineDto } from '../../models/Player/drill-timeline-model';
import { TeamScheduledEventsResponseDto } from '../../models/Player/scheduled-event-model';
import { ProfileViewsResponse } from '../../models/Player/profile-views-model';
import { PlayerVsAcademyModel } from '../../models/Player/player-vs-academy-model';

@Injectable({
  providedIn: 'root'
})
export class PlayerProfileService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/api`;

  getPlayerProfile(playerId: number): Observable<PlayerProfileModel> {
    return this.http.get<PlayerProfileModel>(`${this.apiUrl}/Player/${playerId}/profile`);
  }

  getMatchTimeline(
    playerId: number,
    page: number = 1,
    pageSize: number = 20,
    matchType?: string,
    dateFrom?: string,
    dateTo?: string
  ): Observable<MatchTimelineDto> {
    let params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());

    if (matchType) {
      params = params.set('matchType', matchType);
    }
    if (dateFrom) {
      params = params.set('dateFrom', dateFrom);
    }
    if (dateTo) {
      params = params.set('dateTo', dateTo);
    }

    return this.http.get<MatchTimelineDto>(`${this.apiUrl}/Player/${playerId}/timeline/matches`, { params });
  }

  getDrillTimeline(
    playerId: number,
    page: number = 1,
    pageSize: number = 20,
    dateFrom?: string,
    dateTo?: string
  ): Observable<DrillTimelineDto> {
    let params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());

    if (dateFrom) {
      params = params.set('dateFrom', dateFrom);
    }
    if (dateTo) {
      params = params.set('dateTo', dateTo);
    }

    return this.http.get<DrillTimelineDto>(`${this.apiUrl}/Player/${playerId}/timeline/drills`, { params });
  }

  getTeamScheduledEvents(
    playerId: number,
    page: number = 1,
    pageSize: number = 20,
    eventType?: string,
    dateFrom?: string,
    dateTo?: string
  ): Observable<TeamScheduledEventsResponseDto> {
    let params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());

    if (eventType) {
      params = params.set('eventType', eventType);
    }
    if (dateFrom) {
      params = params.set('dateFrom', dateFrom);
    }
    if (dateTo) {
      params = params.set('dateTo', dateTo);
    }

    return this.http.get<TeamScheduledEventsResponseDto>(`${this.apiUrl}/Player/${playerId}/team/scheduled`, { params });
  }

  getProfileViews(playerId: number): Observable<ProfileViewsResponse> {
    return this.http.get<ProfileViewsResponse>(`${this.apiUrl}/Scouter/${playerId}/profile-views`);
  }

  getPlayerVsAcademyAverage(): Observable<PlayerVsAcademyModel> {
    return this.http.get<PlayerVsAcademyModel>(`${this.apiUrl}/Player/academy-comparison`);
  }
}
