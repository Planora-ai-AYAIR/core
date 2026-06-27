"""Module 5 — Optimized Borehole Campaign Plan (client-facing) — §2.6.

POST /api/boreholes/jobs        — Submit borehole optimization job (202)
GET  /api/boreholes/{parcelId}  — Retrieve borehole plan (200)
"""

from fastapi import APIRouter, BackgroundTasks

from app.schemas.client import BoreholeJobSubmit, JobQueued, BoreholeClientResult
from app.schemas.common import Envelope, accepted_response, success_response
from app.routers.client._helpers import (
    make_job, run_job, require_parcel, require_result, ESTIMATED_DURATION,
)

router = APIRouter(prefix="/api/boreholes", tags=["client: boreholes"])
MODULE = "borehole"


@router.post("/jobs", status_code=202, response_model=Envelope[JobQueued])
async def submit_borehole_job(req: BoreholeJobSubmit, bg: BackgroundTasks):
    """Submit a borehole optimization job (§2.6.1)."""
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
        message="Borehole optimization job queued",
    )


@router.get("/{parcelId}", response_model=Envelope[BoreholeClientResult])
async def get_borehole_results(parcelId: str):
    """Retrieve borehole campaign plan (§2.6.2)."""
    data = require_result(parcelId, MODULE)
    return success_response(data=data, message="Success")
