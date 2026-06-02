import logging, uuid, time, os
from datetime import datetime, timezone

from fastapi import APIRouter, HTTPException, BackgroundTasks

from app.schemas.topography import (
    TopographyRequest,
    JobAccepted,
    JobProcessing,
    ErrorResponse,
)
from app.services.gee_service import validate_bbox_egypt, export_dem_for_parcel
from app.config import settings

logger = logging.getLogger(__name__)

router = APIRouter(prefix="/api/v1/topography", tags=["topography"])

_jobs: dict = {}


# ── POST /api/v1/topography/jobs ──────────────────────────────
@router.post("/jobs", response_model=JobAccepted, status_code=202)
async def submit_topography_job(req: TopographyRequest,
                                 bg: BackgroundTasks):
    # Validate bbox
    if not validate_bbox_egypt(req.bbox):
        raise HTTPException(
            status_code=400,
            detail={
                "status_code": 400,
                "error_code":  "INVALID_BBOX",
                "message":     "Parcel must be within Egypt bounds [24-37°E, 22-32°N]",
                "retryable":   False,
                "details":     {"bbox": req.bbox}
            }
        )

    # Generate python_job_id (FastAPI owns this)
    python_job_id = str(uuid.uuid4())
    accepted_at   = datetime.now(timezone.utc).isoformat()

    # Save to in-memory store immediately
    _jobs[python_job_id] = {
        "python_job_id": python_job_id,
        "parcel_id":     req.parcel_id,
        "status":        "queued",
        "progress":      0,
        "results":       None,
        "error":         None,
    }

    # Run pipeline in background
    bg.add_task(_run_pipeline, python_job_id, req)

    # Return 202 immediately
    return JobAccepted(
        python_job_id = python_job_id,
        parcel_id     = req.parcel_id,
        status        = "queued",
        accepted_at   = accepted_at,
    )


# ── GET /api/v1/topography/jobs/{python_job_id} ───────────────
@router.get("/jobs/{python_job_id}", response_model=JobProcessing)
async def get_topography_status(python_job_id: str):
    job = _jobs.get(python_job_id)

    if not job:
        raise HTTPException(
            status_code=404,
            detail={
                "status_code": 404,
                "error_code":  "JOB_NOT_FOUND",
                "message":     f"No job found with id {python_job_id}",
                "retryable":   False,
                "details":     {}
            }
        )

    return JobProcessing(**job)


# ── Background pipeline ───────────────────────────────────────
async def _run_pipeline(python_job_id: str, req: TopographyRequest):
    out_dir = f"{settings.local_out_dir}/{python_job_id}"
    os.makedirs(out_dir, exist_ok=True)

    def upd(status: str, progress: int,
            results=None, error=None):
        _jobs[python_job_id].update({
            "status":   status,
            "progress": progress,
            "results":  results,
            "error":    error,
        })

    try:
        t0 = time.time()

        upd("processing", 10)
        task_id = export_dem_for_parcel(req.bbox, python_job_id, out_dir)



        upd("completed", 100, results={
            "task_id":                 task_id,
            "dem_file":                f"dem_{python_job_id}.tif",
            "coordinate_system":       "EPSG:32636",
            "resolution_m":            30,
            "processing_time_seconds": round(time.time() - t0, 1),
            "note": "Day 1 complete — terrain analysis coming Day 2"
        })

    except Exception as e:
        logger.error(f"Pipeline failed for {python_job_id}: {e}", exc_info=True)
        upd("failed", 0, error={
            "code":      "PROCESSING_ERROR",
            "message":   str(e),
            "retryable": True
        })