"""Module 3 — Bearing Capacity Estimate (client-facing) — API Contract §2.4.

POST /api/bearing/jobs        — Submit bearing capacity job (202)
GET  /api/bearing/{parcelId}  — Retrieve bearing capacity results (200)
"""

from fastapi import APIRouter, BackgroundTasks

from app.schemas.client import BearingJobSubmit, JobQueued, BearingClientResult
from app.schemas.common import Envelope, accepted_response, success_response
from app.services import client_mocks, store
from app.routers.client._helpers import (
    make_job, require_parcel, require_result, ESTIMATED_DURATION,
)
from app.services.webhook_service import send_analysis_webhook

router = APIRouter(prefix="/api/bearing", tags=["client: bearing"])
MODULE = "bearing"


@router.post("/jobs", status_code=202, response_model=Envelope[JobQueued])
async def submit_bearing_job(req: BearingJobSubmit, bg: BackgroundTasks):
    """Submit a bearing capacity job (§2.4.1)."""
    require_parcel(req.parcelId)
    job = make_job(MODULE, req.parcelId)
    # carry foundationType so the result reflects the request
    store.update_job(job["jobId"], foundationType=req.foundationType.value)
    bg.add_task(_run_bearing, job["jobId"], req.parcelId, req.foundationType.value)
    return accepted_response(
        data={
            "jobId": job["jobId"],
            "parcelId": req.parcelId,
            "status": "queued",
            "estimatedDuration": ESTIMATED_DURATION,
        },
        message="Bearing capacity job queued",
    )


async def _run_bearing(job_id: str, parcel_id: str, foundation_type: str) -> None:
    store.update_job(job_id, status="processing", progressPercentage=50,
                     message="bearing analysis in progress")
    result = client_mocks.bearing_result(parcel_id, foundation_type=foundation_type)
    store.save_result(parcel_id, MODULE, result)
    from app.schemas.common import utc_now_iso
    store.update_job(job_id, status="completed", progressPercentage=100,
                     completedAt=utc_now_iso(),
                     message="bearing analysis completed successfully")
    await send_analysis_webhook(job_id, result)


@router.get("/{parcelId}", response_model=Envelope[BearingClientResult])
async def get_bearing_results(parcelId: str):
    """Retrieve bearing capacity results (§2.4.2)."""
    data = require_result(parcelId, MODULE)
    return success_response(data=data, message="Success")
