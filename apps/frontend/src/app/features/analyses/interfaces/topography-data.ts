export interface TopographyData {
  minElevation: number;
  maxElevation: number;
  meanElevation: number;
  cutFill: number;
  slopeDistribution: { name: string; value: number }[];
  pondingZones: { id: number; lat: number; lng: number; area: number; risk: string }[];
  engineeringFlags: { text: string }[];
  // Map layers
  elevationGrid: { lng: number; lat: number; elev: number }[];
  contourLines: any[]; // Feature[]
  slopePolygons: any[]; // Feature[]
  pondingPolygons: any[]; // Feature[]
}
