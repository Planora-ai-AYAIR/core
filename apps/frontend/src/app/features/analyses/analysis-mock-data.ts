import { BearingData } from './interfaces/bearing-data';
import { BoreholeData } from './interfaces/borehole-data';
import { RiskData } from './interfaces/risk-data';
import { SoilData } from './interfaces/soil-data';
import { TopographyData } from './interfaces/topography-data';

// ---------- Helper functions (extracted from TopographyMapInitialiser) ----------
function buildElevationGrid(): { lng: number; lat: number; elev: number }[] {
  const cx = 31.942,
    cy = 30.633,
    cols = 14,
    rows = 12;
  const pts: { lng: number; lat: number; elev: number }[] = [];
  for (let i = 0; i < cols; i++) {
    for (let j = 0; j < rows; j++) {
      const dx = -0.0007 + (i / (cols - 1)) * 0.0014,
        dy = -0.0005 + (j / (rows - 1)) * 0.001;
      const nx = dx / 0.0014,
        ny = dy / 0.001;
      let elev =
        33.9 +
        nx * 16 +
        ny * 5 +
        4.5 * Math.exp(-((nx - 0.28) ** 2 + (ny + 0.22) ** 2) * 18) -
        3.0 * Math.exp(-((nx + 0.32) ** 2 + (ny - 0.28) ** 2) * 22) +
        1.5 * Math.sin(nx * Math.PI) * Math.cos(ny * Math.PI);
      pts.push({
        lng: cx + dx,
        lat: cy + dy,
        elev: Math.round(Math.max(22, Math.min(46, elev)) * 10) / 10,
      });
    }
  }
  return pts;
}

function buildContourLines(): any[] {
  const cx = 31.942,
    cy = 30.633;
  const levels = [24, 27, 30, 33, 36, 39, 42];

  return levels.map((elev, idx) => {
    const t = idx / (levels.length - 1);
    const ox = cx + t * 0.00042;
    const oy = cy + t * 0.00021;
    const a = 0.00062 - t * 0.00048;
    const b = 0.00046 - t * 0.00036;
    const rot = -0.52;

    const coords: [number, number][] = [];
    for (let deg = 0; deg <= 360; deg += 6) {
      const r = (deg * Math.PI) / 180;
      const x = a * Math.cos(r),
        y = b * Math.sin(r);
      coords.push([
        ox + x * Math.cos(rot) - y * Math.sin(rot),
        oy + x * Math.sin(rot) + y * Math.cos(rot),
      ]);
    }

    return {
      type: 'Feature',
      properties: { elevation: elev },
      geometry: { type: 'LineString', coordinates: coords },
    };
  });
}

function buildSlopePolygons(): any[] {
  const SW: [number, number] = [31.941933, 30.633079];
  const NW: [number, number] = [31.942059, 30.63315];
  const NE: [number, number] = [31.942183, 30.632954];
  const SE: [number, number] = [31.942048, 30.632878];

  const lerp = (a: [number, number], b: [number, number], t: number): [number, number] => [
    a[0] + (b[0] - a[0]) * t,
    a[1] + (b[1] - a[1]) * t,
  ];

  const mSW_NW = lerp(SW, NW, 0.5);
  const mNW_NE = lerp(NW, NE, 0.5);
  const mNE_SE = lerp(NE, SE, 0.5);
  const mSE_SW = lerp(SE, SW, 0.5);
  const ctr = lerp(lerp(SW, NE, 0.5), lerp(NW, SE, 0.5), 0.5);

  return [
    { slope: 'flat', ring: [SW, mSW_NW, ctr, mSE_SW, SW] },
    { slope: 'gentle', ring: [mSW_NW, NW, mNW_NE, ctr, mSW_NW] },
    { slope: 'moderate', ring: [mSE_SW, ctr, mNE_SE, SE, mSE_SW] },
    { slope: 'steep', ring: [ctr, mNW_NE, NE, mNE_SE, ctr] },
  ].map(({ slope, ring }) => ({
    type: 'Feature',
    properties: { slope },
    geometry: { type: 'Polygon', coordinates: [ring] },
  }));
}

function buildPondingPolygons(): any[] {
  const z1: [number, number][] = [
    [31.94183, 30.6331],
    [31.94192, 30.63316],
    [31.94202, 30.63312],
    [31.94206, 30.63304],
    [31.942, 30.63296],
    [31.94188, 30.63294],
    [31.94181, 30.63302],
    [31.94183, 30.6331],
  ];
  const z2: [number, number][] = [
    [31.942, 30.63288],
    [31.94214, 30.63295],
    [31.94225, 30.63292],
    [31.94229, 30.63282],
    [31.94222, 30.63271],
    [31.94207, 30.63268],
    [31.94198, 30.63276],
    [31.942, 30.63288],
  ];

  return [
    { area: 120, risk: 'Low', ring: z1 },
    { area: 350, risk: 'Moderate', ring: z2 },
  ].map(({ area, risk, ring }) => ({
    type: 'Feature',
    properties: { area, risk },
    geometry: { type: 'Polygon', coordinates: [ring] },
  }));
}

