import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../../interfaces/api-response.model';
import { MatchListDto } from '../../models/Match/match-list.model';
import { MatchRequestListDto } from '../../models/Match/match-request.model';

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
    return this.http.get<ApiResponse<any>>(`${this.apiUrl}/${matchId}/timeline`, {
      params: { t: new Date().getTime().toString() }
    });
  }

  getLineup(matchId: number): Observable<ApiResponse<any>> {
    return this.http.get<ApiResponse<any>>(`${this.apiUrl}/${matchId}/lineup`, {
      params: { t: new Date().getTime().toString() }
    });
  }

  submitLineup(matchId: number, dto: { formation?: string; players: any[] }): Observable<ApiResponse<any>> {
    return this.http.post<ApiResponse<any>>(`${this.apiUrl}/${matchId}/lineup`, dto);
  }

  requestFriendlyMatch(dto: {
    requesterTeamId: number;
    targetTeamId: number;
    format: string;
    proposedDate: string;
    location?: string;
  }): Observable<ApiResponse<any>> {
    return this.http.post<ApiResponse<any>>(`${this.apiUrl}/request`, dto);
  }

  getIncomingRequests(
    teamId: number,
    page: number = 1,
    pageSize: number = 20,
    status?: string,
    dateFrom?: string,
    dateTo?: string
  ): Observable<ApiResponse<MatchRequestListDto>> {
    let params = new HttpParams()
      .set('teamId', teamId.toString())
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());

    if (status)   params = params.set('status', status);
    if (dateFrom) params = params.set('dateFrom', dateFrom);
    if (dateTo)   params = params.set('dateTo', dateTo);

    return this.http.get<ApiResponse<MatchRequestListDto>>(`${this.apiUrl}/request/incoming`, { params });
  }

  getOutgoingRequests(
    teamId: number,
    page: number = 1,
    pageSize: number = 20,
    status?: string,
    dateFrom?: string,
    dateTo?: string
  ): Observable<ApiResponse<MatchRequestListDto>> {
    let params = new HttpParams()
      .set('teamId', teamId.toString())
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());

    if (status)   params = params.set('status', status);
    if (dateFrom) params = params.set('dateFrom', dateFrom);
    if (dateTo)   params = params.set('dateTo', dateTo);

    return this.http.get<ApiResponse<MatchRequestListDto>>(`${this.apiUrl}/request/outgoing`, { params });
  }

  acceptMatchRequest(requestId: number): Observable<ApiResponse<any>> {
    return this.http.patch<ApiResponse<any>>(`${this.apiUrl}/request/${requestId}/accept`, {});
  }

  declineMatchRequest(requestId: number): Observable<ApiResponse<any>> {
    return this.http.patch<ApiResponse<any>>(`${this.apiUrl}/request/${requestId}/decline`, {});
  }

  createSessionMatch(dto: {
    sessionId: number;
    homePlayers: Array<{ playerId: number; isStarting: boolean; jerseyNumber?: number; positionInMatch?: string }>;
    awayPlayers: Array<{ playerId: number; isStarting: boolean; jerseyNumber?: number; positionInMatch?: string }>;
    format: string | number;
    matchDate: string;
    location?: string;
    formation?: string;
    awayFormation?: string;
  }): Observable<ApiResponse<any>> {
    return this.http.post<ApiResponse<any>>(`${this.apiUrl}/session`, dto);
  }

  logMatchEvent(matchId: number, dto: {
    teamId: number;
    playerId: number;
    assistPlayerId?: number | null;
    eventType: string | number;
    minute: number;
  }): Observable<ApiResponse<any>> {
    return this.http.post<ApiResponse<any>>(`${this.apiUrl}/${matchId}/events`, dto);
  }

  logSessionMatchEvent(matchId: number, dto: {
    playerId: number;
    assistPlayerId?: number | null;
    eventType: string | number;
    minute: number;
    isHomeSide: boolean;
  }): Observable<ApiResponse<any>> {
    return this.http.post<ApiResponse<any>>(`${this.apiUrl}/${matchId}/session-events`, dto);
  }

  deleteMatchEvent(matchId: number, eventId: number): Observable<ApiResponse<any>> {
    return this.http.delete<ApiResponse<any>>(`${this.apiUrl}/${matchId}/events/${eventId}`);
  }

  startMatch(matchId: number): Observable<ApiResponse<any>> {
    return this.http.patch<ApiResponse<any>>(`${this.apiUrl}/${matchId}/start`, {});
  }

  endMatch(matchId: number): Observable<ApiResponse<any>> {
    return this.http.patch<ApiResponse<any>>(`${this.apiUrl}/${matchId}/end`, {});
  }

  createTournamentMatch(dto: CreateTournamentMatchDto): Observable<ApiResponse<any>> {
    return this.http.post<ApiResponse<any>>(`${this.apiUrl}/tournament`, dto);
  }

  submitMatchRatings(matchId: number, dto: any): Observable<ApiResponse<any>> {
    return this.http.post<ApiResponse<any>>(`${this.apiUrl}/${matchId}/ratings`, dto);
  }

  getMatchRatings(matchId: number): Observable<ApiResponse<any>> {
    return this.http.get<ApiResponse<any>>(`${this.apiUrl}/${matchId}/ratings`);
  }

  getMatchReport(matchId: number, reportType: string = 'Match'): Observable<ApiResponse<any>> {
    const params = new HttpParams().set('reportType', reportType);
    return this.http.get<ApiResponse<any>>(`${this.apiUrl}/${matchId}/report`, { params });
  }

  generateMatchReport(matchId: number): Observable<ApiResponse<any>> {
    return this.http.post<ApiResponse<any>>(`${this.apiUrl}/${matchId}/generate-report`, {});
  }

  getHeadToHead(teamAId: number, teamBId: number): Observable<ApiResponse<HeadToHeadResponseDto>> {
    const params = new HttpParams()
      .set('teamAId', teamAId.toString())
      .set('teamBId', teamBId.toString());
    return this.http.get<ApiResponse<HeadToHeadResponseDto>>(`${this.apiUrl}/head-to-head`, { params });
  }

  getPostMatchAnalysis(teamId: number): Observable<ApiResponse<PostMatchAnalysisResponseDto>> {
    return this.http.get<ApiResponse<PostMatchAnalysisResponseDto>>(`${this.apiUrl}/team/${teamId}/analysis`);
  }
}

