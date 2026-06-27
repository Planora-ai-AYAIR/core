export interface AnalysisOptionsDto {
  includeTopography?: boolean;
  includeSoil?: boolean;
  includeBearing?: boolean;
  includeRisk?: boolean;
  includeBorehole?: boolean;
  contourInterval?: number;
  slopeCategories?: number[];
  referencePlane?: string;
  soilDepths?: string[];
}