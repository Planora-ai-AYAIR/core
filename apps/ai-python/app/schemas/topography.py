from typing import Optional, List, Generic, TypeVar, Literal
from pydantic import BaseModel, Field
from datetime import datetime

T = TypeVar("T")

# ── Unified Error ────────────────────────────
class ErrorDetail(BaseModel):
    field:   Optional[str] = None
    code:    str
    message: str

class ErrorResponse(BaseModel):
    status_code: int
    error_code:  str
    message:     str
    retryable:   bool = False
    details:     dict = {}

# ── Request ───────────────────────────────────────────────────
class TopographyOptions(BaseModel):
    contour_interval_m:  float          = 0.5
    reference_elevation: Optional[float]= None
    twi_threshold:       float          = 8.0
    export_tiles:        bool           = True

class TopographyRequest(BaseModel):
    parcel_id: str
    bbox:      list[float]   # [minLon, minLat, maxLon, maxLat]
    geo_json:  dict
    options:   TopographyOptions = TopographyOptions()

# ── 202 Accepted Response ─────────────────────────────────────
class JobAccepted(BaseModel):
    python_job_id: str
    parcel_id:     str
    status:        str = "queued"
    accepted_at:   str

# ── Polling Response — while processing ──────────────────────
class JobProcessing(BaseModel):
    python_job_id: str
    parcel_id:     str
    status:        Literal["queued", "processing", "completed", "failed"]
    progress:      int   # 0-100
    results:       Optional[dict] = None
    error:         Optional[dict] = None

# ── Health ────────────────────────────────────────────────────
class HealthResponse(BaseModel):
    status:          str
    gee_initialized: bool
    redis_connected: bool
    version:         str