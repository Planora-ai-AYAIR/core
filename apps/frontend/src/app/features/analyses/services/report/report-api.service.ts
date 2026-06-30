import { inject, Injectable } from '@angular/core';
import { Observable, map } from 'rxjs';
import { environment } from '../../../../../environments/environment';
import { HttpClient } from '@angular/common/http';

@Injectable({
  providedIn: 'root',
})
export class ReportApiService {
  private http = inject(HttpClient);
  private baseUrl = environment.apiUrl;

  /** Start PDF generation */
  submitReport(parcelId: string, options: any): Observable<any> {
    return this.http
      .post<any>(`${this.baseUrl}${environment.Report.submit(parcelId)}`, options)
      .pipe(map((env) => env.data));
  }

  /** Get download URL for a report (by report job ID) */
  getReportDownload(reportId: string): Observable<any> {
    return this.http
      .get<any>(`${this.baseUrl}${environment.Report.download(reportId)}`)
      .pipe(map((env) => env.data));
  }
}
