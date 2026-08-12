import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../../interfaces/api-response.model';
import {
  AcademyRequestResponseDto,
  CreateAcademyRequestDto,
  AcademyAdminResponseDto,
  AcademyResponseDto,
  AcademyBadgeResponseDto,
  UpdatePlayerSubscriptionDto,
  PaginationRequestDto,
  AcademyMemberResponseDto,
  PagedResponseDto,
  AcademyLocationResponseDto,
  AgeGroupResponseDto,
  CreateAgeGroupDto,
  TeamResponseDto,
  CreateTeamDto
} from '../../interfaces/academy.models';

@Injectable({
  providedIn: 'root'
})
export class AcademyService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/api/Academy`;
  constructor() { }

  // ==================== Academy Requests ====================

  requestAcademy(dto: CreateAcademyRequestDto): Observable<ApiResponse<AcademyRequestResponseDto>> {
    return this.http.post<ApiResponse<AcademyRequestResponseDto>>(`${this.apiUrl}/requests`, dto);
  }

  getMyAcademyRequests(): Observable<ApiResponse<AcademyRequestResponseDto[]>> {
    return this.http.get<ApiResponse<AcademyRequestResponseDto[]>>(`${this.apiUrl}/requests/my-requests`);
  }

  getAcademyById(academyId: number): Observable<ApiResponse<AcademyResponseDto>> {
    return this.http.get<ApiResponse<AcademyResponseDto>>(`${this.apiUrl}/${academyId}`);
  }

  updateAcademyLogo(academyId: number, image: File): Observable<ApiResponse<any>> {
    const formData = new FormData();
    formData.append('image', image);
    return this.http.patch<ApiResponse<any>>(`${this.apiUrl}/${academyId}/logo`, formData);
  }

  removeAcademyLogo(academyId: number): Observable<ApiResponse<any>> {
    return this.http.delete<ApiResponse<any>>(`${this.apiUrl}/${academyId}/logo`);
  }

  getAcademyBadges(academyId: number): Observable<ApiResponse<AcademyBadgeResponseDto[]>> {
    return this.http.get<ApiResponse<AcademyBadgeResponseDto[]>>(`${this.apiUrl}/${academyId}/badges`);
  }

  // ==================== Academy Dashboard Data ====================

  getAcademyMembers(academyId: number, pagination?: any): Observable<ApiResponse<PagedResponseDto<AcademyMemberResponseDto>>> {
    let params = new HttpParams();
    if (pagination) {
      const pageVal = pagination.pageNumber || pagination.page;
      if (pageVal) params = params.set('pageNumber', pageVal.toString());
      if (pagination.pageSize) params = params.set('pageSize', pagination.pageSize.toString());
    }
    return this.http.get<ApiResponse<PagedResponseDto<AcademyMemberResponseDto>>>(`${this.apiUrl}/${academyId}/members`, { params });
  }

  getAcademyAdmins(academyId: number, pagination?: any): Observable<ApiResponse<PagedResponseDto<AcademyAdminResponseDto>>> {
    let params = new HttpParams();
    if (pagination) {
      const pageVal = pagination.pageNumber || pagination.page;
      if (pageVal) params = params.set('pageNumber', pageVal.toString());
      if (pagination.pageSize) params = params.set('pageSize', pagination.pageSize.toString());
    }

    return this.http.get<ApiResponse<PagedResponseDto<AcademyAdminResponseDto>>>(`${this.apiUrl}/${academyId}/admins`, { params });
  }

  assignAdmin(academyId: number, adminId: number): Observable<ApiResponse<any>> {
    return this.http.post<ApiResponse<any>>(`${this.apiUrl}/${academyId}/admins/${adminId}`, {});
  }

  removeAdmin(academyId: number, adminId: number): Observable<ApiResponse<any>> {
    return this.http.delete<ApiResponse<any>>(`${this.apiUrl}/${academyId}/admins/${adminId}`);
  }

  // ==================== Admin Management ====================

  searchAdmins(academyId: number, name?: string): Observable<ApiResponse<any>> {
    let params = new HttpParams();
    if (name) params = params.set('name', name);
    return this.http.get<ApiResponse<any>>(`${this.apiUrl}/${academyId}/search-admins`, { params });
  }

  sendAdminJoinRequest(academyId: number, adminId: number): Observable<ApiResponse<any>> {
    return this.http.post<ApiResponse<any>>(`${this.apiUrl}/${academyId}/send-admin-join-request?adminId=${adminId}`, {});
  }

  getPendingAdminRequests(academyId: number): Observable<ApiResponse<any>> {
    return this.http.get<ApiResponse<any>>(`${this.apiUrl}/${academyId}/pending-admin-requests`);
  }

  cancelAdminJoinRequest(requestId: number): Observable<ApiResponse<any>> {
    return this.http.patch<ApiResponse<any>>(`${this.apiUrl}/admin-join-requests/${requestId}/cancel`, {});
  }

  getMyPendingAdminRequests(): Observable<ApiResponse<any>> {
    return this.http.get<ApiResponse<any>>(`${this.apiUrl}/admin-join-requests/my-requests`);
  }

  respondToAdminJoinRequest(requestId: number, dto: { status: number }): Observable<ApiResponse<any>> {
    return this.http.put<ApiResponse<any>>(`${this.apiUrl}/admin-join-requests/${requestId}/respond`, dto);
  }

  // ==================== Coach Management ====================

  searchCoaches(academyId: number, name?: string): Observable<ApiResponse<any>> {
    let params = new HttpParams();
    if (name) params = params.set('name', name);
    return this.http.get<ApiResponse<any>>(`${this.apiUrl}/${academyId}/search-coaches`, { params });
  }

  sendCoachJoinRequest(academyId: number, coachId: number): Observable<ApiResponse<any>> {
    return this.http.post<ApiResponse<any>>(`${this.apiUrl}/${academyId}/Send-coach-join-request?coachId=${coachId}`, {});
  }

  getPendingCoachRequests(academyId: number): Observable<ApiResponse<any>> {
    return this.http.get<ApiResponse<any>>(`${this.apiUrl}/${academyId}/Pending-coach-requests`);
  }

  cancelCoachJoinRequest(requestId: number): Observable<ApiResponse<any>> {
    return this.http.patch<ApiResponse<any>>(`${this.apiUrl}/coach-join-requests/${requestId}/cancel`, {});
  }

  removeCoach(academyId: number, coachId: number): Observable<ApiResponse<any>> {
    return this.http.delete<ApiResponse<any>>(`${this.apiUrl}/${academyId}/coaches/${coachId}`);
  }

  // ==================== Player Management ====================

  searchPlayers(academyId: number, name?: string): Observable<ApiResponse<any>> {
    let params = new HttpParams();
    if (name) params = params.set('name', name);
    return this.http.get<ApiResponse<any>>(`${this.apiUrl}/${academyId}/search-players`, { params });
  }

  sendPlayerJoinRequest(academyId: number, playerId: number): Observable<ApiResponse<any>> {
    return this.http.post<ApiResponse<any>>(`${this.apiUrl}/${academyId}/Send-player-join-request?playerId=${playerId}`, {});
  }

  getPendingPlayerRequests(academyId: number): Observable<ApiResponse<any>> {
    return this.http.get<ApiResponse<any>>(`${this.apiUrl}/${academyId}/Pending-player-requests`);
  }

  cancelPlayerJoinRequest(requestId: number): Observable<ApiResponse<any>> {
    return this.http.patch<ApiResponse<any>>(`${this.apiUrl}/player-join-requests/${requestId}/cancel`, {});
  }

  removePlayer(academyId: number, playerId: number): Observable<ApiResponse<any>> {
    return this.http.delete<ApiResponse<any>>(`${this.apiUrl}/${academyId}/players/${playerId}`);
  }

  updatePlayerSubscription(academyId: number, playerId: number, dto: UpdatePlayerSubscriptionDto): Observable<ApiResponse<any>> {
    return this.http.put<ApiResponse<any>>(`${this.apiUrl}/${academyId}/subscriptions/${playerId}`, dto);
  }

  // ==================== Teams & Age Groups ====================

  getLocations(academyId: number): Observable<ApiResponse<AcademyLocationResponseDto[]>> {
    return this.http.get<ApiResponse<AcademyLocationResponseDto[]>>(`${this.apiUrl}/${academyId}/locations`);
  }

  getAgeGroups(academyId: number): Observable<ApiResponse<AgeGroupResponseDto[]>> {
    return this.http.get<ApiResponse<AgeGroupResponseDto[]>>(`${this.apiUrl}/${academyId}/age-groups`);
  }

  getAgeGroupsByNames(names: string[]): Observable<ApiResponse<AgeGroupResponseDto[]>> {
    let params = new HttpParams();
    names.forEach(name => {
      params = params.append('names', name);
    });
    return this.http.get<ApiResponse<AgeGroupResponseDto[]>>(`${this.apiUrl}/age-groups`, { params });
  }

  createAgeGroup(academyId: number, dto: CreateAgeGroupDto): Observable<ApiResponse<AgeGroupResponseDto>> {
    return this.http.post<ApiResponse<AgeGroupResponseDto>>(`${this.apiUrl}/${academyId}/age-groups`, dto);
  }

  deleteAgeGroup(academyId: number, ageGroupId: number): Observable<ApiResponse<void>> {
    return this.http.delete<ApiResponse<void>>(`${this.apiUrl}/${academyId}/age-groups/${ageGroupId}`);
  }

  getTeams(academyId: number): Observable<ApiResponse<TeamResponseDto[]>> {
    return this.http.get<ApiResponse<TeamResponseDto[]>>(`${this.apiUrl}/${academyId}/teams`);
  }

  createTeam(academyId: number, dto: CreateTeamDto): Observable<ApiResponse<TeamResponseDto>> {
    return this.http.post<ApiResponse<TeamResponseDto>>(`${this.apiUrl}/${academyId}/teams`, dto);
  }

  // ==================== System Admin Academy Management ====================

  approveAcademy(dto: any): Observable<ApiResponse<any>> {
    return this.http.post<ApiResponse<any>>(`${this.apiUrl}/approve`, dto);
  }

  getAllAcademies(request?: any): Observable<ApiResponse<PagedResponseDto<AcademyResponseDto>>> {
    let params = new HttpParams();
    if (request) {
      const pageVal = request.page || request.pageNumber || request.Page || request.PageNumber;
      if (pageVal) params = params.set('Page', pageVal.toString());

      const pageSizeVal = request.pageSize || request.PageSize;
      if (pageSizeVal) params = params.set('PageSize', pageSizeVal.toString());

      const searchVal = request.searchQuery || request.SearchQuery || request.searchTerm || request.SearchTerm;
      if (searchVal) params = params.set('SearchQuery', searchVal.toString());

      Object.keys(request).forEach(key => {
        if (!['page', 'pageNumber', 'Page', 'PageNumber', 'pageSize', 'PageSize', 'searchQuery', 'SearchQuery', 'searchTerm', 'SearchTerm'].includes(key)) {
          if (request[key] !== undefined && request[key] !== null) {
            params = params.set(key, request[key].toString());
          }
        }
      });
    }
    return this.http.get<ApiResponse<PagedResponseDto<AcademyResponseDto>>>(`${this.apiUrl}`, { params });
  }

  updateAcademy(academyId: number, dto: any): Observable<ApiResponse<any>> {
    return this.http.put<ApiResponse<any>>(`${this.apiUrl}/${academyId}`, dto);
  }

  getPendingRequests(): Observable<ApiResponse<AcademyRequestResponseDto[]>> {
    return this.http.get<ApiResponse<AcademyRequestResponseDto[]>>(`${this.apiUrl}/requests/pending`);
  }

  rejectAcademyRequest(requestId: number, dto: any): Observable<ApiResponse<any>> {
    return this.http.put<ApiResponse<any>>(`${this.apiUrl}/requests/${requestId}/reject`, dto);
  }

  createBadge(academyId: number, dto: any): Observable<ApiResponse<AcademyBadgeResponseDto>> {
    return this.http.post<ApiResponse<AcademyBadgeResponseDto>>(`${this.apiUrl}/${academyId}/badges`, dto);
  }

  deleteBadge(academyId: number, badgeId: number): Observable<ApiResponse<any>> {
    return this.http.delete<ApiResponse<any>>(`${this.apiUrl}/${academyId}/badges/${badgeId}`);
  }

  // ==================== Locations ====================

  addLocation(academyId: number, dto: any): Observable<ApiResponse<AcademyLocationResponseDto>> {
    return this.http.post<ApiResponse<AcademyLocationResponseDto>>(`${this.apiUrl}/${academyId}/locations`, dto);
  }

  setMainLocation(academyId: number, locationId: number): Observable<ApiResponse<any>> {
    return this.http.put<ApiResponse<any>>(`${this.apiUrl}/${academyId}/locations/${locationId}/set-main`, {});
  }

  deleteLocation(academyId: number, locationId: number): Observable<ApiResponse<any>> {
    return this.http.delete<ApiResponse<any>>(`${this.apiUrl}/${academyId}/locations/${locationId}`);
  }

  // ==================== Team Management ====================

  assignCoachToTeam(teamId: number, coachId: number): Observable<ApiResponse<any>> {
    return this.http.post<ApiResponse<any>>(`${this.apiUrl}/teams/${teamId}/coaches/${coachId}`, {});
  }

  removeCoachFromTeam(teamId: number, coachId: number): Observable<ApiResponse<any>> {
    return this.http.delete<ApiResponse<any>>(`${this.apiUrl}/teams/${teamId}/coaches/${coachId}`);
  }

  assignPlayerToTeam(teamId: number, playerId: number): Observable<ApiResponse<any>> {
    return this.http.post<ApiResponse<any>>(`${this.apiUrl}/teams/${teamId}/players/${playerId}`, {});
  }

  removePlayerFromTeam(teamId: number, playerId: number): Observable<ApiResponse<any>> {
    return this.http.delete<ApiResponse<any>>(`${this.apiUrl}/teams/${teamId}/players/${playerId}`);
  }

  // ==================== My Join Requests (Player/Coach/Admin) ====================

  getMyPendingPlayerRequests(): Observable<ApiResponse<any>> {
    return this.http.get<ApiResponse<any>>(`${this.apiUrl}/player-join-requests/my-requests`);
  }

  respondToPlayerJoinRequest(requestId: number, dto: any): Observable<ApiResponse<any>> {
    return this.http.put<ApiResponse<any>>(`${this.apiUrl}/player-join-requests/${requestId}/respond`, dto);
  }

  getMyPendingCoachRequests(): Observable<ApiResponse<any>> {
    return this.http.get<ApiResponse<any>>(`${this.apiUrl}/coach-join-requests/my-requests`);
  }

  respondToCoachJoinRequest(requestId: number, dto: any): Observable<ApiResponse<any>> {
    return this.http.put<ApiResponse<any>>(`${this.apiUrl}/coach-join-requests/${requestId}/Respond`, dto);
  }

  // ==================== Announcements ====================

  sendAnnouncement(academyId: number, dto: any): Observable<ApiResponse<any>> {
    return this.http.post<ApiResponse<any>>(`${this.apiUrl}/${academyId}/announcements`, dto);
  }

  getAnnouncements(academyId: number, pagination?: any): Observable<ApiResponse<PagedResponseDto<any>>> {
    let params = new HttpParams();
    if (pagination) {
      const pageVal = pagination.pageNumber || pagination.page;
      if (pageVal) params = params.set('pageNumber', pageVal.toString());
      if (pagination.pageSize) params = params.set('pageSize', pagination.pageSize.toString());
    }
    return this.http.get<ApiResponse<PagedResponseDto<any>>>(`${this.apiUrl}/${academyId}/announcements`, { params });
  }

  // ==================== Analytics ====================

  getCoachPerformance(academyId: number): Observable<ApiResponse<any>> {
    return this.http.get<ApiResponse<any>>(`${this.apiUrl}/${academyId}/coaches`);
  }

  getSubscriptionStatus(academyId: number): Observable<ApiResponse<any>> {
    return this.http.get<ApiResponse<any>>(`${this.apiUrl}/${academyId}/subscriptions`);
  }


  getAcademies(page = 1, pageSize = 100): Observable<any> {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);
    return this.http.get<any>(this.apiUrl, { params });
  }

  searchAcademies(name: string): Observable<ApiResponse<any[]>> {
    let params = new HttpParams().set('name', name);
    return this.http.get<ApiResponse<any[]>>(`${this.apiUrl}/search`, { params });
  }

  getAcademyTeamsSummary(academyId: number): Observable<ApiResponse<any[]>> {
    return this.http.get<ApiResponse<any[]>>(`${this.apiUrl}/${academyId}/teams/summary`);
  }

  /** POST api/Academy/search-ai-chat */
  academySearchAiChatBot(message: string, sessionId?: string): Observable<string> {
    return this.http
      .post<ApiResponse<string>>(`${this.apiUrl}/search-ai-chat`, { message, sessionId })
      .pipe(map((res) => res.data ?? ''));
  }
}
