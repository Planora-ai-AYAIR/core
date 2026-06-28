export interface TopographyResultsDto {
  parcelId: string;
  elevation: ElevationDto;
  slopeAnalysis: SlopeAnalysisDto;
  cutFill: CutFillDto;
  contourLines: ContourLinesDto;
  pondingRisk: PondingRiskDto;
  rasterTiles?: RasterTilesDto;
  generatedAt: string;
}

export interface ElevationDto {
  min: number;
  max: number;
  mean: number;
  unit: string;
}
export interface SlopeCategoryDto {
  category: string;
  range: string;
  percentage: number;
  color: string;
}
export interface SlopeAnalysisDto {
  distribution: SlopeCategoryDto[];
}
export interface CutFillDto {
  cutVolume: number;
  fillVolume: number;
  netVolume: number;
  unit: string;
}
export interface ContourLinesDto {
  geoJsonUrl: string;
  interval: number;
}
export interface PondingRiskDto {
  zonesCount: number;
  totalArea: number;
  unit: string;
  geoJsonUrl: string;
}
export interface RasterTilesDto {
  elevation: string;
  slope: string;
}
