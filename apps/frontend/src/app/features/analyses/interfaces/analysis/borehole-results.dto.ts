export interface BoreholeResultsDto {
  parcelId: string;
  minimumRequired: number;
  optimalCount: number;
  coveragePercentage: number;
  gridSize: string | null;
  placementStrategy: string | null;
  placementPoints: BoreholePlacementPointDto[] | null;
  placementGeoJsonUrl: string | null;
  costComparison: CostComparisonDto;
  generatedAt: string;
}

export interface BoreholePlacementPointDto {
  id: string;
  latitude: number;
  longitude: number;
  priority: string;
  reason: string | null;
  estimatedDepth: number | null;
}

export interface CostComparisonDto {
  traditionalBoreholeCount: number;
  traditionalEstimatedCost: number;
  optimizedBoreholeCount: number;
  optimizedEstimatedCost: number;
  savingsAmount: number;
  savingsPercentage: number;
  currency: string;
}
