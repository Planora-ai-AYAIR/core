"""Client-facing API schemas — API Contract §2.

These models describe the *.NET backend* surface from the contract. They are
implemented here in the Python AI engine so the §2 endpoints are browsable and
testable end-to-end (see ``app/routers/client``). Request models are kept
permissive where the contract mandates specific 400 error codes — that
validation happens in the routers so we can emit the contracted code/field.

Grouped in one module (mirrors how the §2 sections share the unified envelope);
the routers stay split 1:1 with the PDF sections.
"""

from __future__ import annotations

from enum import Enum
from typing import Optional

from pydantic import BaseModel, Field

from app.schemas.common import BoundingBox, GeoJsonPolygon, JobStatus, FoundationType


# ── Shared request pieces ─────────────────────────────────────
class AreaUnit(str, Enum):
    M2 = "m2"
    HECTARES = "hectares"
    ACRES = "acres"


class ParcelCreateRequest(BaseModel):
    """POST /api/parcels (§2.1.1).

    ``areaUnit`` is a free string (not the ``AreaUnit`` enum) so the router can
    return the contracted ``INVALID_UNIT`` 400 rather than a generic 422.
    """
    clientName: str
    geoJson: GeoJsonPolygon
    area: float
    areaUnit: str = "m2"


class _ClientJobBase(BaseModel):
    parcelId: str


class TopographyOptions(BaseModel):
    contourInterval: float = 0.5
    slopeCategories: list[int] = Field(default=[2, 5, 15])
    generateCutFill: bool = True
    referencePlane: str = "auto"


class TopographyJobSubmit(_ClientJobBase):
    options: TopographyOptions = TopographyOptions()


class SoilOptions(BaseModel):
    depths: list[str] = ["0-20cm", "20-50cm", "50-100cm", "100-200cm"]
    includeHeatmap: bool = True


class SoilJobSubmit(_ClientJobBase):
    options: SoilOptions = SoilOptions()


class BearingJobSubmit(_ClientJobBase):
    foundationType: FoundationType = FoundationType.SHALLOW


class RiskJobSubmit(_ClientJobBase):
    riskTypes: list[str] = ["flood", "seismic", "expansiveSoil", "liquefaction"]


class BoreholeParameters(BaseModel):
    maxSpacing: int = 30
    minBoreholes: int = 12
    targetDepth: int = 20
    unit: str = "m"


class BoreholeJobSubmit(_ClientJobBase):
    parameters: BoreholeParameters = BoreholeParameters()


class ReportOptions(BaseModel):
    language: str = "en"
    includeMaps: bool = True
    includeTables: bool = True
    includeRiskMatrix: bool = True
    disclaimerLevel: str = "full"
    companyName: Optional[str] = None
    projectName: Optional[str] = None


class ReportJobSubmit(_ClientJobBase):
    reportOptions: ReportOptions = ReportOptions()


# ── Response data: parcels & jobs ─────────────────────────────
class ParcelCreatedData(BaseModel):
    """§2.1.1 response (201)."""
    parcelId: str
    boundingBox: BoundingBox
    area: float
    createdAt: str


class ParcelData(BaseModel):
    """§2.1.2 response (200)."""
    parcelId: str
    clientName: str
    area: float
    status: str
    modulesCompleted: list[str] = []
    boundingBox: BoundingBox
    createdAt: str
    completedAt: Optional[str] = None


class JobQueued(BaseModel):
    """Async job accepted payload (§1.3 / all §2 POST /jobs)."""
    jobId: str
    parcelId: str
    status: JobStatus = JobStatus.QUEUED
    estimatedDuration: str = "2-6 hours"


class JobStatusData(BaseModel):
    """§2.8 GET /api/jobs/{jobId}/status."""
    jobId: str
    parcelId: str
    module: str
    status: JobStatus
    progressPercentage: int = 0
    startedAt: Optional[str] = None
    completedAt: Optional[str] = None
    nextModule: Optional[str] = None
    message: Optional[str] = None


# ── Response data: §2.2 Topography results ───────────────────
class ElevationStats(BaseModel):
    min: float
    max: float
    mean: float
    unit: str = "m"


class SlopeDistItem(BaseModel):
    category: str
    range: str
    percentage: float
    color: str


class SlopeAnalysis(BaseModel):
    distribution: list[SlopeDistItem]


