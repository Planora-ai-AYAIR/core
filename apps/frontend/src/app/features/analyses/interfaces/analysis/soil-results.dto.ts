export interface SoilResultsDto {
  parcelId: string;
  sandPercent: number;
  siltPercent: number;
  clayPercent: number;
  compositionUnit: string;
  bulkDensity: number;
  bulkDensityUnit: string;
  organicCarbon: number;
  organicCarbonUnit: string;
  ph: number;
  primaryType: string;
  usdaClass: string;
  aiConfidence: number | null;
  multiDepthProfile: DepthProfileItem[] | null;
  heatmapTileUrl: string | null;

  bearingCapacityEstimate: number;
  bearingCapacityCategory: string;

  generatedAt: string;
}

export interface DepthProfileItem {
  depth: string;
  sand: number;
  clay: number;
  type: string;
}
