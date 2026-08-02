import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface ParentChild {
    playerId: number;
    fullName: string;
    position?: string;
    teamName?: string;
    jerseyNumber?: number;
    photoUrl?: string;
}

export interface ParentPlayerSearchResponse {
    playerId: number;
    fullName: string;
    position?: string;
    teamName?: string;
    photoUrl?: string;
    hasPendingRequest: boolean;
    isAlreadyLinked: boolean;
}

export interface PlayerParent {
    parentId: number;
    fullName: string;
    email: string;
    phoneNumber?: string;
    photoUrl?: string;
}

export interface ParentPlayerJoinRequest {
    id: number;
    parentId: number;
    playerId: number;
    playerName: string;
    playerPhotoUrl?: string;
    playerPosition?: string;
    parentName: string;
    parentEmail: string;
    status: number; // 0=Pending, 1=Accepted, 2=Rejected, 3=Cancelled
    requestedAt: string;
    respondedAt?: string;
}

@Injectable({
    providedIn: 'root'
})
export class ParentService {
    private http = inject(HttpClient);
    private readonly apiUrl = `${environment.apiUrl}/api/Parent`;

    /**
     * Fetches the list of children (players) linked to the currently logged-in parent.
     */
    getMyChildren(): Observable<any> {
        return this.http.get<any>(`${this.apiUrl}/my-children`);
    }

    /**
     * Searches available players by name.
     */
    searchPlayers(name?: string): Observable<any> {
        let params = new HttpParams();
        if (name) {
            params = params.set('name', name);
        }
        return this.http.get<any>(`${this.apiUrl}/search-players`, { params });
    }

    /**
     * Sends a join request from the parent to a child player.
     */
    sendChildRequest(playerId: number): Observable<any> {
        const params = new HttpParams().set('playerId', playerId.toString());
        return this.http.post<any>(`${this.apiUrl}/child-requests`, null, { params });
    }

    /**
     * Retrieves pending join requests sent by the logged-in parent.
     */
    getMyPendingRequests(): Observable<any> {
        return this.http.get<any>(`${this.apiUrl}/my-pending-requests`);
    }

    /**
     * Cancels a pending join request sent by the logged-in parent.
     */
    cancelChildRequest(requestId: number): Observable<any> {
        return this.http.patch<any>(`${this.apiUrl}/child-requests/${requestId}/cancel`, null);
    }

    /**
     * Unlinks a child player from the parent account.
     */
    unlinkChild(playerId: number): Observable<any> {
        return this.http.delete<any>(`${this.apiUrl}/children/${playerId}`);
    }

    /**
     * Retrieves pending parent join requests received by the logged-in player.
     */
    getPlayerPendingRequests(): Observable<any> {
        return this.http.get<any>(`${this.apiUrl}/player-pending-requests`);
    }

    /**
     * Allows a player to respond (Accept=2, Reject=3) to a parent join request.
     */
    respondToChildRequest(requestId: number, status: number): Observable<any> {
        return this.http.put<any>(`${this.apiUrl}/child-requests/${requestId}/respond`, { status });
    }

    /**
     * Retrieves linked parents/guardians for the logged-in player.
     */
    getMyParents(): Observable<any> {
        return this.http.get<any>(`${this.apiUrl}/my-parents`);
    }

    /**
     * Unlinks a parent/guardian from the logged-in player's account.
     */
    unlinkParent(parentId: number): Observable<any> {
        return this.http.delete<any>(`${this.apiUrl}/parents/${parentId}`);
    }
}