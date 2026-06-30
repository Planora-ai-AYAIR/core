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
  private reportGeneratedSubject = new Subject<any>();
  private reportFailedSubject = new Subject<any>();

  notification$ = this.notificationSubject.asObservable();
  analysisResult$ = this.analysisResultSubject.asObservable();
  reportGenerated$ = this.reportGeneratedSubject.asObservable();
  reportFailed$ = this.reportFailedSubject.asObservable();

  async startConnection(): Promise<void> {
    if (this.hubConnection?.state === signalR.HubConnectionState.Connected) {
      return;
    }

    // 1. Wait for a valid token (refreshes if necessary)
    const token = await this.auth.getValidAccessToken();
    if (!token) {
      console.warn('SignalR: No valid token available, skipping connection');
      return;
    }

    // 2. Build connection with an async accessTokenFactory that always fetches fresh token
    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(`${this.getBaseUrl()}/hubs/notifications`, {
        accessTokenFactory: async () => {
          // This runs BEFORE every connection or reconnection attempt
          const freshToken = await this.auth.getValidAccessToken();
          return freshToken ?? '';
        },
      })
      .withAutomaticReconnect()
      .build();

    // 3. Register event handlers
    this.registerEvents();

    // 4. Start
    try {
      await this.hubConnection.start();
    } catch (err) {
      console.error('SignalR error:', err);
    }
  }

  private registerEvents(): void {
    if (!this.hubConnection) return;

    this.hubConnection.on('NotificationReceived', (notification: NotificationDto) => {
      this.notificationSubject.next(notification);
    });

    this.hubConnection.on('AnalysisResultReceived', (envelope: AnalysisResultEnvelope) => {
      this.analysisResultSubject.next(envelope);
    });

    this.hubConnection.on('ReportGenerated', (event: any) => {
      this.reportGeneratedSubject.next(event);
    });

    this.hubConnection.on('ReportFailed', (event: any) => {
      this.reportFailedSubject.next(event);
    });
  }

  private getBaseUrl(): string {
    return environment.apiUrl.replace(/\/api\/?$/, '');
  }

  subscribeToParcel(parcelId: string): void {
    this.hubConnection?.invoke('SubscribeToParcel', parcelId).catch((err) => console.error(err));
  }

  stopConnection(): void {
    this.hubConnection?.stop();
  }

  ngOnDestroy(): void {
    this.hubConnection?.stop();
  }
}
