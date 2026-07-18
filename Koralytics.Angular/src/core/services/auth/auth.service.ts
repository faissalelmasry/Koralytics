import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, BehaviorSubject, tap, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { TokenStorageService } from './token-storage.service';
import { User } from '../../interfaces/user.model';
import { ApiResponse } from '../../interfaces/api-response.model';
import {
  LoginRequest,
  AuthResponseDto,
  AuthResultDto,
  OAuthLoginRequest,
  OAuthLoginResult,
  ForgotPasswordRequest,
  ResetPasswordRequest,
  ChangePasswordRequest,
  RegisterPlayerRequest,
  RegisterCoachRequest,
  RegisterScouterRequest,
  RegisterParentRequest,
  RegisterAcademyAdminRequest,
  CompleteProfileAsPlayer,
  CompleteProfileAsCoach,
  CompleteProfileAsScouter,
  CompleteProfileAsParent,
  CompleteProfileBase
} from '../../interfaces/auth.models';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private http = inject(HttpClient);
  private tokenStorage = inject(TokenStorageService);
  private apiUrl = `${environment.apiUrl}/api`;

  private currentUserSubject = new BehaviorSubject<User | null>(this.tokenStorage.getUser());
  public currentUser$ = this.currentUserSubject.asObservable();
  
  private isAuthenticatedSubject = new BehaviorSubject<boolean>(!!this.tokenStorage.getAccessToken());
  public isAuthenticated$ = this.isAuthenticatedSubject.asObservable();

  constructor() {}

  // ==================== Auth Flow ====================

  login(request: LoginRequest, rememberMe: boolean = false): Observable<ApiResponse<AuthResponseDto>> {
    return this.http.post<ApiResponse<AuthResponseDto>>(`${this.apiUrl}/Auth/login`, request).pipe(
      tap(response => {
        if (response.isSuccess && response.data) {
          // Cookies handle tokens in backend, but backend also returns them in the data
          // We will store them in tokenStorage
          // The response.data has accessToken and refreshToken
          this.tokenStorage.saveTokens(response.data.accessToken, response.data.refreshToken, rememberMe);
          this.saveUserFromResponse(response.data, rememberMe);
        }
      })
    );
  }

  oauthLogin(request: OAuthLoginRequest, rememberMe: boolean = false): Observable<ApiResponse<OAuthLoginResult>> {
    return this.http.post<ApiResponse<OAuthLoginResult>>(`${this.apiUrl}/Auth/oauth`, request).pipe(
      tap(response => {
        if (response.isSuccess && response.data && !response.data.requiresProfileCompletion) {
          if (response.data.authResult) {
            this.tokenStorage.saveTokens(response.data.authResult.tokens.accessToken, response.data.authResult.tokens.refreshToken, rememberMe);
            this.saveUserFromResponse(response.data.authResult.user, rememberMe);
          }
        }
      })
    );
  }

  logout(): Observable<any> {
    const refreshToken = this.tokenStorage.getRefreshToken();
    // We send request even if refresh token is missing (API can use cookie)
    return this.http.post(`${this.apiUrl}/Auth/logout`, { refreshToken }).pipe(
      tap(() => {
        this.clearLocalState();
      }),
      catchError(err => {
        this.clearLocalState();
        return throwError(() => err);
      })
    );
  }

  logoutAll(): Observable<any> {
    return this.http.post(`${this.apiUrl}/Auth/logout-all`, {}).pipe(
      tap(() => {
        this.clearLocalState();
      })
    );
  }

  refreshToken(): Observable<ApiResponse<AuthResponseDto>> {
    const refreshToken = this.tokenStorage.getRefreshToken();
    return this.http.post<ApiResponse<AuthResponseDto>>(`${this.apiUrl}/Auth/refresh`, { refreshToken }).pipe(
      tap(response => {
        if (response.isSuccess && response.data) {
          // Note: in login we might have chosen 'rememberMe' and thus used localStorage.
          // In refresh we just update whatever storage is currently in use.
          const isLocalStorage = this.tokenStorage.isRememberMe();
          this.tokenStorage.saveTokens(response.data.accessToken, response.data.refreshToken, isLocalStorage);
          this.saveUserFromResponse(response.data, isLocalStorage);
        }
      })
    );
  }

  getCurrentUser(): Observable<ApiResponse<any>> {
    return this.http.get<ApiResponse<any>>(`${this.apiUrl}/Auth/me`);
  }

  // ==================== Registration ====================

  registerPlayer(request: RegisterPlayerRequest): Observable<ApiResponse<AuthResponseDto>> {
    return this.handleRegistration(`${this.apiUrl}/Register/player`, request);
  }

  registerCoach(request: RegisterCoachRequest): Observable<ApiResponse<AuthResponseDto>> {
    return this.handleRegistration(`${this.apiUrl}/Register/coach`, request);
  }

  registerScouter(request: RegisterScouterRequest): Observable<ApiResponse<AuthResponseDto>> {
    return this.handleRegistration(`${this.apiUrl}/Register/scouter`, request);
  }

  registerParent(request: RegisterParentRequest): Observable<ApiResponse<AuthResponseDto>> {
    return this.handleRegistration(`${this.apiUrl}/Register/parent`, request);
  }

  registerAcademyAdmin(request: RegisterAcademyAdminRequest): Observable<ApiResponse<AuthResponseDto>> {
    return this.handleRegistration(`${this.apiUrl}/Register/academy-admin`, request);
  }

  private handleRegistration(url: string, request: any): Observable<ApiResponse<AuthResponseDto>> {
    return this.http.post<ApiResponse<AuthResponseDto>>(url, request).pipe(
      tap(response => {
        if (response.isSuccess && response.data) {
          // Default to sessionStorage for new registrations
          this.tokenStorage.saveTokens(response.data.accessToken, response.data.refreshToken, false);
          this.saveUserFromResponse(response.data, false);
        }
      })
    );
  }

  // ==================== Email Confirmation ====================

  sendEmailConfirmation(userId: number): Observable<ApiResponse<any>> {
    return this.http.post<ApiResponse<any>>(`${this.apiUrl}/Register/send-email-confirmation`, { userId });
  }

  confirmEmail(userId: number, token: string): Observable<ApiResponse<any>> {
    return this.http.post<ApiResponse<any>>(`${this.apiUrl}/Register/confirm-email`, { userId, token });
  }

  isEmailConfirmed(userId: number): Observable<ApiResponse<{ isConfirmed: boolean }>> {
    return this.http.get<ApiResponse<{ isConfirmed: boolean }>>(`${this.apiUrl}/Register/${userId}/is-email-confirmed`);
  }

  // ==================== Password Management ====================

  forgotPassword(email: string): Observable<ApiResponse<any>> {
    return this.http.post<ApiResponse<any>>(`${this.apiUrl}/Auth/forgot-password`, { email });
  }

  resetPassword(request: ResetPasswordRequest): Observable<ApiResponse<any>> {
    return this.http.post<ApiResponse<any>>(`${this.apiUrl}/Auth/reset-password`, request);
  }

  changePassword(request: ChangePasswordRequest): Observable<any> {
    return this.http.post(`${this.apiUrl}/Auth/change-password`, request).pipe(
      tap(() => {
        this.clearLocalState();
      })
    );
  }

  // ==================== Profile Completion ====================

  completeProfileAsPlayer(request: CompleteProfileAsPlayer): Observable<ApiResponse<AuthResponseDto>> {
    return this.handleProfileCompletion(`${this.apiUrl}/Auth/complete-profile/player`, request);
  }

  completeProfileAsCoach(request: CompleteProfileAsCoach): Observable<ApiResponse<AuthResponseDto>> {
    return this.handleProfileCompletion(`${this.apiUrl}/Auth/complete-profile/coach`, request);
  }

  completeProfileAsScouter(request: CompleteProfileAsScouter): Observable<ApiResponse<AuthResponseDto>> {
    return this.handleProfileCompletion(`${this.apiUrl}/Auth/complete-profile/scouter`, request);
  }

  completeProfileAsParent(request: CompleteProfileAsParent): Observable<ApiResponse<AuthResponseDto>> {
    return this.handleProfileCompletion(`${this.apiUrl}/Auth/complete-profile/parent`, request);
  }

  completeProfileAsAcademyAdmin(request: CompleteProfileBase): Observable<ApiResponse<AuthResponseDto>> {
    return this.handleProfileCompletion(`${this.apiUrl}/Auth/complete-profile/academy-admin`, request);
  }

  private handleProfileCompletion(url: string, request: any): Observable<ApiResponse<AuthResponseDto>> {
    return this.http.post<ApiResponse<AuthResponseDto>>(url, request).pipe(
      tap(response => {
        if (response.isSuccess && response.data) {
          const isLocalStorage = !!localStorage.getItem('koralytics_access_token');
          this.tokenStorage.saveTokens(response.data.accessToken, response.data.refreshToken, isLocalStorage);
          this.saveUserFromResponse(response.data, isLocalStorage);
        }
      })
    );
  }

  // ==================== Helpers ====================

  private saveUserFromResponse(data: AuthResponseDto, rememberMe: boolean) {
    const user: User = {
      userId: data.userId,
      email: data.email,
      userName: data.userName,
      fullName: data.fullName,
      roles: data.roles
    };
    this.tokenStorage.saveUser(user, rememberMe);
    this.currentUserSubject.next(user);
    this.isAuthenticatedSubject.next(true);
  }

  public clearLocalState() {
    this.tokenStorage.clear();
    this.currentUserSubject.next(null);
    this.isAuthenticatedSubject.next(false);
  }

  public isLoggedIn(): boolean {
    return !!this.tokenStorage.getAccessToken();
  }

  public getUserRoles(): string[] {
    const user = this.currentUserSubject.value;
    return user ? user.roles : [];
  }
}