// ---------- Topography mock ----------
export const MOCK_TOPOGRAPHY_DATA: TopographyData = {
  minElevation: 22.5,
  maxElevation: 45.3,
  meanElevation: 33.9,
  cutFill: 1450,
  slopeDistribution: [
    { name: 'Flat (<2%)', value: 40 },
    { name: 'Gentle (2-5%)', value: 30 },
    { name: 'Moderate (5-15%)', value: 20 },
    { name: 'Steep (>15%)', value: 10 },
  ],
  pondingZones: [
    { id: 1, lat: 30.63305, lng: 31.94188, area: 120, risk: 'Low' },
    { id: 2, lat: 30.6328, lng: 31.94213, area: 350, risk: 'Moderate' },
  ],
  engineeringFlags: [
    { text: 'High elevation delta across parcel: 22.8 m — significant grading work expected.' },
    { text: '2 ponding risk zones detected (470 m² combined) — drainage design required.' },
  ],
  elevationGrid: buildElevationGrid(),
  contourLines: buildContourLines(),
  slopePolygons: buildSlopePolygons(),
  pondingPolygons: buildPondingPolygons(),
};

// ---------- Soil mock ----------
export const MOCK_SOIL_DATA: SoilData = {
  bulkDensity: 1.45,
  organicCarbon: 2.3,
  pH: 6.8,
  classification: 'Sandy Loam',
  confidence: 0.92,
  depthProfiles: [
    {
      depthRange: '0-20cm',
      sandPercent: 55,
      siltPercent: 30,
      clayPercent: 15,
      classification: 'Sandy Loam',
      color: '#F4D03F',
    },
    {
      depthRange: '20-50cm',
      sandPercent: 45,
      siltPercent: 35,
      clayPercent: 20,
      classification: 'Sandy Clay Loam',
      color: '#D9A23A',
    },
    {
      depthRange: '50-100cm',
      sandPercent: 30,
      siltPercent: 35,
      clayPercent: 35,
      classification: 'Clay Loam',
      color: '#BD7434',
    },
    {
      depthRange: '100-200cm',
      sandPercent: 20,
      siltPercent: 30,
      clayPercent: 50,
      classification: 'Clay',
      color: '#C0392B',
    },
  ],
  heatmapUrls: {
    '0-20cm': 'https://b.basemaps.cartocdn.com/light_only_labels/{z}/{x}/{y}.png',
    '20-50cm': 'https://b.basemaps.cartocdn.com/dark_only_labels/{z}/{x}/{y}.png',
    '50-100cm': 'https://b.basemaps.cartocdn.com/light_only_labels/{z}/{x}/{y}.png',
    '100-200cm': 'https://b.basemaps.cartocdn.com/dark_only_labels/{z}/{x}/{y}.png',
  },
  // Colors match the AI engine's tile palette (sand/silt/clay) so the legend
  // stays accurate once real S3 heatmap tiles are wired in.
  heatmapLegend: [
    { color: '#F4D03F', label: 'Sand' },
    { color: '#A0522D', label: 'Silt' },
    { color: '#C0392B', label: 'Clay' },
  ],
  composition: [
    { type: 'Clay', percent: 45, color: '#C0392B' },
    { type: 'Silt', percent: 35, color: '#A0522D' },
    { type: 'Sand', percent: 20, color: '#F4D03F' },
  ],
  soilCompositionGeoJSON: {
    type: 'FeatureCollection',
    features: [
      {
        type: 'Feature',
        properties: { type: 'Clay' },
        geometry: {
          type: 'Polygon',
          coordinates: [
            [
              [31.9418, 30.633],
              [31.942, 30.633],
              [31.942, 30.6332],
              [31.9418, 30.6332],
              [31.9418, 30.633],
            ],
          ],
        },
      },
      {
        type: 'Feature',
        properties: { type: 'Silt' },
        geometry: {
          type: 'Polygon',
          coordinates: [
            [
              [31.942, 30.633],
              [31.9422, 30.633],
              [31.9422, 30.6332],
              [31.942, 30.6332],
              [31.942, 30.633],
            ],
          ],
        },
      },
      {
        type: 'Feature',
        properties: { type: 'Sand' },
        geometry: {
          type: 'Polygon',
          coordinates: [
            [
              [31.9418, 30.6328],
              [31.9422, 30.6328],
              [31.9422, 30.633],
              [31.9418, 30.633],
              [31.9418, 30.6328],
            ],
          ],
        },
      },
    ],
  },
};

