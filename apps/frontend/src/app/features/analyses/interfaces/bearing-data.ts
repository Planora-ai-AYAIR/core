export interface BearingData {
  bearingCapacity: number;
  plasticityIndex: number;
  organicContent: number;
  cohesion: number;
  moistureIndex: number;
  waterTableDepth: number;
  terrainSlope: number;
  clayPercent: number;
  sandPercent: number;
  bearingPoints: { lng: number; lat: number; capacity: number; depth: number }[];
  waterTableLines: any[];
}
