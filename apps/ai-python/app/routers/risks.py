"""Risk router — API Contract §3.3.

POST /api/v1/risks/jobs              — Submit risk assessment job
GET  /api/v1/risks/jobs/{pythonJobId} — Poll job status / results
"""

import logging
import uuid

from fastapi import APIRouter, HTTPException, BackgroundTasks
from fastapi.responses import JSONResponse

from app.schemas.risks import RiskJobRequest
from app.schemas.common import (
    accepted_response,
    error_response,
    success_response,
    utc_now_iso,
)
from app.services.risk_service import compute_risks
from app.services.gee_service import terrain_from_gee

logger = logging.getLogger(__name__)

router = APIRouter(prefix="/api/v1/risks", tags=["risks"])

_jobs: dict = {}


# ── POST /api/v1/risks/jobs ───────────────────────────────────
@router.post("/jobs", status_code=202)
async def submit_risk_job(req: RiskJobRequest, bg: BackgroundTasks):
    """Submit a risk assessment job (§3.3.1)."""
    python_job_id = f"pyjob_risk_{uuid.uuid4().hex[:12]}"
    accepted_at = utc_now_iso()

    _jobs[python_job_id] = {
        "pythonJobId": python_job_id,
        "status": "queued",
        "results": None,
        "error": None,
    }

    bg.add_task(_run_risk_pipeline, python_job_id, req)

    return JSONResponse(
        status_code=202,
        content=accepted_response(
            data={
                "pythonJobId": python_job_id,
                "status": "queued",
                "acceptedAt": accepted_at,
            },
            message="Python risk job queued",
        ),
    )


# ── GET /api/v1/risks/jobs/{pythonJobId} ──────────────────────
@router.get("/jobs/{pythonJobId}")
async def get_risk_status(pythonJobId: str):
    """Poll risk job status (§3.3.2)."""
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
async def _run_risk_pipeline(python_job_id: str, req: RiskJobRequest):
    _jobs[python_job_id]["status"] = "processing"

    try:
        # Try to get terrain data from GEE for flood analysis
        terrain = None
        try:
            geo_json = req.geoJson.model_dump()
            terrain = terrain_from_gee(geo_json)
        except Exception as e:
            logger.warning(f"GEE terrain fetch failed (risk will use soil data only): {e}")

        clay = req.soilData.clayContent if req.soilData else None
        sand = req.soilData.sandContent if req.soilData else None
        wtd = req.soilData.waterTableDepth if req.soilData else None

        results = compute_risks(
            risk_types=req.riskTypes,
            terrain=terrain,
            clay_content=clay,
            sand_content=sand,
            water_table_depth=wtd,
        )

        _jobs[python_job_id].update({
            "status": "completed",
            "results": results,
        })

    except Exception as e:
        logger.error(f"Risk pipeline failed for {python_job_id}: {e}", exc_info=True)
        _jobs[python_job_id].update({
            "status": "failed",
            "error": {"code": "PROCESSING_ERROR", "message": str(e)},
        })
