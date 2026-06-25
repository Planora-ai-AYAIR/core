import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { AnalysisOptionsDto } from '../interfaces/start-analysis/analysis-options-dto';
import { StartAnalysisResponse } from '../interfaces/start-analysis/start-analysis-response';

@Injectable({ providedIn: 'root' })
export class AnalysisApiService {
  private http = inject(HttpClient);
  private baseUrl = environment.apiUrl;

  startAnalysis(parcelId: string, options: AnalysisOptionsDto): Observable<StartAnalysisResponse> {
    const url = `${this.baseUrl}${environment.Analysis.start(parcelId)}`;
    return this.http
      .post<any>(url, { analysisOptions: options })
      .pipe(map((envelope) => envelope.data));
  }
}