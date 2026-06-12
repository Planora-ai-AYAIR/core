export interface SoilData {
  bearingCapacity: number;
  plasticityIndex: number;
  organicContent: number;
  cohesion: number;
  composition: { type: string; percent: number; color: string }[];
  // Map layers
  soilCompositionGeoJSON: any; // FeatureCollection
  bearingPoints: { lng: number; lat: number; capacity: number; depth: number }[];
  waterTableLines: any[]; // Feature[]
}
