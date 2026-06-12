export interface BoreholeData {
  points: { id: string; lng: number; lat: number; depth: number; cost: number }[];
  optimalArea: [number, number][];
  estimatedCost: number;
  drillingCost: number;
  testingCost: number;
}
