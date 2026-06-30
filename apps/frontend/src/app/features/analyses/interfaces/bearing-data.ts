export interface BearingData {
  bearingCapacity: number;
  uncertaintyRangeKpa: { min: number; max: number };
  capacityClass: string;
  isUnreliableEstimate: boolean;
  floorCountCategory: string;
  maxFloorsWithoutDeepFoundation: number;
  foundationType: string;
  factors: {
    clayPercent: {
      value: number;
      unit: string;
      safeThreshold: number;
      source: string;
      tooltip: string;
    };
    sandPercent: {
      value: number;
      unit: string;
      safeThreshold: number;
      source: string;
      tooltip: string;
    };
    moistureIndex: {
      value: number;
      unit: string;
      safeThreshold: number;
      source: string;
      tooltip: string;
    };
    waterTableDepth: {
      value: number;
      unit: string;
      safeThreshold: number;
      source: string;
      tooltip: string;
    };
    terrainSlope: {
      value: number;
      unit: string;
      safeThreshold: number;
      source: string;
      tooltip: string;
    };
  };
  buildingLoadReferences: { floorCategory: string; typicalLoadKpa: number; supported: boolean }[];
  bearingPoints: BearingPoint[];
  waterTableLines: any[];
  trafficLight?: string;
  range?: string;
  confidence?: number;
  disclaimer?: string;
  modelMetadata?: {
    modelName: string;
    framework: string;
    trainingR2: number;
    shapEnabled: boolean;
  };
  featureImportance?: {
    feature: string;
    weight: number;
  }[];
}

export interface BearingPoint {
  lng: number;
  lat: number;
  capacity: number;
  depth: number;
}
