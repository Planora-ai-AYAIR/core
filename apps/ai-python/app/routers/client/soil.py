"""Module 2 — AI Soil Composition Estimate (client-facing) — API Contract §2.3.

POST /api/soil/jobs        — Submit soil analysis job (202)
GET  /api/soil/{parcelId}  — Retrieve soil results (200)
"""

from fastapi import APIRouter, BackgroundTasks, Query

from app.schemas.client import SoilJobSubmit, JobQueued, SoilClientResult
from app.schemas.common import Envelope, accepted_response, success_response
from app.services import client_mocks
from app.routers.client._helpers import (
    make_job, run_job, require_parcel, require_result, ESTIMATED_DURATION,
)

router = APIRouter(prefix="/api/soil", tags=["client: soil"])
MODULE = "soil"
_DEPTHS = ["0-20cm", "20-50cm", "50-100cm", "100-200cm"]


@router.post("/jobs", status_code=202, response_model=Envelope[JobQueued])
async def submit_soil_job(req: SoilJobSubmit, bg: BackgroundTasks):
    """Submit a soil analysis job (§2.3.1)."""
    require_parcel(req.parcelId)
    job = make_job(MODULE, req.parcelId)
    bg.add_task(run_job, job["jobId"], MODULE, req.parcelId)
    return accepted_response(
        data={
            "jobId": job["jobId"],
            "parcelId": req.parcelId,
            "status": "queued",
            "estimatedDuration": ESTIMATED_DURATION,
        },
        message="Soil job queued",
    )


@router.get("/{parcelId}", response_model=Envelope[SoilClientResult])
async def get_soil_results(
    parcelId: str,
    depth: str = Query("0-20cm", description="0-20cm | 20-50cm | 50-100cm | 100-200cm"),
):
    """Retrieve soil results (§2.3.2)."""
    require_result(parcelId, MODULE)
    sel = depth if depth in _DEPTHS else "0-20cm"
    data = client_mocks.soil_result(parcelId, depth=sel)
    return success_response(data=data, message="Success")
