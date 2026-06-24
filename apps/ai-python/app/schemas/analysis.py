"""
schemas/analysis.py — GeoSense AI
Pydantic models for POST /api/v1/analysis/jobs (submit + poll).

Matches the PlanoraAI Service API Contract (MVP) exactly.
"""

from __future__ import annotations

from datetime import datetime
from enum import Enum
from typing import Any, Optional

from pydantic import BaseModel, Field


# ═══════════════════════════════════════════════════════════════════════════
# ENUMS
# ═══════════════════════════════════════════════════════════════════════════

class JobStatus(str, Enum):
    QUEUED      = "Queued"
    PROCESSING  = "Processing"
    COMPLETED   = "Completed"
    FAILED      = "Failed"


class RiskLevel(str, Enum):
    LOW      = "Low"
    MODERATE = "Moderate"
    MEDIUM   = "Medium"
    HIGH     = "High"


class BearingClass(str, Enum):
    LOW    = "Low"
    MEDIUM = "Medium"
    HIGH   = "High"


class TrafficLight(str, Enum):
    GREEN  = "green"
    YELLOW = "yellow"
    RED    = "red"


class BoreholePriority(str, Enum):
    CRITICAL = "Critical"
    HIGH     = "High"
    MEDIUM   = "Medium"
    LOW      = "Low"


# ═══════════════════════════════════════════════════════════════════════════
# REQUEST MODELS
# ═══════════════════════════════════════════════════════════════════════════

class ParcelInfo(BaseModel):
    name:    str   = Field(..., example="New Administrative Capital Parcel A")
    area_m2: float = Field(..., alias="areaM2", example=84231.15)

    model_config = {"populate_by_name": True}


class BoundingBox(BaseModel):
    min_x: float = Field(..., alias="minX")
    min_y: float = Field(..., alias="minY")
    max_x: float = Field(..., alias="maxX")
    max_y: float = Field(..., alias="maxY")

    model_config = {"populate_by_name": True}


class GeoJSONGeometry(BaseModel):
    type:        str             = Field(..., example="Polygon")
    coordinates: list[Any]       = Field(...)


class AnalysisOptions(BaseModel):
    include_topography: bool        = Field(True,  alias="includeTopography")
    include_soil:       bool        = Field(True,  alias="includeSoil")
    include_bearing:    bool        = Field(True,  alias="includeBearing")
    include_risk:       bool        = Field(True,  alias="includeRisk")
    include_borehole:   bool        = Field(True,  alias="includeBorehole")
    contour_interval:   float       = Field(0.5,   alias="contourInterval")
    slope_categories:   list[float] = Field([2, 5, 15], alias="slopeCategories")
    reference_plane:    str         = Field("auto", alias="referencePlane")
    soil_depths:        list[str]   = Field(
        ["0-20cm", "20-50cm", "50-100cm", "100-200cm"],
        alias="soilDepths",
    )

    model_config = {"populate_by_name": True}


class AnalysisJobRequest(BaseModel):
    """POST /api/v1/analysis/jobs — request body."""
    job_id:           str             = Field(..., alias="jobId")
    parcel_id:        str             = Field(..., alias="parcelId")
    parcel:           ParcelInfo
    bounding_box:     BoundingBox     = Field(..., alias="boundingBox")
    geometry:         GeoJSONGeometry
    analysis_options: AnalysisOptions = Field(
        default_factory=AnalysisOptions,
        alias="analysisOptions",
    )

    model_config = {"populate_by_name": True}


# ═══════════════════════════════════════════════════════════════════════════
# RESPONSE — 202 Accepted (job queued)
# ═══════════════════════════════════════════════════════════════════════════

class JobAcceptedData(BaseModel):
    python_job_id:      str      = Field(..., alias="pythonJobId")
    backend_job_id:     str      = Field(..., alias="backendJobId")
    parcel_id:          str      = Field(..., alias="parcelId")
    status:             JobStatus
    accepted_at:        datetime = Field(..., alias="acceptedAt")
    estimated_duration: str      = Field("2-6 minutes", alias="estimatedDuration")

    model_config = {"populate_by_name": True}


