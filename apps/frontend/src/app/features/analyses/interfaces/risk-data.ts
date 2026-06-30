export interface RiskFactor {
  label: string;
  detail: string;
}

export interface RiskBreakdown {
  score: number; 
  level: 'Low' | 'Moderate' | 'High';
  icon: string; 
  color: string; 
  factors: RiskFactor[];
  mitigation?: string;
}

export interface MitigationItem {
  riskType: string;
  suggestion: string;
  costImpact: string;
  feasibility: string;
}

export interface RiskData {
  overallRiskScore: number; 
  overallRiskLevel: string; 
  benchmarkComparison: string; 
  floodRisk: RiskBreakdown;
  seismicRisk: RiskBreakdown;
  expansiveSoilRisk: RiskBreakdown;
  liquefactionRisk: RiskBreakdown;
  mitigations: MitigationItem[];

  floodFeatures: { risk: string; coords: [number, number][] }[];
  seismicZonesGeoJSON: any;
  expansiveSoilZonesGeoJSON: any;
  liquefactionZonesGeoJSON: any;
}
