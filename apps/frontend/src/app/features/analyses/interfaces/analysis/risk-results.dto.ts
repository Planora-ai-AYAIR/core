export interface RiskResultsDto {
  parcelId: string;
  overallRiskScore: number;
  overallRiskLevel: string;
  flood: RiskSubResultDto;
  seismic: RiskSubResultDto;
  expansiveSoil: RiskSubResultDto;
  liquefaction: RiskSubResultDto;
  generatedAt: string;
}

export interface RiskSubResultDto {
  score: number;
  level: string;
  factors: string[] | null;
  geoJsonUrl: string | null;
  source: string | null;
  replacementDepth: number | null;
  susceptibility: string | null;
}
