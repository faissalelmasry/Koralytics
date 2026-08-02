import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../../interfaces/api-response.model';
import { CreateAcademyDto } from '../../interfaces/academy.models';

export interface UserSummaryDto {
  id: number;
  firstName: string;
  lastName: string;
  fullName: string;
  email: string;
  userName: string;
  roles: string[];
  createdAt: string;
  isDeleted: boolean;
  profileImageUrl?: string;
}

export interface UserDetailDto extends UserSummaryDto {
  emailConfirmed: boolean;
  googleId?: string;
  academyId?: number;
  academyName?: string;
}

export interface UserListResponseDto {
  items: UserSummaryDto[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
}

@Injectable({
  providedIn: 'root'
})
export class SystemAdminService {
  private http = inject(HttpClient);
  private usersUrl = `${environment.apiUrl}/api/SystemAdmin/users`;
  private academyUrl = `${environment.apiUrl}/api/Academy`;

  // ==================== User Management ====================

  getUsers(params: {
    page?: number;
    pageNumber?: number;
    pageSize?: number;
    searchTerm?: string;
    roleFilter?: string;
    isDeletedFilter?: boolean;
  } = {}): Observable<ApiResponse<UserListResponseDto>> {
    let httpParams = new HttpParams();
    const p = params.pageNumber || params.page;
    if (p) httpParams = httpParams.set('PageNumber', p);
    if (params.pageSize) httpParams = httpParams.set('PageSize', params.pageSize);
    if (params.searchTerm) httpParams = httpParams.set('SearchTerm', params.searchTerm);
    if (params.roleFilter) httpParams = httpParams.set('RoleFilter', params.roleFilter);
    if (params.isDeletedFilter !== undefined) httpParams = httpParams.set('IsDeletedFilter', params.isDeletedFilter);

    return this.http.get<ApiResponse<UserListResponseDto>>(this.usersUrl, { params: httpParams });
  }

  getUserById(id: number): Observable<ApiResponse<UserDetailDto>> {
    return this.http.get<ApiResponse<UserDetailDto>>(`${this.usersUrl}/${id}`);
  }

  updateUserRoles(id: number, roles: string[]): Observable<ApiResponse<UserSummaryDto>> {
    return this.http.put<ApiResponse<UserSummaryDto>>(`${this.usersUrl}/${id}/roles`, { roles });
  }

  toggleUserStatus(id: number, isDeleted: boolean): Observable<ApiResponse<any>> {
    return this.http.put<ApiResponse<any>>(`${this.usersUrl}/${id}/status`, { isDeleted });
  }

  // ==================== Academy Requests ====================

  getPendingAcademyRequests(): Observable<ApiResponse<any[]>> {
    return this.http.get<ApiResponse<any[]>>(`${this.academyUrl}/requests/pending`);
  }

  approveAcademyRequest(dto: CreateAcademyDto | any): Observable<ApiResponse<any>> {
    return this.http.post<ApiResponse<any>>(`${this.academyUrl}/approve`, dto);
  }

  rejectAcademyRequest(requestId: number, reason: string): Observable<ApiResponse<any>> {
    return this.http.put<ApiResponse<any>>(`${this.academyUrl}/requests/${requestId}/reject`, { reason });
  }

  // ==================== Active Academies & Details ====================

  getAllAcademies(params: {
    page?: number;
    pageNumber?: number;
    pageSize?: number;
    searchQuery?: string;
  } = {}): Observable<ApiResponse<any>> {
    let httpParams = new HttpParams();
    const p = params.page || params.pageNumber;
    if (p) httpParams = httpParams.set('Page', p);
    if (params.pageSize) httpParams = httpParams.set('PageSize', params.pageSize);
    if (params.searchQuery) httpParams = httpParams.set('SearchQuery', params.searchQuery);

    return this.http.get<ApiResponse<any>>(this.academyUrl, { params: httpParams });
  }

  getAcademyDetails(academyId: number): Observable<ApiResponse<any>> {
    return this.http.get<ApiResponse<any>>(`${this.academyUrl}/${academyId}`);
  }

  updateAcademyStatus(academyId: number, status: string): Observable<ApiResponse<any>> {
    return this.http.put<ApiResponse<any>>(`${this.academyUrl}/${academyId}/status`, { status });
  }

  getAcademyMembers(academyId: number, params: { page?: number; pageNumber?: number; pageSize?: number } = {}): Observable<ApiResponse<any>> {
    let httpParams = new HttpParams();
    const p = params.pageNumber || params.page;
    if (p) httpParams = httpParams.set('pageNumber', p);
    if (params.pageSize) httpParams = httpParams.set('pageSize', params.pageSize);

    return this.http.get<ApiResponse<any>>(`${this.academyUrl}/${academyId}/members`, { params: httpParams });
  }

  getAcademyAdmins(academyId: number, params: { page?: number; pageNumber?: number; pageSize?: number } = {}): Observable<ApiResponse<any>> {
    let httpParams = new HttpParams();
    const p = params.pageNumber || params.page;
    if (p) httpParams = httpParams.set('pageNumber', p);
    if (params.pageSize) httpParams = httpParams.set('pageSize', params.pageSize);

    return this.http.get<ApiResponse<any>>(`${this.academyUrl}/${academyId}/admins`, { params: httpParams });
  }

  getAcademyLocations(academyId: number): Observable<ApiResponse<any>> {
    return this.http.get<ApiResponse<any>>(`${this.academyUrl}/${academyId}/locations`);
  }

  getAcademyBadges(academyId: number): Observable<ApiResponse<any>> {
    return this.http.get<ApiResponse<any>>(`${this.academyUrl}/${academyId}/badges`);
  }

  createBadge(academyId: number, badge: any): Observable<ApiResponse<any>> {
    return this.http.post<ApiResponse<any>>(`${this.academyUrl}/${academyId}/badges`, badge);
  }

  deleteBadge(academyId: number, badgeId: number): Observable<ApiResponse<any>> {
    return this.http.delete<ApiResponse<any>>(`${this.academyUrl}/${academyId}/badges/${badgeId}`);
  }

  getSubscriptionStatus(academyId: number): Observable<ApiResponse<any>> {
    return this.http.get<ApiResponse<any>>(`${this.academyUrl}/${academyId}/subscriptions`);
  }
}
