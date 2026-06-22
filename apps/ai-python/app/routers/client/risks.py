"""Module 4 — Construction Risk Score (client-facing) — API Contract §2.5.

POST /api/risks/jobs        — Submit risk assessment job (202)
GET  /api/risks/{parcelId}  — Retrieve risk results (200)
"""

from fastapi import APIRouter, BackgroundTasks

from app.schemas.client import RiskJobSubmit, JobQueued, RiskClientResult
from app.schemas.common import Envelope, accepted_response, success_response
from app.routers.client._helpers import (
    make_job, run_job, require_parcel, require_result, ESTIMATED_DURATION,
)

router = APIRouter(prefix="/api/risks", tags=["client: risks"])
MODULE = "risk"


@router.post("/jobs", status_code=202, response_model=Envelope[JobQueued])
async def submit_risk_job(req: RiskJobSubmit, bg: BackgroundTasks):
    """Submit a risk assessment job (§2.5.1)."""
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
        message="Risk assessment job queued",
    )


@router.get("/{parcelId}", response_model=Envelope[RiskClientResult])
async def get_risk_results(parcelId: str):
    """Retrieve risk results (§2.5.2)."""
    data = require_result(parcelId, MODULE)
    return success_response(data=data, message="Success")
