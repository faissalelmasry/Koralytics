import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
    DrillSessionDto,
    DrillSessionDetailsDto,
    CreateDrillSessionDto,
    UpdateDrillSessionDto,
    SessionFilterDto,
    AddSessionDrillDto,
    DrillDto
} from '../../interfaces/drill-session.model';
import { environment } from '../../../environments/environment';
@Injectable({
    providedIn: 'root'
})
export class DrillSessionService {
    private http = inject(HttpClient);
    private readonly apiUrl = `${environment.apiUrl}/api/drills/sessions`;
    // ==========================================
    // SESSION CRUD ENDPOINTS
    // ==========================================

    getCoachSessions(filter: SessionFilterDto): Observable<DrillSessionDto[]> {
        let params = new HttpParams()
            .set('pageNumber', filter.pageNumber.toString())
            .set('pageSize', filter.pageSize.toString());

        if (filter.teamId) {
            params = params.set('teamId', filter.teamId.toString());
        }
        if (filter.status !== null && filter.status !== undefined) {
            params = params.set('status', filter.status.toString());
        }
        if (filter.fromDate) {
            params = params.set('fromDate', filter.fromDate);
        }
        if (filter.toDate) {
            params = params.set('toDate', filter.toDate);
        }

        return this.http.get<DrillSessionDto[]>(this.apiUrl, { params });
    }

    getSessionById(sessionId: number): Observable<DrillSessionDetailsDto> {
        return this.http.get<DrillSessionDetailsDto>(`${this.apiUrl}/${sessionId}`);
    }

    createSession(dto: CreateDrillSessionDto): Observable<DrillSessionDto> {
        return this.http.post<DrillSessionDto>(this.apiUrl, dto);
    }

    updateSession(sessionId: number, dto: UpdateDrillSessionDto): Observable<DrillSessionDto> {
        return this.http.put<DrillSessionDto>(`${this.apiUrl}/${sessionId}`, dto);
    }

    deleteSession(sessionId: number): Observable<void> {
        return this.http.delete<void>(`${this.apiUrl}/${sessionId}`);
    }

    completeSession(sessionId: number): Observable<{ message: string }> {
        return this.http.patch<{ message: string }>(`${this.apiUrl}/${sessionId}/complete`, {});
    }

    // ==========================================
    // DRILLS INSIDE A SESSION (The Planner)
    // ==========================================

    addDrillToSession(sessionId: number, dto: AddSessionDrillDto): Observable<DrillDto> {
        return this.http.post<DrillDto>(`${this.apiUrl}/${sessionId}/drills`, dto);
    }

    removeDrillFromSession(sessionId: number, drillId: number): Observable<void> {
        return this.http.delete<void>(`${this.apiUrl}/${sessionId}/drills/${drillId}`);
    }
}