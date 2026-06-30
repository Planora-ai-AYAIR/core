export interface TopographyData {
  minElevation: number;
  maxElevation: number;
  meanElevation: number;
  cutFill: number;
  slopeDistribution: {
    name: string;
    value: number;
  }[];
  pondingRisk?: {
    riskLevel: string;
    zonesCount: number;
    affectedAreaM2: number;
  };

  pondingZones: any[];
  engineeringFlags: any[];
  elevationGrid: any[];
  contourLines: any[];
  slopePolygons: any[];
  pondingPolygons: any[];
}