class CutFillVolume(BaseModel):
    cutVolume: float
    fillVolume: float
    netVolume: float
    unit: str = "m³"


class ContourLines(BaseModel):
    geoJsonUrl: str
    interval: float


class PondingRisk(BaseModel):
    zonesCount: int
    totalArea: float
    unit: str = "m²"
    geoJsonUrl: str


class RasterTiles(BaseModel):
    elevation: Optional[str] = None
    slope: Optional[str] = None


class TopographyClientResult(BaseModel):
    parcelId: str
    elevation: ElevationStats
    slopeAnalysis: SlopeAnalysis
    cutFill: Optional[CutFillVolume] = None
    contourLines: Optional[ContourLines] = None
    pondingRisk: Optional[PondingRisk] = None
    rasterTiles: Optional[RasterTiles] = None
    generatedAt: str


# ── Response data: §2.3 Soil results ─────────────────────────
class SoilComposition(BaseModel):
    sand: float
    silt: float
    clay: float
    unit: str = "%"


class SoilPropertiesClient(BaseModel):
    bulkDensity: float
    bulkDensityUnit: str = "g/cm³"
    organicCarbon: float
    organicCarbonUnit: str = "%"
    ph: float


class SoilClassificationClient(BaseModel):
    primaryType: str
    usdaClass: str
    aiConfidence: float


class MultiDepthItem(BaseModel):
    depth: str
    sand: float
    clay: float
    type: str


class SoilClientResult(BaseModel):
    parcelId: str
    depth: str
    composition: SoilComposition
    properties: SoilPropertiesClient
    classification: SoilClassificationClient
    multiDepthProfile: list[MultiDepthItem]
    heatmapTileUrl: Optional[str] = None
    generatedAt: str


# ── Response data: §2.4 Bearing results ──────────────────────
class BearingCapacityClient(BaseModel):
    value: float
    unit: str = "kPa"
    category: str
    range: str
    trafficLight: str


class FoundationRecommendation(BaseModel):
    type: str
    suitable: bool
    maxFloorsWithoutDeepFoundation: int
    floorCountCategory: str


class BearingSoilFactors(BaseModel):
    clayContent: float
    sandContent: float
    moistureIndex: float
    depthToWaterTable: float


class BearingClientResult(BaseModel):
    parcelId: str
    bearingCapacity: BearingCapacityClient
    foundationRecommendation: FoundationRecommendation
    soilFactors: BearingSoilFactors
    disclaimer: str
    generatedAt: str


# ── Response data: §2.5 Risk results ─────────────────────────
class RiskBreakdownItem(BaseModel):
    score: int
    level: str
    factors: list[str] = []
    geoJsonUrl: Optional[str] = None
    source: Optional[str] = None
    replacementDepth: Optional[float] = None
    susceptibility: Optional[str] = None


class RiskBreakdown(BaseModel):
    flood: Optional[RiskBreakdownItem] = None
    seismic: Optional[RiskBreakdownItem] = None
    expansiveSoil: Optional[RiskBreakdownItem] = None
    liquefaction: Optional[RiskBreakdownItem] = None


class RiskClientResult(BaseModel):
    parcelId: str
    overallScore: int
    overallRiskLevel: str
    maxScore: int = 100
    riskBreakdown: RiskBreakdown
    generatedAt: str


# ── Response data: §2.6 Borehole results ─────────────────────
class BoreholeRecommendation(BaseModel):
    minimumRequired: int
    optimalCount: int
    coveragePercentage: int
    gridSize: str


class PlacementPointClient(BaseModel):
    id: str
    latitude: float
    longitude: float
    priority: str
    reason: str
    estimatedDepth: int


class BoreholePlacement(BaseModel):
    strategy: str
    points: list[PlacementPointClient]
    geoJsonUrl: Optional[str] = None


class CostApproachClient(BaseModel):
    boreholes: int
    estimatedCost: int
    currency: str = "EGP"


class CostSavings(BaseModel):
    amount: int
    currency: str = "EGP"
    percentage: int


class CostAnalysis(BaseModel):
    traditionalApproach: CostApproachClient
    optimizedApproach: CostApproachClient
    savings: CostSavings


class BoreholeClientResult(BaseModel):
    parcelId: str
    recommendation: BoreholeRecommendation
    placement: BoreholePlacement
    costAnalysis: CostAnalysis
    generatedAt: str