class JobAcceptedResponse(BaseModel):
    success: bool        = True
    message: str         = "Analysis job accepted and queued for processing."
    data:    JobAcceptedData


# ═══════════════════════════════════════════════════════════════════════════
# RESPONSE — GET poll while processing
# ═══════════════════════════════════════════════════════════════════════════

class JobProgressData(BaseModel):
    python_job_id:              str      = Field(..., alias="pythonJobId")
    backend_job_id:             str      = Field(..., alias="backendJobId")
    parcel_id:                  str      = Field(..., alias="parcelId")
    status:                     JobStatus
    progress_percentage:        int      = Field(..., alias="progressPercentage")
    current_stage:              str      = Field(..., alias="currentStage")
    stage_details:              str      = Field(..., alias="stageDetails")
    started_at:                 datetime = Field(..., alias="startedAt")
    estimated_remaining_minutes: int     = Field(..., alias="estimatedRemainingMinutes")

    model_config = {"populate_by_name": True}


class JobProgressResponse(BaseModel):
    success: bool            = True
    message: str             = "Analysis is actively processing."
    data:    JobProgressData


# ═══════════════════════════════════════════════════════════════════════════
# RESULT SUB-MODELS — Topography
# ═══════════════════════════════════════════════════════════════════════════

class ElevationResult(BaseModel):
    minimum_meters: float = Field(..., alias="minimumMeters")
    maximum_meters: float = Field(..., alias="maximumMeters")
    average_meters: float = Field(..., alias="averageMeters")
    unit:           str   = "m"

    model_config = {"populate_by_name": True}


class SlopeZone(BaseModel):
    range:      str   = Field(...)   # e.g. "0-2%"
    percentage: float


class CutFillAnalysis(BaseModel):
    cut_volume_m3:  float = Field(..., alias="cutVolumeM3")
    fill_volume_m3: float = Field(..., alias="fillVolumeM3")
    net_volume_m3:  float = Field(..., alias="netVolumeM3")
    unit:           str   = "m3"

    model_config = {"populate_by_name": True}


class PondingRisk(BaseModel):
    risk_level:       RiskLevel = Field(..., alias="riskLevel")
    zones_count:      int       = Field(..., alias="zonesCount")
    affected_area_m2: float     = Field(..., alias="affectedAreaM2")
    unit:             str       = "m2"

    model_config = {"populate_by_name": True}


class TopographyAssets(BaseModel):
    elevation_tile_url:   str = Field(..., alias="elevationTileUrl")
    slope_tile_url:       str = Field(..., alias="slopeTileUrl")
    contour_geo_json_url: str = Field(..., alias="contourGeoJsonUrl")
    ponding_geo_json_url: str = Field(..., alias="pondingGeoJsonUrl")
    dem_raster_url:       str = Field(..., alias="demRasterUrl")
    slope_raster_url:     str = Field(..., alias="slopeRasterUrl")

    model_config = {"populate_by_name": True}


class TopographyMetadata(BaseModel):
    copernicus_dem_version:  str = Field("COP-30",      alias="copernicusDemVersion")
    pixel_resolution_meters: int = Field(30,            alias="pixelResolutionMeters")
    crs:                     str = Field("EPSG:32636",  alias="crs")
    processing_time_seconds: int = Field(...,           alias="processingTimeSeconds")

    model_config = {"populate_by_name": True}


class TopographyResult(BaseModel):
    elevation:            ElevationResult
    slope_distribution:   list[SlopeZone]    = Field(..., alias="slopeDistribution")
    cut_fill_analysis:    CutFillAnalysis     = Field(..., alias="cutFillAnalysis")
    ponding_risk:         PondingRisk         = Field(..., alias="pondingRisk")
    visualization_assets: TopographyAssets   = Field(..., alias="visualizationAssets")
    metadata:             TopographyMetadata

    model_config = {"populate_by_name": True}


