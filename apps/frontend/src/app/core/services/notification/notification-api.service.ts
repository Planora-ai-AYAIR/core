import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { NotificationsPageDto } from '../../interfaces/notification/notification-page-dto';

@Injectable({ providedIn: 'root' })
export class NotificationApiService {
  private http = inject(HttpClient);
  private baseUrl = environment.apiUrl;

  getNotifications(unreadOnly = false, take = 20, skip = 0): Observable<NotificationsPageDto> {
    const params = new HttpParams()
      .set('unreadOnly', unreadOnly)
      .set('take', take)
      .set('skip', skip);
    return this.http
      .get<any>(`${this.baseUrl}${environment.Notifications.list}`, { params })
      .pipe(map((envelope) => envelope.data));
  }

  markAsRead(id: string): Observable<void> {
    return this.http
      .post<any>(`${this.baseUrl}${environment.Notifications.markAsRead(id)}`, {})
      .pipe(map(() => undefined));
  }
}
