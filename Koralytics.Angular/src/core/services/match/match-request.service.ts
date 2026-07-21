import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  CreateMatchRequestDto,
  MatchRequestResponseDto,
} from '../../../core/interfaces/match-request.interfaces';

@Injectable({
  providedIn: 'root',
})
export class MatchRequestService {
  private http = inject(HttpClient);
  private baseUrl = `${environment.apiUrl}/api/Match`;

  /** POST /api/Match/request */
  requestFriendlyMatch(
    dto: CreateMatchRequestDto
  ): Observable<MatchRequestResponseDto> {
    return this.http.post<MatchRequestResponseDto>(
      `${this.baseUrl}/request`,
      dto
    );
  }

  /** PATCH /api/Match/request/{requestId}/accept */
  acceptMatchRequest(requestId: number): Observable<MatchRequestResponseDto> {
    return this.http.patch<MatchRequestResponseDto>(
      `${this.baseUrl}/request/${requestId}/accept`,
      {}
    );
  }

  /** PATCH /api/Match/request/{requestId}/decline */
  declineMatchRequest(requestId: number): Observable<void> {
    return this.http.patch<void>(
      `${this.baseUrl}/request/${requestId}/decline`,
      {}
    );
  }

  /** GET /api/Match/request/incoming?teamId= */
  getIncomingRequests(
    teamId: number
  ): Observable<MatchRequestResponseDto[]> {
    const params = new HttpParams().set('teamId', teamId);
    return this.http.get<MatchRequestResponseDto[]>(
      `${this.baseUrl}/request/incoming`,
      { params }
    );
  }

  /** GET /api/Match/request/outgoing?teamId= */
  getOutgoingRequests(
    teamId: number
  ): Observable<MatchRequestResponseDto[]> {
    const params = new HttpParams().set('teamId', teamId);
    return this.http.get<MatchRequestResponseDto[]>(
      `${this.baseUrl}/request/outgoing`,
      { params }
    );
  }
}
