import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { CreateTournamentDto, TournamentStatus } from '../../interfaces/tournament.models';

@Injectable({
  providedIn: 'root'
})
export class TournamentService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/api/Tournament`;

  getTournaments(): Observable<any> {
    return this.http.get<any>(this.apiUrl);
  }

  getTournamentById(id: number): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/${id}`);
  }

  createTournament(dto: CreateTournamentDto): Observable<any> {
    return this.http.post<any>(this.apiUrl, dto);
  }

  inviteAcademy(tournamentId: number, academyId: number): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/${tournamentId}/invite/${academyId}`, {});
  }

  acceptInvitation(tournamentId: number, academyId: number, teamId?: number): Observable<any> {
    const url = teamId 
      ? `${this.apiUrl}/${tournamentId}/accept/${academyId}?teamId=${teamId}`
      : `${this.apiUrl}/${tournamentId}/accept/${academyId}`;
    return this.http.put<any>(url, {});
  }

  registerSquad(tournamentId: number, teamId: number, playerIds: number[]): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/${tournamentId}/squad/${teamId}`, playerIds);
  }

  getRegisteredPlayerIds(tournamentId: number, teamId: number): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/${tournamentId}/squad/${teamId}/players`);
  }

  generateSeeding(tournamentId: number): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/${tournamentId}/seeding`, {});
  }

  generateDraw(tournamentId: number): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/${tournamentId}/draw`, {});
  }

  advanceKnockout(tournamentId: number, roundId: number): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/${tournamentId}/rounds/${roundId}/advance`, {});
  }

  updateStatus(tournamentId: number, status: TournamentStatus): Observable<any> {
    return this.http.put<any>(
      `${this.apiUrl}/${tournamentId}/status`,
      JSON.stringify(status),
      { headers: { 'Content-Type': 'application/json' } }
    );
  }

  getBracket(tournamentId: number): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/${tournamentId}/bracket`);
  }

  getHallOfFame(tournamentId: number): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/${tournamentId}/hall-of-fame`);
  }

  completeTournament(tournamentId: number): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/${tournamentId}/complete`, {});
  }

  getTournamentReport(tournamentId: number): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/${tournamentId}/report`);
  }

  regenerateTournamentReport(tournamentId: number): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/${tournamentId}/regenerate-report`, {});
  }

  simulateTournament(tournamentId: number): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/${tournamentId}/simulate`, {});
  }

  getTournamentTeams(tournamentId: number): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/${tournamentId}/teams`);
  }

  getTournamentInvitationsForAcademy(academyId: number): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/academies/${academyId}/invitations`);
  }

  getFixtureById(fixtureId: number): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/fixture/${fixtureId}`);
  }

  updateFixtureResult(fixtureId: number, homeScore: number, awayScore: number): Observable<any> {
    return this.http.put<any>(`${this.apiUrl}/fixture/${fixtureId}/result`, { homeScore, awayScore });
  }

  generateKnockoutFromGroups(tournamentId: number): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/${tournamentId}/generate-knockout`, {});
  }

  updateFixtureStats(fixtureId: number, data: any): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/fixture/${fixtureId}/stats`, data);
  }
}