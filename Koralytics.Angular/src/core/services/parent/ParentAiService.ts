import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, Subject } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface ParentChatRequest {
  sessionId: string;
  message: string;
  requestedPlayerId?: number;
}

export interface ParentChatResponse {
  sessionId: string;
  question: string;
  answer: string;
  usedRAG: boolean;
  usedSQL: boolean;
  responseTime: number;
}

export interface ParentChatStreamChunk {
  eventType: 'token' | 'meta' | 'error';
  text?: string;
  meta?: ParentChatResponse;
}

@Injectable({
  providedIn: 'root'
})
export class ParentAiService {
  private readonly http = inject(HttpClient);
  private readonly BaseUrl = `${environment.apiUrl}/api`;

  private readonly apiUrl = `${this.BaseUrl}/koralytics/parent-assistant/chat`;
  private readonly streamUrl = `${this.BaseUrl}/koralytics/parent-assistant/chat/stream`;

  /**
   * The auth token lives in an `access_token` cookie (not localStorage), so
   * the browser attaches it automatically as long as `withCredentials` is
   * set — no manual Authorization header needed, and no access to the
   * cookie's value from JS is required (works even if it's httpOnly).
   */
  sendMessage(request: ParentChatRequest): Observable<ParentChatResponse> {
    return this.http.post<ParentChatResponse>(this.apiUrl, request, {
      withCredentials: true
    });
  }

  /**
   * Streaming call via fetch(), since HttpClient can't incrementally read
   * an SSE body. `credentials: 'include'` is the fetch equivalent of
   * `withCredentials: true` — it sends the access_token cookie the same way.
   */
  streamMessage(request: ParentChatRequest): Observable<ParentChatStreamChunk> {
    const subject = new Subject<ParentChatStreamChunk>();

    (async () => {
      try {
        const response = await fetch(this.streamUrl, {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json'
          },
          credentials: 'include', // sends the access_token cookie
          body: JSON.stringify(request)
        });

        if (!response.ok || !response.body) {
          subject.error(new Error(`Stream request failed with status ${response.status}`));
          return;
        }

        const reader = response.body.getReader();
        const decoder = new TextDecoder('utf-8');
        let buffer = '';

        while (true) {
          const { value, done } = await reader.read();
          if (done) break;

          buffer += decoder.decode(value, { stream: true });

          const records = buffer.split('\n\n');
          buffer = records.pop() ?? '';

          for (const record of records) {
            const dataLine = record.split('\n').find(line => line.startsWith('data:'));
            if (!dataLine) continue;

            const jsonPart = dataLine.slice('data:'.length).trim();
            if (!jsonPart) continue;

            try {
              const parsed: ParentChatStreamChunk = JSON.parse(jsonPart);
              subject.next(parsed);
            } catch {
              // Skip malformed/heartbeat lines.
            }
          }
        }

        subject.complete();
      } catch (err) {
        subject.error(err);
      }
    })();

    return subject.asObservable();
  }
}