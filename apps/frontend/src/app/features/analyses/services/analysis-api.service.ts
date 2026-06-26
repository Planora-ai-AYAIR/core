import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '../../../../environments/environment';

// ── DTO interfaces ──
import { StartAnalysisResponse } from '../interfaces/start-analysis/start-analysis-response';
import { AnalysisOptionsDto } from '../interfaces/start-analysis/analysis-options-dto';
import { TopographyResultsDto } from '../interfaces/analysis/topography-results.dto';
import { SoilResultsDto } from '../interfaces/analysis/soil-results.dto';
import { RiskResultsDto } from '../interfaces/analysis/risk-results.dto';
import { BoreholeResultsDto } from '../interfaces/analysis/borehole-results.dto';
import { ParcelAnalysisStatusResponse } from '../interfaces/analysis/parcel-analysis-status-response';

@Injectable({ providedIn: 'root' })
export class AnalysisApiService {
  private http = inject(HttpClient);
  private baseUrl = environment.apiUrl;

  /** Get current progress of each module for a parcel. */
  getParcelAnalysisStatus(parcelId: string): Observable<ParcelAnalysisStatusResponse> {
    return this.http
      .get<any>(`${this.baseUrl}${environment.Analysis.status(parcelId)}`)
      .pipe(map((envelope) => envelope.data));
  }

  /** Get topography analysis results. */
  getTopographyResults(parcelId: string): Observable<TopographyResultsDto> {
    return this.http
      .get<any>(`${this.baseUrl}${environment.Analysis.topography(parcelId)}?includeTiles=true`)
      .pipe(map((envelope) => envelope.data));
  }

  /** Get soil analysis results. */
  getSoilResults(parcelId: string): Observable<SoilResultsDto> {
    return this.http
      .get<any>(`${this.baseUrl}${environment.Analysis.soil(parcelId)}`)
      .pipe(map((envelope) => envelope.data));
  }

  /** Get risk analysis results. */
  getRiskResults(parcelId: string): Observable<RiskResultsDto> {
    return this.http
      .get<any>(`${this.baseUrl}${environment.Analysis.risk(parcelId)}`)
      .pipe(map((envelope) => envelope.data));
  }

  /** Get borehole analysis results. */
  getBoreholeResults(parcelId: string): Observable<BoreholeResultsDto> {
    return this.http
      .get<any>(`${this.baseUrl}${environment.Analysis.borehole(parcelId)}`)
      .pipe(map((envelope) => envelope.data));
  }
}
