import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../../interfaces/api-response.model';
import { BaseUserProfileResponse, UpdateProfileRequest } from '../../models/profile/profile.models';

@Injectable({
  providedIn: 'root'
})
export class ProfileService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/api/Profile`;

  getMyProfile(): Observable<ApiResponse<BaseUserProfileResponse>> {
    return this.http.get<ApiResponse<BaseUserProfileResponse>>(`${this.apiUrl}/me`);
  }

  updateMyProfile(dto: UpdateProfileRequest): Observable<ApiResponse<BaseUserProfileResponse>> {
    return this.http.put<ApiResponse<BaseUserProfileResponse>>(`${this.apiUrl}/me`, dto);
  }

  updateProfileImage(image: File): Observable<ApiResponse<string>> {
    const formData = new FormData();
    formData.append('image', image);
    return this.http.patch<ApiResponse<string>>(`${this.apiUrl}/me/image`, formData);
  }
}
