export type FoundationType = 'Shallow' | 'Shallow (Reinforced)' | 'Deep' | 'Pile';

export type BearingCapacityClass = 'Low' | 'Medium' | 'High';

export interface BearingFactor {
  value: number;
  unit: string;
  safeThreshold: number;
  source: 'Soil module' | 'Sentinel-2 NDMI' | 'SoilGrids' | 'Topography module';
  tooltip: string;
}

export interface BuildingLoadReference {
  floorCategory: string;
  /** TODO(backend): replace with real structural load data once available. Typical RC structure assumption ~10-12 kPa/floor. */
  typicalLoadKpa: number;
  supported: boolean;
}

export interface BearingPoint {
  lng: number;
  lat: number;
  capacity: number;
  depth: number;
}

export interface BearingData {
  bearingCapacity: number;
  uncertaintyRangeKpa: { min: number; max: number };
  capacityClass: BearingCapacityClass;
  isUnreliableEstimate: boolean;
  floorCountCategory: '1-2 floors' | '3-5 floors' | '6-10 floors' | '10+ floors';
  maxFloorsWithoutDeepFoundation: number;
  foundationType: FoundationType;
  factors: {
    clayPercent: BearingFactor;
    sandPercent: BearingFactor;
    moistureIndex: BearingFactor;
    waterTableDepth: BearingFactor;
    terrainSlope: BearingFactor;
  };
  buildingLoadReferences: BuildingLoadReference[];
  bearingPoints: BearingPoint[];
  waterTableLines: any[];
}