// ---------- Bearing mock ----------
// TODO(backend integration): replace with BearingController response (US32-35).
// floorCountCategory / maxFloorsWithoutDeepFoundation / foundationType / uncertaintyRangeKpa
// are expected to come directly from the .NET API once wired in.
const BEARING_CAPACITY = 245;

export const MOCK_BEARING_DATA: BearingData = {
  bearingCapacity: BEARING_CAPACITY,
  uncertaintyRangeKpa: {
    min: Math.round(BEARING_CAPACITY * 0.7),
    max: Math.round(BEARING_CAPACITY * 1.3),
  },
  capacityClass: 'High',
  isUnreliableEstimate: false,

  floorCountCategory: '6-10 floors',
  maxFloorsWithoutDeepFoundation: 10,
  foundationType: 'Shallow',

  factors: {
    clayPercent: {
      value: 45,
      unit: '%',
      safeThreshold: 50,
      source: 'Soil module',
      tooltip: 'Higher clay content reduces drainage and can lower long-term bearing capacity.',
    },
    sandPercent: {
      value: 20,
      unit: '%',
      safeThreshold: 60,
      source: 'Soil module',
      tooltip: 'Sand improves load-bearing strength but offers less cohesion than clay.',
    },
    moistureIndex: {
      value: 0.32,
      unit: '',
      safeThreshold: 0.4,
      source: 'Sentinel-2 NDMI',
      tooltip:
        'Elevated soil moisture can reduce effective bearing capacity and increase settlement risk.',
    },
    waterTableDepth: {
      value: 8.5,
      unit: 'm',
      safeThreshold: 5,
      source: 'SoilGrids',
      tooltip:
        'A shallower water table increases the risk of liquefaction and reduces usable bearing capacity.',
    },
    terrainSlope: {
      value: 3.2,
      unit: '%',
      safeThreshold: 5,
      source: 'Topography module',
      tooltip: 'Steeper terrain increases grading costs and can affect foundation uniformity.',
    },
  },

  // TODO(structural team): placeholder loads only — typical RC structure ~10-12 kPa/floor.
  buildingLoadReferences: [
    { floorCategory: '1-2 floors', typicalLoadKpa: 11, supported: true },
    { floorCategory: '3-5 floors', typicalLoadKpa: 11, supported: true },
    { floorCategory: '6-10 floors', typicalLoadKpa: 11, supported: BEARING_CAPACITY >= 11 * 10 },
    { floorCategory: '10+ floors', typicalLoadKpa: 11, supported: BEARING_CAPACITY >= 11 * 12 },
  ],

  bearingPoints: [
    { lng: 31.942, lat: 30.6331, capacity: 245, depth: 5 },
    { lng: 31.9419, lat: 30.633, capacity: 210, depth: 4 },
    { lng: 31.9421, lat: 30.6329, capacity: 270, depth: 6 },
  ],
  waterTableLines: (() => {
    const waterLines: any[] = [];
    for (let d = 3; d <= 8; d += 2) {
      const coords: [number, number][] = [];
      for (let a = 0; a <= 360; a += 30) {
        const rad = (a * Math.PI) / 180;
        coords.push([31.942 + 0.0001 * d * Math.cos(rad), 30.633 + 0.0001 * d * Math.sin(rad)]);
      }
      waterLines.push({
        type: 'Feature',
        properties: { depth: d },
        geometry: { type: 'LineString', coordinates: coords },
      });
    }
    return waterLines;
  })(),
};

