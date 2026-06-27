import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { ParcelAnalysisStatusResponse } from '../interfaces/analysis/parcel-analysis-status-response';
import { ParcelAnalysisFullResponse } from '../interfaces/analysis/analysis-full-response';

@Injectable({ providedIn: 'root' })
export class AnalysisApiService {
  private http = inject(HttpClient);
  private baseUrl = environment.apiUrl;

  getFullAnalysis(parcelId: string): Observable<ParcelAnalysisFullResponse> {
    return this.http
      .get<any>(`${this.baseUrl}/api/parcels/${parcelId}/analysis`)
      .pipe(map((envelope) => envelope.data));
  }

  getParcelAnalysisStatus(parcelId: string): Observable<ParcelAnalysisStatusResponse> {
    return this.http
      .get<any>(`${this.baseUrl}${environment.Analysis.status(parcelId)}`)
      .pipe(map((envelope) => envelope.data));
  }
}
