import { Injectable, inject, OnDestroy } from '@angular/core';
import { Subject } from 'rxjs';
import { environment } from '../../../environments/environment';
import { NotificationDto } from '../interfaces/notification/notification-dto';
import { AuthService } from './auth.service';
import * as signalR from '@microsoft/signalr';
import { AnalysisResultEnvelope } from '../../features/analyses/interfaces/analysis/analysis-result-envelope';

@Injectable({ providedIn: 'root' })
export class SignalRService implements OnDestroy {
  private auth = inject(AuthService);
  private hubConnection?: signalR.HubConnection;
  private notificationSubject = new Subject<NotificationDto>();
  private analysisResultSubject = new Subject<AnalysisResultEnvelope>();

  notification$ = this.notificationSubject.asObservable();
  analysisResult$ = this.analysisResultSubject.asObservable();

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

    this.hubConnection.on('AnalysisResultReceived', (envelope: AnalysisResultEnvelope) => {
      this.analysisResultSubject.next(envelope);
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
