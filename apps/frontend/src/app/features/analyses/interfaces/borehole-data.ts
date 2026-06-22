export interface BoreholePoint {
  id: string;
  lng: number;
  lat: number;
  priority: 'Critical' | 'High' | 'Medium' | 'Low';
  reason: string;
  estimatedDepth: number; 
}

export interface CostAnalysis {
  traditionalCount: number;
  traditionalCost: number; 
  optimizedCount: number;
  optimizedCost: number;
  savingsAmount: number;
  savingsPercent: number;
  ratePerMeter: number;
}

export interface BoreholeParameters {
  maxSpacing: number; 
  minBoreholes: number;
  targetDepth: number; 
  unit: 'm' | 'ft';
}

export interface BoreholeData {
  minRequired: number;
  recommended: number;
  coveragePercent: number;
  gridSize: string; 
  strategy: string; 
  placementPoints: BoreholePoint[];
  costAnalysis: CostAnalysis;
  parameters: BoreholeParameters;
}
