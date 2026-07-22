import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import {
  ApiEnvelope,
  GenerateReportResponse,
  MessageOnlyResponse,
  PaginatedResult,
  PlayerCardDto,
  PlayerSearchFiltersDto,
  PlayerProfileViewAnalyticsDto,
  ScouterProfileDto,
  ScouterShortlistDto,
  ScouterReport,
} from '../../interfaces/Scouter.interfaces';


@Injectable({
  providedIn: 'root',
})
export class ScouterService {
  private http = inject(HttpClient);
  private baseUrl = `${environment.apiUrl}/api/scouter`;

  // ── Follow / Profile Views (ScouterFollowService) ──

  /** POST api/scouter/{scouterId}/follow/{playerId} */
  followPlayer(scouterId: number, playerId: number): Observable<MessageOnlyResponse> {
    return this.http.post<MessageOnlyResponse>(`${this.baseUrl}/${scouterId}/follow/${playerId}`, {});
  }

  /** DELETE api/scouter/{scouterId}/follow/{playerId} */
  unfollowPlayer(scouterId: number, playerId: number): Observable<MessageOnlyResponse> {
    return this.http.delete<MessageOnlyResponse>(`${this.baseUrl}/${scouterId}/follow/${playerId}`);
  }

  /**
   * POST api/scouter/{scouterId}/view-profile/{playerId}
   * Backend TODO notes this should move off the request thread (Redis/
   * background worker) -- fire-and-forget from the client is reasonable
   * either way; don't block UI on this call's result.
   */
  logProfileView(scouterId: number, playerId: number): Observable<MessageOnlyResponse> {
    return this.http.post<MessageOnlyResponse>(`${this.baseUrl}/${scouterId}/view-profile/${playerId}`, {});
  }

  /** GET api/scouter/{scouterId}/followed-players?pageNumber=&pageSize=&searchTerm= */
  getFollowedPlayers(
    scouterId: number,
    pageNumber: number = 1,
    pageSize: number = 10,
    searchTerm?: string
  ): Observable<PaginatedResult<PlayerCardDto>> {
    let params = new HttpParams().set('pageNumber', pageNumber).set('pageSize', pageSize);
    if (searchTerm) params = params.set('searchTerm', searchTerm);

    return this.http
      .get<ApiEnvelope<PaginatedResult<PlayerCardDto>>>(`${this.baseUrl}/${scouterId}/followed-players`, { params })
      .pipe(map((res) => res.data));
  }

  /**
   * GET api/scouter/{playerId}/profile-views
   * Player/Parent/SystemAdmin only -- not callable by a Scouter. Controller
   * enforces the caller's own playerId for Player/Parent roles.
   */
  getProfileViewsAnalytics(playerId: number): Observable<PlayerProfileViewAnalyticsDto> {
    return this.http
      .get<ApiEnvelope<PlayerProfileViewAnalyticsDto>>(`${this.baseUrl}/${playerId}/profile-views`)
      .pipe(map((res) => res.data));
  }

  // ── Search (ScouterSearchService) ──

  /**
   * POST api/scouter/search
   * POST rather than GET: PlayerSearchFiltersDto has enough optional fields
   * (positions array, rating ranges, etc.) that a query-string GET would get
   * unwieldy and awkward to encode correctly.
   */
  searchPlayers(filters: PlayerSearchFiltersDto): Observable<PaginatedResult<PlayerCardDto>> {
    return this.http
      .post<ApiEnvelope<PaginatedResult<PlayerCardDto>>>(`${this.baseUrl}/search`, filters)
      .pipe(map((res) => res.data));
  }

  /** GET api/scouter/me -- current scouter's own profile, id taken from auth claims */
  getMyProfile(): Observable<ScouterProfileDto> {
    return this.http.get<ApiEnvelope<ScouterProfileDto>>(`${this.baseUrl}/me`).pipe(map((res) => res.data));
  }

  /** GET api/scouter/{scouterId} -- SystemAdmin, Player, or Parent only (not Scouter -- use getMyProfile) */
  getScouterById(scouterId: number): Observable<ScouterProfileDto> {
    return this.http
      .get<ApiEnvelope<ScouterProfileDto>>(`${this.baseUrl}/${scouterId}`)
      .pipe(map((res) => res.data));
  }

  // ── Shortlist (ScouterShortlistService) ──

  /** POST api/scouter/{scouterId}/shortlist/{playerId} */
  addToShortlist(scouterId: number, playerId: number): Observable<ScouterShortlistDto> {
    return this.http
      .post<ApiEnvelope<ScouterShortlistDto>>(`${this.baseUrl}/${scouterId}/shortlist/${playerId}`, {})
      .pipe(map((res) => res.data));
  }

  /** DELETE api/scouter/{scouterId}/shortlist/{playerId} */
  removeFromShortlist(scouterId: number, playerId: number): Observable<MessageOnlyResponse> {
    return this.http.delete<MessageOnlyResponse>(`${this.baseUrl}/${scouterId}/shortlist/${playerId}`);
  }

  /** GET api/scouter/{scouterId}/shortlist?pageNumber=&pageSize=&searchTerm= */
  getShortlist(
    scouterId: number,
    pageNumber: number = 1,
    pageSize: number = 10,
    searchTerm?: string
  ): Observable<PaginatedResult<PlayerCardDto>> {
    let params = new HttpParams().set('pageNumber', pageNumber).set('pageSize', pageSize);
    if (searchTerm) params = params.set('searchTerm', searchTerm);

    return this.http
      .get<ApiEnvelope<PaginatedResult<PlayerCardDto>>>(`${this.baseUrl}/${scouterId}/shortlist`, { params })
      .pipe(map((res) => res.data));
  }

  // ── Reports / Verification (ScouterReportService) ──

  /**
   * GET api/scouter/{scouterId}/reports/{playerId}
   * NOTE: falls back to AI generation server-side when no cached report
   * exists, and that generation path currently throws NotImplementedException
   * ("need aiservice"). Expect this to fail for any player without an
   * existing report row until the backend AI service is implemented.
   */
  getScoutingReport(scouterId: number, playerId: number): Observable<ScouterReport> {
    return this.http
      .get<ApiEnvelope<ScouterReport>>(`${this.baseUrl}/${scouterId}/reports/${playerId}`)
      .pipe(map((res) => res.data));
  }

  /**
   * POST api/scouter/{scouterId}/reports/{playerId}/generate
   * NOTE: currently throws NotImplementedException server-side -- same AI
   * service gap as getScoutingReport. Wire it up, but expect it to 500 for now.
   * Response is `{ message, report }`, not `{ message, data }` -- kept as its
   * own type (GenerateReportResponse) rather than unwrapped, since "report"
   * here is plain text, not a ScouterReport entity.
   */
  generateScoutingReport(scouterId: number, playerId: number): Observable<GenerateReportResponse> {
    return this.http.post<GenerateReportResponse>(`${this.baseUrl}/${scouterId}/reports/${playerId}/generate`, {});
  }

  /**
   * POST api/scouter/{scouterId}/verify
   * SystemAdmin only (enforced server-side via [Authorize(Roles = "SystemAdmin")]).
   * Gate this in the UI accordingly (e.g. only show it in an admin panel).
   */
  verifyScouter(scouterId: number): Observable<MessageOnlyResponse> {
    return this.http.post<MessageOnlyResponse>(`${this.baseUrl}/${scouterId}/verify`, {});
  }
}