# ═══════════════════════════════════════════════════════════════════════════
# RESULT SUB-MODELS — Soil
# ═══════════════════════════════════════════════════════════════════════════

class SoilClassification(BaseModel):
    primary_type:   str   = Field(..., alias="primaryType")
    usda_class:     str   = Field(..., alias="usdaClass")
    ai_confidence:  float = Field(..., alias="aiConfidence")

    model_config = {"populate_by_name": True}


class SurfaceComposition(BaseModel):
    sand_percentage: float = Field(..., alias="sandPercentage")
    silt_percentage: float = Field(..., alias="siltPercentage")
    clay_percentage: float = Field(..., alias="clayPercentage")
    unit:            str   = "%"

    model_config = {"populate_by_name": True}


class SoilProperties(BaseModel):
    bulk_density:              float = Field(..., alias="bulkDensity")
    bulk_density_unit:         str   = Field("g/cm3", alias="bulkDensityUnit")
    organic_carbon_percentage: float = Field(..., alias="organicCarbonPercentage")
    ph:                        float
    cec:                       float
    water_table_depth_meters:  float = Field(..., alias="waterTableDepthMeters")

    model_config = {"populate_by_name": True}


class SoilDepthLayer(BaseModel):
    depth:        str   = Field(...)    # e.g. "0-20cm"
    sand:         float
    silt:         float
    clay:         float
    soil_type:    str   = Field(..., alias="soilType")
    bulk_density: float = Field(..., alias="bulkDensity")

    model_config = {"populate_by_name": True}


class SoilAssets(BaseModel):
    soil_heatmap_tile_url:  str = Field(..., alias="soilHeatmapTileUrl")
    soil_type_geo_json_url: str = Field(..., alias="soilTypeGeoJsonUrl")
    depth_profile_image_url: str = Field(..., alias="depthProfileImageUrl")

    model_config = {"populate_by_name": True}


class SpectralIndices(BaseModel):
    ndvi_mean: float = Field(..., alias="ndviMean")
    bsi_mean:  float = Field(..., alias="bsiMean")
    ndmi_mean: float = Field(..., alias="ndmiMean")

    model_config = {"populate_by_name": True}


class SoilResult(BaseModel):
    classification:       SoilClassification
    surface_composition:  SurfaceComposition  = Field(..., alias="surfaceComposition")
    properties:           SoilProperties
    depth_layers:         list[SoilDepthLayer] = Field(..., alias="depthLayers")
    visualization_assets: SoilAssets           = Field(..., alias="visualizationAssets")
    data_sources:         list[str]             = Field(..., alias="dataSources")
    spectral_indices:     SpectralIndices       = Field(..., alias="spectralIndices")

    model_config = {"populate_by_name": True}


# ═══════════════════════════════════════════════════════════════════════════
# RESULT SUB-MODELS — Bearing Capacity
# ═══════════════════════════════════════════════════════════════════════════

class UncertaintyRange(BaseModel):
    minimum_kpa: float = Field(..., alias="minimumKpa")
    maximum_kpa: float = Field(..., alias="maximumKpa")

    model_config = {"populate_by_name": True}


class FeatureImportance(BaseModel):
    feature: str
    weight:  float


class BearingSoilFactors(BaseModel):
    clay_content:              float = Field(..., alias="clayContent")
    sand_content:              float = Field(..., alias="sandContent")
    moisture_index:            float = Field(..., alias="moistureIndex")
    depth_to_water_table_meters: float = Field(..., alias="depthToWaterTableMeters")
    terrain_slope_percent:     float = Field(..., alias="terrainSlopePercent")

    model_config = {"populate_by_name": True}


