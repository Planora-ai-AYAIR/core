export interface SoilDepthData {
  depthRange: string;
  sandPercent: number;
  siltPercent: number;
  clayPercent: number;
  classification: string;
  color: string;
}

export interface SoilData {
  bulkDensity: number;
  organicCarbon: number;
  pH: number;
  classification: string;
  confidence: number;
  composition: { type: string; percent: number; color: string }[];
  soilCompositionGeoJSON: any;
  depthProfiles: SoilDepthData[];
  heatmapUrls: Record<string, string>;
  heatmapLegend: { color: string; label: string }[];
  spectralIndices?: {
    ndviMean: number;
    bsiMean: number;
    ndmiMean: number;
  };
}
