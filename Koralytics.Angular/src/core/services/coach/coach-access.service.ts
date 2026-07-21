import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  GrantTempAccessDto,
  TempAccessDto,
} from '../../../core/interfaces/coach.interfaces';

@Injectable({
  providedIn: 'root',
})
export class CoachAccessService {
  private http = inject(HttpClient);
  private baseUrl = `${environment.apiUrl}/api/Coach`;

  /** POST /api/Coach/access/grant */
  grantTempAccess(dto: GrantTempAccessDto): Observable<TempAccessDto> {
    return this.http.post<TempAccessDto>(`${this.baseUrl}/access/grant`, dto);
  }

  /** PATCH /api/Coach/access/{accessId}/revoke */
  revokeTempAccess(accessId: number): Observable<TempAccessDto> {
    return this.http.patch<TempAccessDto>(
      `${this.baseUrl}/access/${accessId}/revoke`,
      {}
    );
  }

  /** GET /api/Coach/access/active */
  getActiveGrants(): Observable<TempAccessDto[]> {
    return this.http.get<TempAccessDto[]>(`${this.baseUrl}/access/active`);
  }
}