class BearingModelMetadata(BaseModel):
    model_name:   str   = Field("BearingCapacityXGB-v1.0", alias="modelName")
    framework:    str   = Field("XGBoost",                 alias="framework")
    training_r2:  float = Field(0.8388,                    alias="trainingR2")
    shap_enabled: bool  = Field(True,                      alias="shapEnabled")

    model_config = {"populate_by_name": True}


class BearingResult(BaseModel):
    bearing_capacity_kpa:           float               = Field(..., alias="bearingCapacityKpa")
    confidence:                     float
    classification:                 BearingClass
    range:                          str                 # e.g. "75-200 kPa"
    traffic_light:                  TrafficLight        = Field(..., alias="trafficLight")
    recommended_foundation:         str                 = Field(..., alias="recommendedFoundation")
    max_floors_without_deep_foundation: int             = Field(..., alias="maxFloorsWithoutDeepFoundation")
    floor_count_category:           str                 = Field(..., alias="floorCountCategory")
    uncertainty_range:              UncertaintyRange    = Field(..., alias="uncertaintyRange")
    feature_importance:             list[FeatureImportance] = Field(..., alias="featureImportance")
    soil_factors:                   BearingSoilFactors  = Field(..., alias="soilFactors")
    disclaimer:                     str
    model_metadata:                 BearingModelMetadata = Field(..., alias="modelMetadata")

    model_config = {"populate_by_name": True}


# ═══════════════════════════════════════════════════════════════════════════
# RESULT SUB-MODELS — Risk
# ═══════════════════════════════════════════════════════════════════════════

class FloodRiskDetail(BaseModel):
    score:             int
    level:             RiskLevel
    weight:            float       = 0.25
    factors:           list[str]
    zones_geo_json_url: str        = Field(..., alias="zonesGeoJsonUrl")

    model_config = {"populate_by_name": True}


class SeismicRiskDetail(BaseModel):
    score:   int
    level:   RiskLevel
    weight:  float    = 0.20
    factors: list[str]
    zone:    str
    source:  str      = "USGS / Egypt NCSR"


class ExpansiveSoilDetail(BaseModel):
    score:                   int
    level:                   RiskLevel
    weight:                  float = 0.30
    factors:                 list[str]
    replacement_depth_meters: float = Field(..., alias="replacementDepthMeters")

    model_config = {"populate_by_name": True}


class LiquefactionDetail(BaseModel):
    score:           int
    level:           RiskLevel
    weight:          float = 0.25
    factors:         list[str]
    susceptibility:  str
    methodology:     str  = "Idriss & Boulanger (2008) adapted for Egypt"


class RiskBreakdown(BaseModel):
    flood:          FloodRiskDetail      = Field(...)
    seismic:        SeismicRiskDetail    = Field(...)
    expansive_soil: ExpansiveSoilDetail  = Field(..., alias="expansiveSoil")
    liquefaction:   LiquefactionDetail   = Field(...)

    model_config = {"populate_by_name": True}


class MitigationSuggestion(BaseModel):
    risk_type:   str  = Field(..., alias="riskType")
    suggestion:  str
    cost_impact: str  = Field(..., alias="costImpact")
    feasibility: str

    model_config = {"populate_by_name": True}


class RiskAssets(BaseModel):
    flood_risk_zones_geo_json_url: str = Field(..., alias="floodRiskZonesGeoJsonUrl")
    risk_heatmap_tile_url:         str = Field(..., alias="riskHeatmapTileUrl")

    model_config = {"populate_by_name": True}


class RiskResult(BaseModel):
    overall_score:        int                    = Field(..., alias="overallScore")
    overall_risk_level:   RiskLevel              = Field(..., alias="overallRiskLevel")
    max_score:            int                    = Field(100, alias="maxScore")
    risk_breakdown:       RiskBreakdown          = Field(..., alias="riskBreakdown")
    mitigation_suggestions: list[MitigationSuggestion] = Field(..., alias="mitigationSuggestions")
    visualization_assets: RiskAssets             = Field(..., alias="visualizationAssets")

    model_config = {"populate_by_name": True}


