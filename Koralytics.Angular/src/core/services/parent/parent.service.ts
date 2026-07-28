import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
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
}