"""Soil module schemas — API Contract §3.2."""

from __future__ import annotations
from typing import Optional
from pydantic import BaseModel

from app.schemas.common import BoundingBox, GeoJsonPolygon, JobStatus, BaseJobRequest


# ── POST /api/v1/soil/jobs — Request (§3.2.1) ────────────────
class SoilJobRequest(BaseJobRequest):
    depths: list[str] = ["0-20cm", "20-50cm", "50-100cm", "100-200cm"]


# ── Result sub-models ────────────────────────────────────────
class SoilSurface(BaseModel):
    sand: Optional[float] = None
    silt: Optional[float] = None
    clay: Optional[float] = None
    bulkDensity: Optional[float] = None
    organicCarbon: Optional[float] = None
    ph: Optional[float] = None
    classification: Optional[str] = None

class SoilDepthProfile(BaseModel):
    depth: str
    sand: Optional[float] = None
    silt: Optional[float] = None
    clay: Optional[float] = None
    bulkDensity: Optional[float] = None
    organicCarbon: Optional[float] = None
    ph: Optional[float] = None
    type: Optional[str] = None

class SoilResults(BaseModel):
    surface: SoilSurface
    depthProfiles: list[SoilDepthProfile] = []
    heatmapTileUrl: Optional[str] = None
    dataSources: list[str] = ["ISRIC SoilGrids v2.0"]


# ── GET response data (§3.2.2) ───────────────────────────────
class SoilJobData(BaseModel):
    pythonJobId: str
    status: JobStatus
    results: Optional[SoilResults] = None
