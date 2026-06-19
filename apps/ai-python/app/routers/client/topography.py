"""Module 1 — AI Topographic Profile (client-facing) — API Contract §2.2.

POST /api/topography/jobs        — Submit topography job (202)
GET  /api/topography/{parcelId}  — Retrieve topography results (200)
"""

from fastapi import APIRouter, BackgroundTasks, Query

from app.schemas.client import TopographyJobSubmit, JobQueued, TopographyClientResult
from app.schemas.common import Envelope, accepted_response, success_response
from app.services import client_mocks
from app.routers.client._helpers import (
    make_job, run_job, require_parcel, require_result, ESTIMATED_DURATION,
)

router = APIRouter(prefix="/api/topography", tags=["client: topography"])
MODULE = "topography"


@router.post("/jobs", status_code=202, response_model=Envelope[JobQueued])
async def submit_topography_job(req: TopographyJobSubmit, bg: BackgroundTasks):
    """Submit a topography analysis job (§2.2.1)."""
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
        message="Topography job queued",
    )


@router.get("/{parcelId}", response_model=Envelope[TopographyClientResult])
async def get_topography_results(
    parcelId: str,
    includeTiles: bool = Query(True, description="Include S3 signed URLs for raster tiles"),
    format: str = Query("json", description="json | geojson"),
):
    """Retrieve topography results (§2.2.2)."""
    require_result(parcelId, MODULE)  # gate on completion (409 if not ready)
    data = client_mocks.topography_result(parcelId, include_tiles=includeTiles, fmt=format)
    return success_response(data=data, message="Success")
