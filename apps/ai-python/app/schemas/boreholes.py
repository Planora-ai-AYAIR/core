"""Borehole optimization module schemas — API Contract §3.4."""

from __future__ import annotations
from typing import Optional
from pydantic import BaseModel

from app.schemas.common import BoundingBox, GeoJsonPolygon, JobStatus, BaseJobRequest


class BoreholeParameters(BaseModel):
    maxSpacing: int = 30
    minBoreholes: int = 12
    targetDepth: int = 20

class SoilVariability(BaseModel):
    hotspotZones: list[dict] = []
    homogeneousZones: list[dict] = []


# ── POST /api/v1/boreholes/jobs — Request (§3.4.1) ───────────
class BoreholeJobRequest(BaseJobRequest):
    soilVariability: Optional[SoilVariability] = None
    parameters: BoreholeParameters = BoreholeParameters()


# ── Result sub-models ────────────────────────────────────────
class PlacementPoint(BaseModel):
    id: str
    lat: float
    lng: float
    priority: str
    reason: str

class CostApproach(BaseModel):
    count: int
    cost: int

class CostComparison(BaseModel):
    traditional: CostApproach
    optimized: CostApproach
    savings: dict

class BoreholeResults(BaseModel):
    minimumRequired: int
    optimalCount: int
    placementPoints: list[PlacementPoint] = []
    costComparison: Optional[CostComparison] = None


# ── GET response data (§3.4.2) ───────────────────────────────
class BoreholeJobData(BaseModel):
    pythonJobId: str
    status: JobStatus
    results: Optional[BoreholeResults] = None
