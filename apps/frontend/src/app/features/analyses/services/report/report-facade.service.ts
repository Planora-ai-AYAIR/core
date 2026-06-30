import { inject, Injectable, signal } from '@angular/core';
import { ReportApiService } from './report-api.service';
import { Subscription } from 'rxjs';
import { NotificationDto } from '../../../../core/interfaces/notification/notification-dto';
import { SignalRService } from '../../../../core/services/signalr.service';

@Injectable({
  providedIn: 'root',
})
export class ReportFacadeService {
  readonly reportStatus = signal<'idle' | 'generating' | 'ready' | 'failed'>('idle');
  readonly reportDownloadUrl = signal<string | null>(null);
  readonly reportError = signal<string | null>(null);

  private reportJobId?: string;
  private api = inject(ReportApiService);
  private signalR = inject(SignalRService);
  private notificationSub?: Subscription;

  constructor() {
    this.listenForReportNotifications();
  }

  generateReport(parcelId: string, options: any) {
    this.reportStatus.set('generating');
    this.reportError.set(null);
    this.api.submitReport(parcelId, options).subscribe({
      next: (response) => {
        this.reportJobId = response.reportId;
        localStorage.setItem(`report_job_${parcelId}`, response.reportId);
      },
      error: (err) => {
        const code = err?.error?.errors?.[0]?.code;
        if (code === 'ALREADYRUNNING') {
          this.reportError.set('A report is already being generated for this parcel.');
        } else {
          this.reportStatus.set('failed');
          this.reportError.set(err.error?.message ?? 'Report generation failed');
        }
      },
    });
  }
  checkExistingReport(parcelId: string) {
    const storedJobId = localStorage.getItem(`report_job_${parcelId}`);
    if (storedJobId && storedJobId !== 'undefined' && storedJobId.length > 10) {
      this.reportJobId = storedJobId;
      this.reportStatus.set('generating');
      this.pollReportStatus(storedJobId, parcelId);
    }
  }

  private pollReportStatus(reportId: string, parcelId: string) {
    if (!reportId || reportId === 'undefined') {
      console.warn('Invalid reportId, aborting polling');
      return;
    }
    const interval = setInterval(() => {
      this.api.getReportDownload(reportId).subscribe({
        next: (data) => {
          if (data.status === 'Completed') {
            this.reportStatus.set('ready');
            this.reportDownloadUrl.set(data.downloadUrl);
            clearInterval(interval);
            localStorage.removeItem(`report_job_${parcelId}`);
          } else if (data.status === 'Failed') {
            this.reportStatus.set('failed');
            this.reportError.set('Report generation failed');
            clearInterval(interval);
            localStorage.removeItem(`report_job_${parcelId}`);
          }
        },
        error: () => {
          // Retry on error
        },
      });
    }, 5000);
  }

  /** Listen to NotificationReceived for report events */
  private listenForReportNotifications() {
    this.notificationSub = this.signalR.notification$.subscribe((notification: NotificationDto) => {
      if (notification.type === 'ReportCompleted') {
        // The notification data likely contains parcelId and reportJobId
        const data = notification.data ? JSON.parse(notification.data) : null;
        if (data?.parcelId && data?.reportJobId) {
          this.reportJobId = data.reportJobId;
          this.fetchDownloadUrl(data.reportJobId);
        } else {
          // If we already have a reportJobId, use that
          if (this.reportJobId) {
            this.fetchDownloadUrl(this.reportJobId);
          }
        }
      } else if (notification.type === 'ReportFailed') {
        const data = notification.data ? JSON.parse(notification.data) : null;
        this.reportStatus.set('failed');
        this.reportError.set(data?.message || 'Report generation failed');
      }
    });
  }

  private fetchDownloadUrl(reportId: string) {
    this.api.getReportDownload(reportId).subscribe({
      next: (data) => {
        this.reportStatus.set('ready');
        this.reportDownloadUrl.set(data.downloadUrl);
        localStorage.removeItem(`report_job_${data.parcelId}`);
      },
      error: () => {
        this.reportStatus.set('failed');
        this.reportError.set('Failed to retrieve download link');
      },
    });
  }

  // When ReportGenerated SignalR event arrives
  onReportGenerated(parcelId: string, reportId: string) {
    // Fetch the download URL
    this.api.getReportDownload(reportId).subscribe({
      next: (data) => {
        this.reportStatus.set('ready');
        this.reportDownloadUrl.set(data.downloadUrl);
      },
      error: (err) => {
        this.reportStatus.set('failed');
        this.reportError.set('Failed to retrieve download link');
      },
    });
  }

  onReportFailed(errorMessage: string) {
    this.reportStatus.set('failed');
    this.reportError.set(errorMessage);
  }
}