# ═══════════════════════════════════════════════════════════════════════════
# RESULT SUB-MODELS — Borehole
# ═══════════════════════════════════════════════════════════════════════════

class BoreholeRecommendation(BaseModel):
    minimum_required:    int   = Field(..., alias="minimumRequired")
    optimal_count:       int   = Field(..., alias="optimalCount")
    coverage_percentage: float = Field(..., alias="coveragePercentage")
    grid_size:           str   = Field(..., alias="gridSize")
    strategy:            str

    model_config = {"populate_by_name": True}


class BoreholePlacementPoint(BaseModel):
    id:                      str             = Field(...)
    latitude:                float
    longitude:               float
    priority:                BoreholePriority
    reason:                  str
    estimated_depth_meters:  int             = Field(..., alias="estimatedDepthMeters")

    model_config = {"populate_by_name": True}


class BoreholeCostOption(BaseModel):
    boreholes:      int
    estimated_cost: int   = Field(..., alias="estimatedCost")
    currency:       str   = "EGP"
    basis:          Optional[str] = None

    model_config = {"populate_by_name": True}


class BoreholeSavings(BaseModel):
    amount:     int
    currency:   str   = "EGP"
    percentage: float


class BoreholeCostAnalysis(BaseModel):
    traditional_approach: BoreholeCostOption = Field(..., alias="traditionalApproach")
    optimized_approach:   BoreholeCostOption = Field(..., alias="optimizedApproach")
    savings:              BoreholeSavings

    model_config = {"populate_by_name": True}


class BoreholeAssets(BaseModel):
    borehole_points_geo_json_url: str = Field(..., alias="boreholePointsGeoJsonUrl")

    model_config = {"populate_by_name": True}


class BoreholeResult(BaseModel):
    recommendation:       BoreholeRecommendation
    placement_points:     list[BoreholePlacementPoint] = Field(..., alias="placementPoints")
    cost_analysis:        BoreholeCostAnalysis          = Field(..., alias="costAnalysis")
    visualization_assets: BoreholeAssets                = Field(..., alias="visualizationAssets")

    model_config = {"populate_by_name": True}


# ═══════════════════════════════════════════════════════════════════════════
# FULL RESULT + COMPLETED RESPONSE
# ═══════════════════════════════════════════════════════════════════════════

class AnalysisResult(BaseModel):
    topography: Optional[TopographyResult] = None
    soil:       Optional[SoilResult]       = None
    bearing:    Optional[BearingResult]    = None
    risk:       Optional[RiskResult]       = None
    borehole:   Optional[BoreholeResult]   = None


class JobCompletedData(BaseModel):
    python_job_id:           str           = Field(..., alias="pythonJobId")
    backend_job_id:          str           = Field(..., alias="backendJobId")
    parcel_id:               str           = Field(..., alias="parcelId")
    status:                  JobStatus     = JobStatus.COMPLETED
    started_at:              datetime      = Field(..., alias="startedAt")
    completed_at:            datetime      = Field(..., alias="completedAt")
    processing_time_seconds: int           = Field(..., alias="processingTimeSeconds")
    result:                  AnalysisResult

    model_config = {"populate_by_name": True}


class JobCompletedResponse(BaseModel):
    success: bool             = True
    message: str              = "Analysis completed successfully."
    data:    JobCompletedData


# ═══════════════════════════════════════════════════════════════════════════
# ERROR RESPONSE
# ═══════════════════════════════════════════════════════════════════════════

class ErrorDetail(BaseModel):
    code:        str
    description: str


class JobFailedResponse(BaseModel):
    success: bool             = False
    message: str              = "Analysis failed during processing."
    errors:  list[ErrorDetail]


# ═══════════════════════════════════════════════════════════════════════════
# UNION RESPONSE TYPE (for GET /jobs/{id})
# ═══════════════════════════════════════════════════════════════════════════

# Used as the return type annotation in the router:
#   Union[JobProgressResponse, JobCompletedResponse, JobFailedResponse]