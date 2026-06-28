import { inject, Injectable, signal } from '@angular/core';
import { ReportApiService } from './report-api.service';

@Injectable({
  providedIn: 'root',
})
export class ReportFacadeService {
  readonly reportStatus = signal<'idle' | 'generating' | 'ready' | 'failed'>('idle');
  readonly reportDownloadUrl = signal<string | null>(null);
  readonly reportError = signal<string | null>(null);

  private reportJobId?: string;

  private api = inject(ReportApiService);
  
  generateReport(parcelId: string, options: any) {
    this.reportStatus.set('generating');
    this.api.submitReport(parcelId, options).subscribe({
      next: (response) => {
        this.reportJobId = response.reportJobId;
      },
      error: (err) => {
        this.reportStatus.set('failed');
        this.reportError.set(err.error?.message ?? 'Report generation failed');
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
