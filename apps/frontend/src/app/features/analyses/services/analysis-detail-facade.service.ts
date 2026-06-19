import { Injectable, signal } from '@angular/core';
import { BoreholeData } from '../interfaces/borehole-data';
import { RiskData } from '../interfaces/risk-data';
import { SoilData } from '../interfaces/soil-data';
import { TopographyData } from '../interfaces/topography-data';
import {
  MOCK_TOPOGRAPHY_DATA,
  MOCK_SOIL_DATA,
  MOCK_RISK_DATA,
  MOCK_BOREHOLE_DATA,
} from '../analysis-mock-data';

@Injectable({
  providedIn: 'root',
})
export class AnalysisDetailFacadeService {
  readonly topographyData = signal<TopographyData | null>(null);
  readonly soilData = signal<SoilData | null>(null);
  readonly riskData = signal<RiskData | null>(null);
  readonly boreholeData = signal<BoreholeData | null>(null);

  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  loadAllData() {
    this.loading.set(true);
    // Simulate API call - replace with HTTP later
    this.topographyData.set(MOCK_TOPOGRAPHY_DATA);
    this.soilData.set(MOCK_SOIL_DATA);
    this.riskData.set(MOCK_RISK_DATA);
    this.boreholeData.set(MOCK_BOREHOLE_DATA);
    this.loading.set(false);
  }
}