// ---------- Risk mock ----------
export const MOCK_RISK_DATA: RiskData = {
  overallRiskScore: 42,
  overallRiskLevel: 'Moderate',
  benchmarkComparison: 'Lower than 65% of sites in Nile Delta region',

  floodRisk: {
    score: 35,
    level: 'Low',
    icon: 'pi pi-cloud-download',
    color: '#3B82F6',
    factors: [
      { label: 'Terrain slope', detail: 'Average slope 3.2% — moderate drainage' },
      { label: 'TWI index', detail: 'Topographic Wetness Index: 4.8 (moderate)' },
      { label: 'Proximity to drainage', detail: '500m from nearest drainage basin' },
    ],
    mitigation: 'Install drainage systems, elevate foundation above flood level',
  },
  seismicRisk: {
    score: 28,
    level: 'Low',
    icon: 'pi pi-map',
    color: '#F59E0B',
    factors: [
      { label: 'NCSR zone', detail: 'Zone II — moderate shaking expected' },
      { label: 'Fault line distance', detail: '25 km from nearest active fault' },
      { label: 'Soil amplification', detail: 'Sandy loam — moderate amplification potential' },
    ],
    mitigation: 'Design for seismic loads per ECP 201, consider base isolation',
  },

  expansiveSoilRisk: {
    score: 55,
    level: 'Moderate',
    icon: 'pi pi-arrows-v',
    color: '#A0522D',
    factors: [
      { label: 'Clay content', detail: '45% clay in top 1m' },
      { label: 'Shrink-swell potential', detail: 'Medium — montmorillonite traces' },
    ],
    mitigation: 'Replace top 1.5m with non-expansive fill, use raft foundation',
  },

  liquefactionRisk: {
    score: 68,
    level: 'High',
    icon: 'pi pi-exclamation-triangle',
    color: '#8B5CF6',
    factors: [
      { label: 'Sand content', detail: '20% sand in top 1m' },
      { label: 'Water table depth', detail: '8.5m — moderate risk' },
      { label: 'Seismic zone', detail: 'Zone II — moderate shaking' },
    ],
    mitigation: 'Deep pile foundations to bearing stratum, soil densification',
  },

  mitigations: [
    {
      risk: 'High Flood',
      suggestion: 'Install drainage systems, elevate foundation above flood level',
      costImpact: 'Medium',
      feasibility: 'High',
    },
    {
      risk: 'High Seismic',
      suggestion: 'Design for seismic loads per ECP 201, consider base isolation',
      costImpact: 'High',
      feasibility: 'Medium',
    },
    {
      risk: 'High Expansive Soil',
      suggestion: 'Replace top 1.5m with non-expansive fill, use raft foundation',
      costImpact: 'Medium',
      feasibility: 'High',
    },
    {
      risk: 'High Liquefaction',
      suggestion: 'Deep pile foundations to bearing stratum, soil densification',
      costImpact: 'High',
      feasibility: 'Medium',
    },
  ],
  floodFeatures: [
    {
      risk: 'High',
      coords: [
        [31.9418, 30.633],
        [31.942, 30.633],
        [31.942, 30.6331],
        [31.9418, 30.6331],
        [31.9418, 30.633],
      ],
    },
    {
      risk: 'Medium',
      coords: [
        [31.942, 30.633],
        [31.9422, 30.633],
        [31.9422, 30.6331],
        [31.942, 30.6331],
        [31.942, 30.633],
      ],
    },
  ],
  seismicZonesGeoJSON: {
    type: 'FeatureCollection',
    features: [
      {
        type: 'Feature',
        properties: { classification: 'Zone II' },
        geometry: { type: 'Point', coordinates: [31.942, 30.633] },
      },
    ],
  },
  expansiveSoilZonesGeoJSON: {
    type: 'FeatureCollection',
    features: [
      {
        type: 'Feature',
        properties: { clayPercent: 45 },
        geometry: {
          type: 'Polygon',
          coordinates: [
            [
              [31.9418, 30.6328],
              [31.9422, 30.6328],
              [31.9422, 30.6332],
              [31.9418, 30.6332],
              [31.9418, 30.6328],
            ],
          ],
        },
      },
    ],
  },
  liquefactionZonesGeoJSON: {
    type: 'FeatureCollection',
    features: [
      {
        type: 'Feature',
        properties: { risk: 'High' },
        geometry: {
          type: 'Polygon',
          coordinates: [
            [
              [31.9419, 30.6329],
              [31.9421, 30.6329],
              [31.9421, 30.6331],
              [31.9419, 30.6331],
              [31.9419, 30.6329],
            ],
          ],
        },
      },
    ],
  },
};

// ---------- Borehole mock ----------
export const MOCK_BOREHOLE_DATA: BoreholeData = {
  points: [
    { id: 'BH-1', lng: 31.942, lat: 30.633, depth: 15, cost: 4500 },
    { id: 'BH-2', lng: 31.9419, lat: 30.6329, depth: 12, cost: 3800 },
    { id: 'BH-3', lng: 31.9421, lat: 30.6331, depth: 18, cost: 5000 },
  ],
  optimalArea: [
    [31.9418, 30.6328],
    [31.9422, 30.6328],
    [31.9422, 30.6332],
    [31.9418, 30.6332],
    [31.9418, 30.6328],
  ],
  estimatedCost: 13300,
  drillingCost: 9000,
  testingCost: 4300,
};
