"""Borehole optimization router — API Contract §3.4.

POST /api/v1/boreholes/jobs              — Submit borehole optimization job
GET  /api/v1/boreholes/jobs/{pythonJobId} — Poll job status / results
"""

import logging
import uuid

from fastapi import APIRouter, HTTPException, BackgroundTasks
from fastapi.responses import JSONResponse

from app.schemas.boreholes import BoreholeJobRequest
from app.schemas.common import (
    accepted_response,
    error_response,
    success_response,
    utc_now_iso,
)
from app.services.borehole_service import optimize_boreholes

logger = logging.getLogger(__name__)

router = APIRouter(prefix="/api/v1/boreholes", tags=["boreholes"])

_jobs: dict = {}


# ── POST /api/v1/boreholes/jobs ───────────────────────────────
@router.post("/jobs", status_code=202)
async def submit_borehole_job(req: BoreholeJobRequest, bg: BackgroundTasks):
    """Submit a borehole optimization job (§3.4.1)."""
    python_job_id = f"pyjob_bore_{uuid.uuid4().hex[:12]}"
    accepted_at = utc_now_iso()

    _jobs[python_job_id] = {
        "pythonJobId": python_job_id,
        "status": "queued",
        "results": None,
        "error": None,
    }

    bg.add_task(_run_borehole_pipeline, python_job_id, req)

    return JSONResponse(
        status_code=202,
        content=accepted_response(
            data={
                "pythonJobId": python_job_id,
                "status": "queued",
                "acceptedAt": accepted_at,
            },
            message="Python borehole job queued",
        ),
    )


# ── GET /api/v1/boreholes/jobs/{pythonJobId} ──────────────────
@router.get("/jobs/{pythonJobId}")
async def get_borehole_status(pythonJobId: str):
    """Poll borehole job status (§3.4.2)."""
    job = _jobs.get(pythonJobId)

    if not job:
        raise HTTPException(
            status_code=404,
            detail=error_response(
                status_code=404,
                message="Job not found",
                errors=[{
                    "field": "pythonJobId",
                    "code": "JOB_NOT_FOUND",
                    "message": f"No job found with id {pythonJobId}",
                }],
            ),
        )

    msg = {
        "queued": "Job queued",
        "processing": "Job in progress",
        "completed": "Success",
        "failed": "Job failed",
    }.get(job["status"], "Unknown")

    return JSONResponse(
        status_code=200,
        content=success_response(
            data={
                "pythonJobId": job["pythonJobId"],
                "status": job["status"],
                "results": job.get("results"),
            },
            message=msg,
        ),
    )


# ── Background pipeline ──────────────────────────────────────
async def _run_borehole_pipeline(python_job_id: str, req: BoreholeJobRequest):
    _jobs[python_job_id]["status"] = "processing"

    try:
        bbox = req.bbox.model_dump()
        hotspots = req.soilVariability.hotspotZones if req.soilVariability else []
        homogeneous = req.soilVariability.homogeneousZones if req.soilVariability else []

        results = optimize_boreholes(
            bbox=bbox,
            max_spacing=req.parameters.maxSpacing,
            min_boreholes=req.parameters.minBoreholes,
            target_depth=req.parameters.targetDepth,
            hotspot_zones=hotspots,
            homogeneous_zones=homogeneous,
        )

        _jobs[python_job_id].update({
            "status": "completed",
            "results": results,
        })

    except Exception as e:
        logger.error(f"Borehole pipeline failed for {python_job_id}: {e}", exc_info=True)
        _jobs[python_job_id].update({
            "status": "failed",
            "error": {"code": "PROCESSING_ERROR", "message": str(e)},
        })
