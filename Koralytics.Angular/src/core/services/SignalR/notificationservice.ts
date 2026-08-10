import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
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
   */
  notifyPlayerMilestone(playerId: number, achievementType: string): Observable<{ message: string }> {
    const params = new HttpParams().set('achievementType', achievementType);
    return this.http.post<{ message: string }>(
      `${this.baseUrl}/players/${playerId}/milestone`,
      {},
      { params }
    );
  }

  /**
   * Triggers a parent-alert notification for a player's linked guardians.
   * Matches: POST api/Notification/players/{playerId}/parent-alert
   */
  notifyPlayerParents(playerId: number, eventType: string): Observable<{ message: string }> {
    const params = new HttpParams().set('eventType', eventType);
    return this.http.post<{ message: string }>(
      `${this.baseUrl}/players/${playerId}/parent-alert`,
      {},
      { params }
    );
  }

  /**
   * Triggers a subscription-grace-period alert to both the player and their linked parents.
   * Matches: POST api/Notification/players/{playerId}/academies/{academyId}/subscription-grace
   */
  notifySubscriptionGrace(playerId: number, academyId: number): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(
      `${this.baseUrl}/players/${playerId}/academies/${academyId}/subscription-grace`,
      {}
    );
  }

  /**
   * Triggers a scouter-alert notification to everyone following a player.
   * Matches: POST api/Notification/players/{playerId}/scouter-alerts
   */
  notifyScouterFollowers(playerId: number, eventType: string): Observable<{ message: string }> {
    const params = new HttpParams().set('eventType', eventType);
    return this.http.post<{ message: string }>(
      `${this.baseUrl}/players/${playerId}/scouter-alerts`,
      {},
      { params }
    );
  }
  
  /**
   * Broadcasts live match events (kickoff, goals, full-time).
   */
  triggerMatchEventNotification(matchId: number, eventTitle: string, eventMessage: string, eventType: string): Observable<{ message: string }> {
    const params = new HttpParams()
      .set('eventTitle', eventTitle)
      .set('eventMessage', eventMessage)
      .set('eventType', eventType);
      
    return this.http.post<{ message: string }>(
      `${this.baseUrl}/matches/${matchId}/events`, 
      {},
      { params }
    );
  }
  
  /**
   * Notifies the academy administration that a player has successfully completed a subscription payment.
   */
  notifyAcademySubscriptionPaid(playerId: number, academyId: number): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(
      `${this.baseUrl}/players/${playerId}/academies/${academyId}/subscription-paid`,
      {}
    );
  }

  /**
   * Sends an administrative alert/notification to an academy's admins.
   * Matches: POST api/Notification/academies/{academyId}/academy-alerts
   */
  notifyAcademy(academyId: number, message: string): Observable<{ message: string }> {
    const params = new HttpParams().set('message', message);
    return this.http.post<{ message: string }>(
      `${this.baseUrl}/academies/${academyId}/academy-alerts`, 
      {},
      { params }
    );
  }

  // ==========================================
  // NEW BULK ENDPOINTS
  // ==========================================

  /**
   * Triggers a milestone real-time broadcast to multiple players at once.
   * Matches: POST api/Notification/players/bulk-milestone
   */
  notifyMultiplePlayersMilestone(playerIds: number[], message: string): Observable<{ message: string }> {
    const body = { playerIds, message };
    return this.http.post<{ message: string }>(
      `${this.baseUrl}/players/bulk-milestone`,
      body
    );
  }

  /**
   * Dispatches real-time critical events directly to parents tracking a specific list of players.
   * Matches: POST api/Notification/players/bulk-parent-alert
   */
  notifyParentsOfPlayers(playerIds: number[], eventType: string): Observable<{ message: string }> {
    const body = { playerIds, eventType };
    return this.http.post<{ message: string }>(
      `${this.baseUrl}/players/bulk-parent-alert`,
      body
    );
  }
  /**
 * Broadcasts an administrative alert to multiple academies at once in a single request.
 * Matches: POST api/Notification/academies/bulk-alert
 */
notifyMultipleAcademies(academyIds: number[], message: string): Observable<{ message: string }> {
  const body = { academyIds, message };
  return this.http.post<{ message: string }>(
    `${this.baseUrl}/academies/bulk-alert`,
    body
  );
}
}