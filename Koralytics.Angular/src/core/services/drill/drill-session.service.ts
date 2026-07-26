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
    private readonly baseUrl = `${environment.apiUrl}/api/drills`;
    private readonly apiUrl = `${environment.apiUrl}/api/drills/sessions`;

    // ==========================================
    // 1. SESSION CRUD ENDPOINTS
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
    // 2. DRILLS & ATTENDANCE INSIDE SESSION
    // ==========================================

    addDrillToSession(sessionId: number, dto: AddSessionDrillDto): Observable<DrillDto> {
        return this.http.post<DrillDto>(`${this.apiUrl}/${sessionId}/drills`, dto);
    }

    removeDrillFromSession(sessionId: number, drillId: number): Observable<void> {
        return this.http.delete<void>(`${this.apiUrl}/${sessionId}/drills/${drillId}`);
    }

    // Fetch session attendance sheet
    getSessionAttendance(sessionId: number): Observable<any[]> {
        return this.http.get<any[]>(`${this.apiUrl}/${sessionId}/attendance`);
    }

    // Update session attendance sheet
    updateAttendance(sessionId: number, dto: any): Observable<any> {
        return this.http.put(`${this.apiUrl}/${sessionId}/attendance`, dto);
    }

    // Fetch drill results
    getDrillResults(sessionId: number, drillId: number): Observable<any[]> {
        return this.http.get<any[]>(`${this.apiUrl}/${sessionId}/drills/${drillId}/results`);
    }

    // Submit performance results for players
    submitDrillResults(sessionId: number, drillId: number, dto: any): Observable<any> {
        return this.http.post(`${this.apiUrl}/${sessionId}/drills/${drillId}/results`, dto);
    }

    // ==========================================
    // 3. CATEGORIES & ANALYTICS ENDPOINTS
    // ==========================================

    // Fetch all drill categories for filter dropdowns
    getCategories(): Observable<any[]> {
        return this.http.get<any[]>(`${this.baseUrl}/categories`);
    }

    // Fetch single player progression over time for a specific category
    getPlayerProgression(playerId: number, categoryId: number): Observable<any> {
        return this.http.get<any>(`${this.baseUrl}/players/${playerId}/progression/category/${categoryId}`);
    }

    // Fetch squad weak categories summary
    getSquadWeakCategories(teamId: number): Observable<any[]> {
        return this.http.get<any[]>(`${this.baseUrl}/analytics/teams/${teamId}/weak-categories`);
    }

    // Calculate and detect coach bias / trust index
    getCoachBiasReport(coachId: number): Observable<any> {
        return this.http.post<any>(`${this.baseUrl}/coaches/${coachId}/bias/calculate`, {});
    }
}