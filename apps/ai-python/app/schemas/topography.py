"""Topography module schemas — API Contract §3.1."""

from __future__ import annotations
from typing import Optional
from pydantic import BaseModel, Field

from app.schemas.common import JobStatus, BaseJobRequest


# ── Internal options (used by terrain_service) ───────────────
class TopographyOptions(BaseModel):
    contour_interval_m: float = 0.5
    reference_elevation: Optional[float] = None
    twi_threshold: float = 8.0
    export_tiles: bool = True


# ── Contract-aligned options ─────────────────────────────────
class TopographyContractOptions(BaseModel):
    """Options from §3.1.1 request payload."""
    contourInterval: float = 0.5
    slopeCategories: list[int] = Field(default=[2, 5, 15])
    generateCutFill: bool = True
    referencePlane: str = "auto"


# ── POST /api/v1/topography/jobs — Request (§3.1.1) ──────────
class TopographyJobRequest(BaseJobRequest):
    options: TopographyContractOptions = TopographyContractOptions()


# ── Completed result sub-models ──────────────────────────────
class ElevationResult(BaseModel):
    min: float
    max: float
    mean: float

class SlopeDistributionItem(BaseModel):
    category: str
    percentage: float

class CutFillResult(BaseModel):
    cut: float
    fill: float
    net: float

class TileUrls(BaseModel):
    elevation: Optional[str] = None
    slope: Optional[str] = None

class GeoJsonUrls(BaseModel):
    contours: Optional[str] = None
    ponding: Optional[str] = None

class TopographyMetadata(BaseModel):
    copernicusDemVersion: str = "COP-30"
    processingTime: Optional[int] = None
    pixelResolution: int = 30

class TopographyResults(BaseModel):
    elevation: ElevationResult
    slopeDistribution: list[SlopeDistributionItem]
    cutFill: Optional[CutFillResult] = None
    tileUrls: Optional[TileUrls] = None
    geoJsonUrls: Optional[GeoJsonUrls] = None
    metadata: TopographyMetadata = TopographyMetadata()


# ── GET response data (§3.1.2) ───────────────────────────────
class TopographyJobData(BaseModel):
    pythonJobId: str
    status: JobStatus
    progressPercentage: int = 0
    currentStage: Optional[str] = None
    stageDetails: Optional[str] = None
    results: Optional[TopographyResults] = None
    completedAt: Optional[str] = None


# ── Legacy models (kept for backward compat with analyze.py) ─
class TopographyRequest(BaseModel):
    """Legacy request model used by the old pipeline and analyze endpoint."""
    parcel_id: str
    bbox: list[float]  # [minLon, minLat, maxLon, maxLat]
    geo_json: dict
    options: TopographyOptions = TopographyOptions()

class HealthResponse(BaseModel):
    status: str
    gee_initialized: bool
    redis_connected: bool
    version: str