"""Risk module schemas — API Contract §3.3."""

from __future__ import annotations
from typing import Optional
from pydantic import BaseModel

from app.schemas.common import BoundingBox, GeoJsonPolygon, JobStatus, BaseJobRequest


class SoilData(BaseModel):
    """Soil data passed to risk assessment."""
    clayContent: Optional[float] = None
    sandContent: Optional[float] = None
    waterTableDepth: Optional[float] = None


# ── POST /api/v1/risks/jobs — Request (§3.3.1) ───────────────
class RiskJobRequest(BaseJobRequest):
    riskTypes: list[str] = ["flood", "seismic", "expansiveSoil", "liquefaction"]
    soilData: Optional[SoilData] = None


# ── Result sub-models ────────────────────────────────────────
class RiskTypeResult(BaseModel):
    score: int
    level: str
    zonesGeoJson: Optional[str] = None
    zone: Optional[str] = None

class RiskResults(BaseModel):
    overallScore: int
    flood: Optional[RiskTypeResult] = None
    seismic: Optional[RiskTypeResult] = None
    expansiveSoil: Optional[RiskTypeResult] = None
    liquefaction: Optional[RiskTypeResult] = None


# ── GET response data (§3.3.2) ───────────────────────────────
class RiskJobData(BaseModel):
    pythonJobId: str
    status: JobStatus
    results: Optional[RiskResults] = None
