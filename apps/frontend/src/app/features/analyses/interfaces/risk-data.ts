export interface RiskData {
  floodRisk: string;
  seismicZone: string;
  liquefactionPotential: string;
  landslideRisk: string;
  hazards: { label: string; rating: string; description: string; icon: string }[];
  // Map layers
  floodFeatures: { risk: string; coords: [number, number][] }[];
  seismicPoint: [number, number];
  liquefactionPolygon: [number, number][];
}
