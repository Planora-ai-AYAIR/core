"""
Unified response envelope and shared data types for GeoSense AI.

Every API response follows the standardized envelope format defined in
the GeoSense AI API Contract §1. This module provides the base models
and helper functions used across all module schemas.
"""

from __future__ import annotations

from datetime import datetime, timezone
from enum import Enum
from typing import Any, Optional

from pydantic import BaseModel, Field, model_validator


# ── Enums ─────────────────────────────────────────────────────

class JobStatus(str, Enum):
    QUEUED = "queued"
    PROCESSING = "processing"
    COMPLETED = "completed"
    FAILED = "failed"


class RiskLevel(str, Enum):
    VERY_LOW = "Very Low"
    LOW = "Low"
    MODERATE = "Moderate"
    HIGH = "High"
    VERY_HIGH = "Very High"


class FoundationType(str, Enum):
    SHALLOW = "shallow"
    DEEP = "deep"
    PILE = "pile"
    MAT = "mat"


# ── Shared Data Types ────────────────────────────────────────

class BoundingBox(BaseModel):
    """WGS84 bounding box (Appendix B.1)."""
    minX: float
    minY: float
    maxX: float
    maxY: float


class GeoJsonPolygon(BaseModel):
    """GeoJSON Polygon geometry (Appendix B.2)."""
    type: str = "Polygon"
    coordinates: list[list[list[float]]]


def compute_bbox_from_geojson(geo_json: GeoJsonPolygon) -> BoundingBox:
    """Computes a bounding box from a GeoJsonPolygon."""
    if not geo_json.coordinates or not geo_json.coordinates[0]:
        return BoundingBox(minX=0.0, minY=0.0, maxX=0.0, maxY=0.0)
    
    coords = geo_json.coordinates[0]
    lons = [p[0] for p in coords]
    lats = [p[1] for p in coords]
    
    return BoundingBox(
        minX=min(lons),
        minY=min(lats),
        maxX=max(lons),
        maxY=max(lats)
    )


class BaseJobRequest(BaseModel):
    """Base model for async jobs that process a parcel."""
    jobId: str
    parcelId: str
    geoJson: GeoJsonPolygon
    bbox: Optional[BoundingBox] = None

    @model_validator(mode='after')
    def compute_bbox_if_missing(self) -> 'BaseJobRequest':
        if self.bbox is None and self.geoJson:
            self.bbox = compute_bbox_from_geojson(self.geoJson)
        return self


class ErrorDetail(BaseModel):
    """Individual error object within the errors array (§1.5)."""
    field: Optional[str] = None
    code: str
    message: str


# ── Unified Response Envelope ────────────────────────────────

class UnifiedResponse(BaseModel):
    """
    Standardized envelope for every API response (§1.1 / §1.2).

    All endpoints — success, error, and async-accepted — return this shape.
    """
    statusCode: int
    message: str
    errors: Optional[list[ErrorDetail]] = None
    data: Optional[Any] = None


# ── Helper Factories ─────────────────────────────────────────

def success_response(
    data: Any,
    message: str = "Success",
    status_code: int = 200,
) -> dict:
    """Build a success envelope dict."""
    return {
        "statusCode": status_code,
        "message": message,
        "errors": None,
        "data": data,
    }


def accepted_response(
    data: dict,
    message: str = "Job accepted for processing",
) -> dict:
    """Build a 202 Accepted envelope dict."""
    return {
        "statusCode": 202,
        "message": message,
        "errors": None,
        "data": data,
    }


def error_response(
    status_code: int,
    message: str,
    errors: list[dict] | None = None,
) -> dict:
    """Build an error envelope dict."""
    return {
        "statusCode": status_code,
        "message": message,
        "errors": errors,
        "data": None,
    }


def utc_now_iso() -> str:
    """Current UTC timestamp in ISO-8601 format."""
    return datetime.now(timezone.utc).isoformat()
