import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  CoachNoteDto,
  WriteNoteDto,
  PagedResult,
} from '../../../core/interfaces/coach.interfaces';

@Injectable({
  providedIn: 'root',
})
export class CoachNoteService {
  private http = inject(HttpClient);
  private baseUrl = `${environment.apiUrl}/api/Coach`;

  /** POST /api/Coach/notes */
  writeNote(dto: WriteNoteDto): Observable<CoachNoteDto> {
    return this.http.post<CoachNoteDto>(`${this.baseUrl}/notes`, dto);
  }

  /** GET /api/Coach/players/{playerId}/notes?page=&pageSize= */
  getPlayerNotes(
    playerId: number,
    page: number = 1,
    pageSize: number = 20
  ): Observable<PagedResult<CoachNoteDto>> {
    const params = new HttpParams()
      .set('page', page)
      .set('pageSize', pageSize);
    return this.http.get<PagedResult<CoachNoteDto>>(
      `${this.baseUrl}/players/${playerId}/notes`,
      { params }
    );
  }
}