export interface CreateTournamentMatchDto {
  tournamentFixtureId: number;
  homeTeamId: number;
  awayTeamId: number;
  format?: number | string;
  matchDate: string;
  location?: string;
}

export interface HeadToHeadMatchDto {
  matchId: number;
  matchDate: string;
  homeTeamId: number;
  homeTeamName: string;
  homeAcademyName?: string;
  awayTeamId: number;
  awayTeamName: string;
  awayAcademyName?: string;
  homeScore: number;
  awayScore: number;
  homePenaltyScore?: number | null;
  awayPenaltyScore?: number | null;
}

export interface HeadToHeadResponseDto {
  teamAId: number;
  teamAName: string;
  teamAAcademyName?: string;
  teamBId: number;
  teamBName: string;
  teamBAcademyName?: string;
  totalMatches: number;
  teamAWins: number;
  teamBWins: number;
  draws: number;
  matches: HeadToHeadMatchDto[];
}

export interface PostMatchAnalysisMatchDto {
  matchId: number;
  matchDate: string;
  opponentName: string;
  result: string;
  goalsFor: number;
  goalsAgainst: number;
}

export interface PostMatchAnalysisResponseDto {
  teamId: number;
  teamName: string;
  wins: number;
  losses: number;
  draws: number;
  goalsFor: number;
  goalsAgainst: number;
  recentMatches: PostMatchAnalysisMatchDto[];
}

