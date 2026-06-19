"""Parcel management — API Contract §2.1.

POST /api/parcels             — Register a new land parcel (201)
GET  /api/parcels/{parcelId}  — Retrieve a parcel (200)
"""

import logging
import uuid

from fastapi import APIRouter, HTTPException

from app.schemas.client import (
    AreaUnit,
    ParcelCreateRequest,
    ParcelCreatedData,
    ParcelData,
)
from app.schemas.common import (
    Envelope,
    ErrorCode,
    error_response,
    success_response,
    utc_now_iso,
)
from app.schemas.common import compute_bbox_from_geojson
from app.services import store
from app.routers.client._helpers import require_parcel

logger = logging.getLogger(__name__)

router = APIRouter(prefix="/api/parcels", tags=["client: parcels"])

# Minimum parcel area per §2.1.1 validation rules (5 hectares).
MIN_AREA_M2 = 50_000
_TO_M2 = {"m2": 1.0, "hectares": 10_000.0, "acres": 4046.8564224}


def _validation_error(field: str, code: ErrorCode, message: str) -> HTTPException:
    return HTTPException(
        status_code=400,
        detail=error_response(
            status_code=400,
            message="Request validation failed",
            errors=[{"field": field, "code": code.value, "message": message}],
        ),
    )


# ── POST /api/parcels ─────────────────────────────────────────
@router.post("", status_code=201, response_model=Envelope[ParcelCreatedData])
async def create_parcel(req: ParcelCreateRequest):
    """Register a new land parcel boundary for analysis (§2.1.1)."""
    # clientName: required, max 200 chars
    if not req.clientName or len(req.clientName) > 200:
        raise _validation_error(
            "clientName", ErrorCode.INVALID_CLIENT_NAME,
            "Client name is required and must be at most 200 characters.",
        )

    # areaUnit: enum m2 | hectares | acres
    if req.areaUnit not in {u.value for u in AreaUnit}:
        raise _validation_error(
            "areaUnit", ErrorCode.INVALID_UNIT,
            "Area unit must be one of: m2, hectares, acres.",
        )

    # geoJson: valid closed polygon ring with >= 4 coordinates
    coords = req.geoJson.coordinates[0] if req.geoJson.coordinates else []
    if len(coords) < 4 or coords[0] != coords[-1]:
        raise _validation_error(
            "geoJson", ErrorCode.INVALID_GEOMETRY,
            "Polygon must contain at least 4 coordinates and close the ring.",
        )

    # area: >= 50,000 m2 (after unit conversion)
    area_m2 = req.area * _TO_M2[req.areaUnit]
    if area_m2 < MIN_AREA_M2:
        raise _validation_error(
            "area", ErrorCode.PARCEL_TOO_SMALL,
            "Parcel area must be at least 50,000 m² (5 hectares).",
        )

    parcel_id = f"parcel_{uuid.uuid4().hex[:8]}"
    bbox = compute_bbox_from_geojson(req.geoJson)
    created_at = utc_now_iso()

    store.save_parcel({
        "parcelId": parcel_id,
        "clientName": req.clientName,
        "geoJson": req.geoJson.model_dump(),
        "area": req.area,
        "areaUnit": req.areaUnit,
        "boundingBox": bbox.model_dump(),
        "status": "registered",
        "modulesCompleted": [],
        "createdAt": created_at,
        "completedAt": None,
    })

    return success_response(
        data={
            "parcelId": parcel_id,
            "boundingBox": bbox.model_dump(),
            "area": req.area,
            "createdAt": created_at,
        },
        message="Parcel created successfully",
        status_code=201,
    )


# ── GET /api/parcels/{parcelId} ───────────────────────────────
@router.get("/{parcelId}", response_model=Envelope[ParcelData])
async def get_parcel(parcelId: str):
    """Retrieve a registered parcel (§2.1.2)."""
    parcel = require_parcel(parcelId)
    modules = parcel.get("modulesCompleted", [])
    status = "completed" if modules else parcel.get("status", "registered")

    return success_response(
        data={
            "parcelId": parcel["parcelId"],
            "clientName": parcel["clientName"],
            "area": parcel["area"],
            "status": status,
            "modulesCompleted": modules,
            "boundingBox": parcel["boundingBox"],
            "createdAt": parcel["createdAt"],
            "completedAt": parcel.get("completedAt"),
        },
        message="Success",
    )
