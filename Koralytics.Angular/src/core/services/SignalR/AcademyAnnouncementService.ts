import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { AnnouncementResponseDto } from '../../interfaces/AnnouncementResponse';
import { CreateAnnouncementPayload } from '../../interfaces/CreateAnnouncementPayload';

// Every endpoint on this controller wraps its real payload in this envelope
// (see swagger response for GET .../announcements):
//   { isSuccess, statusCode, message, data: <T>, errorCode, errors }
// The old service methods returned this whole envelope typed as if it were
// just <T> -- TypeScript never caught it because HttpClient.get<T>() doesn't
// validate the shape at runtime, it just trusts the generic. That mismatch
// is what made `announcements()` hold an object instead of an array.
interface ApiEnvelope<T> {
  isSuccess: boolean;
  statusCode: number;
  message: string;
  data: T;
  errorCode: string | null;
  errors: string[] | null;
}

@Injectable({
    providedIn: 'root'
})
export class AcademyAnnouncementService {
    private readonly apiUrl = `${environment.apiUrl}/api`;

    constructor(private readonly http: HttpClient) { }

    sendAnnouncement(academyId: number, dto: CreateAnnouncementPayload): Observable<AnnouncementResponseDto> {
        return this.http
          .post<ApiEnvelope<AnnouncementResponseDto>>(`${this.apiUrl}/AcademyAnnouncement/${academyId}/announcements`, dto)
          .pipe(map((res) => res.data));
    }

    getAnnouncements(academyId: number): Observable<AnnouncementResponseDto[]> {
        return this.http
          .get<ApiEnvelope<AnnouncementResponseDto[]>>(`${this.apiUrl}/AcademyAnnouncement/${academyId}/announcements`)
          .pipe(map((res) => res.data));
    }
}