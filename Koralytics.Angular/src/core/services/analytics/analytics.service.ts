import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../../interfaces/api-response.model';

export interface AiSearchResponse {
  answer: string;
}

@Injectable({
  providedIn: 'root'
})
export class AnalyticsService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/api/Analytics`;

  /**
   * Sends a natural-language query to the AI Player Search endpoint.
   * The backend forwards it to the Langflow pipeline and returns a human-readable answer.
   */
  aiPlayerSearch(query: string): Observable<ApiResponse<AiSearchResponse>> {
    return this.http.post<ApiResponse<AiSearchResponse>>(
      `${this.apiUrl}/ai-search`,
      { query }
    );
  }
}
