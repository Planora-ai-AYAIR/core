import { Injectable, inject, OnDestroy } from '@angular/core';
import { Subject } from 'rxjs';
import { environment } from '../../../environments/environment';
import { NotificationDto } from '../interfaces/notification/notification-dto';
import { AuthService } from './auth.service';
import * as signalR from '@microsoft/signalr';

@Injectable({ providedIn: 'root' })
export class SignalRService implements OnDestroy {
  private auth = inject(AuthService);
  private hubConnection?: signalR.HubConnection;
  private notificationSubject = new Subject<NotificationDto>();

  notification$ = this.notificationSubject.asObservable();

  startConnection(): void {
    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(`${this.getBaseUrl()}/hubs/notifications`, {
        accessTokenFactory: () => this.auth.accessToken ?? '',
      })
      .withAutomaticReconnect()
      .build();

    this.hubConnection.on('NotificationReceived', (notification: NotificationDto) => {
      this.notificationSubject.next(notification);
    });

    this.hubConnection.start().catch((err) => console.error('SignalR error:', err));
  }

  private getBaseUrl(): string {
    return environment.apiUrl.replace(/\/api\/?$/, '');
  }

  subscribeToParcel(parcelId: string): void {
    this.hubConnection?.invoke('SubscribeToParcel', parcelId).catch((err) => console.error(err));
  }

  ngOnDestroy(): void {
    this.hubConnection?.stop();
  }
}
