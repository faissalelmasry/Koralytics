import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { PlayerHighlightDto } from '../../../core/interfaces/highlight.interfaces';

@Injectable({
  providedIn: 'root',
})
export class PlayerHighlightService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/api/Player`;

  /** POST /api/Player/{playerId}/highlights (multipart/form-data) */
  uploadHighlight(
    playerId: number,
    academyId: number,
    file: File,
    title?: string
  ): Observable<PlayerHighlightDto> {
    const formData = new FormData();
    formData.append('file', file);
    formData.append('academyId', academyId.toString());
    if (title) {
      formData.append('title', title);
    }
    return this.http.post<PlayerHighlightDto>(
      `${this.apiUrl}/${playerId}/highlights`,
      formData
    );
  }

  /** DELETE /api/Player/{playerId}/highlights/{highlightId} */
  deleteHighlight(playerId: number, highlightId: number): Observable<void> {
    return this.http.delete<void>(
      `${this.apiUrl}/${playerId}/highlights/${highlightId}`
    );
  }

  /** PATCH /api/Player/{playerId}/highlights/{highlightId}/pin */
  pinHighlight(playerId: number, highlightId: number): Observable<void> {
    return this.http.patch<void>(
      `${this.apiUrl}/${playerId}/highlights/${highlightId}/pin`,
      {}
    );
  }

  /** GET /api/Player/{playerId}/highlights */
  getHighlights(playerId: number): Observable<PlayerHighlightDto[]> {
    return this.http.get<PlayerHighlightDto[]>(
      `${this.apiUrl}/${playerId}/highlights`
    );
  }
}
