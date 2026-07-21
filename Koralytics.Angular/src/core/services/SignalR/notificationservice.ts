import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { CachedNotification } from '../../interfaces/CachedNotification';
import { environment } from '../../../environments/environment';


@Injectable({
  providedIn: 'root',
})
export class NotificationService {
  private http = inject(HttpClient);
  private baseUrl = `${environment.apiUrl}/api/Notification`;

  getMyNotifications(skip: number = 0, take: number = 50): Observable<CachedNotification[]> {
    return this.http.get<CachedNotification[]>(`${this.baseUrl}?skip=${skip}&take=${take}`);
  }

  markAsRead(notificationId: string): Observable<void> {
    return this.http.patch<void>(`${this.baseUrl}/${notificationId}/read`, {});
  }

  purgeExpired(): Observable<{ message: string }> {
    return this.http.delete<{ message: string }>(`${this.baseUrl}/expired`);
  }

  /**
   * Triggers a milestone notification for a player.
   * Matches: POST api/Notification/players/{playerId}/milestone
   * NOTE: this endpoint is restricted to Coach/Admin roles on the backend
   */
  notifyPlayerMilestone(playerId: number, achievementType: string): Observable<{ message: string }> {
    const params = new URLSearchParams({ achievementType });
    return this.http.post<{ message: string }>(
      `${this.baseUrl}/players/${playerId}/milestone?${params.toString()}`,
      {}
    );
  }

  /**
   * Triggers a parent-alert notification for a player's linked guardians.
   * Matches: POST api/Notification/players/{playerId}/parent-alert
   */
  notifyPlayerParents(playerId: number, eventType: string): Observable<{ message: string }> {
    const params = new URLSearchParams({ eventType });
    return this.http.post<{ message: string }>(
      `${this.baseUrl}/players/${playerId}/parent-alert?${params.toString()}`,
      {}
    );
  }

  /**
   * Triggers a subscription-grace-period alert to both the player and their
   * linked parents.
   * Matches: POST api/Notification/players/{playerId}/academies/{academyId}/subscription-grace
   */
  notifySubscriptionGrace(playerId: number, academyId: number): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(
      `${this.baseUrl}/players/${playerId}/academies/${academyId}/subscription-grace`,
      {}
    );
  }

  /**
   * Triggers a scouter-alert notification to everyone following a player
   * (e.g. new highlight uploaded, MOTM awarded).
   * Matches: POST api/Notification/players/{playerId}/scouter-alerts
   */
  notifyScouterFollowers(playerId: number, eventType: string): Observable<{ message: string }> {
    const params = new URLSearchParams({ eventType });
    return this.http.post<{ message: string }>(
      `${this.baseUrl}/players/${playerId}/scouter-alerts?${params.toString()}`,
      {}
    );
  }
}
//TODO: fix scouter controller
//TODO: add scouter service and scouter components logic
//TODO: add player profile analytics components logic