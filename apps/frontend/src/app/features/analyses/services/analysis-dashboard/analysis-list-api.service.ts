import { inject, Injectable } from '@angular/core';
import { environment } from '../../../../../environments/environment';
import { map, Observable } from 'rxjs';
import { HttpClient } from '@angular/common/http';
import { ApiResponse } from '../../../../core/interfaces/api-response';

@Injectable({
  providedIn: 'root',
})
export class AnalysisListApiService {
  private http = inject(HttpClient);
  private baseUrl = environment.apiUrl;

  getAnalysisDashboard(): Observable<ApiResponse<AnalysisJobsSummaryResponse>> {
    const url = `${this.baseUrl}${environment.Analysis.dashboard}`;
    return this.http.get<ApiResponse<AnalysisJobsSummaryResponse>>(url);
  }
}
