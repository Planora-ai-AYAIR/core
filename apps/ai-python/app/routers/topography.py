"""Topography router — API Contract §3.1.

POST /api/v1/topography/jobs   — Submit topography processing job
GET  /api/v1/topography/jobs/{pythonJobId} — Poll job status / results
"""

import logging
import os
import time
import uuid
from datetime import datetime, timezone

from fastapi import APIRouter, HTTPException, BackgroundTasks

from app.schemas.topography import (
    TopographyJobRequest,
    TopographyJobData,
    TopographyOptions,
    TopographyRequest as LegacyRequest,
)
from app.schemas.common import (
    Envelope,
    ErrorCode,
    JobAccepted,
    accepted_response,
    error_response,
    success_response,
    utc_now_iso,
)
from app.services.gee_service import validate_bbox_egypt, export_dem_for_parcel
from app.services.topography_service import run_analysis, load_model_b
from app.config import settings

logger = logging.getLogger(__name__)

router = APIRouter(prefix="/api/v1/topography", tags=["topography"])

_jobs: dict = {}

# Paths (overridable via environment)
RASTER_DIR = os.getenv("RASTER_DIR", "/data/rasters/")
MODEL_B_PATH = os.getenv("MODEL_B_PATH", "/data/models/model_b_bundle.joblib")

# Model B bundle is loaded once and shared read-only across requests.
_model_b: dict | None = None


def _get_model_b() -> dict | None:
    """Lazily load the Model B bundle once; return None if unavailable."""
    global _model_b
    if _model_b is None:
        try:
            _model_b = load_model_b(MODEL_B_PATH)
        except Exception as e:
            logger.error(f"Model B load failed ({MODEL_B_PATH}): {e}")
            _model_b = None
    return _model_b


# ── POST /api/v1/topography/jobs ──────────────────────────────
@router.post("/jobs", status_code=202, response_model=Envelope[JobAccepted])
async def submit_topography_job(
    req: TopographyJobRequest,
    bg: BackgroundTasks,
):
    """Submit a topography analysis job (§3.1.1)."""
    bbox_list = [req.bbox.minX, req.bbox.minY, req.bbox.maxX, req.bbox.maxY]

    if not validate_bbox_egypt(bbox_list):
        raise HTTPException(
            status_code=400,
            detail=error_response(
                status_code=400,
                message="Request validation failed",
                errors=[{
                    "field": "bbox",
                    "code": ErrorCode.INVALID_GEOMETRY.value,
                    "message": "Parcel must be within Egypt bounds [24-37°E, 22-32°N]",
                }],
            ),
        )

    python_job_id = f"pyjob_topo_{uuid.uuid4().hex[:12]}"
    accepted_at = utc_now_iso()

    _jobs[python_job_id] = {
        "pythonJobId": python_job_id,
        "status": "queued",
        "progressPercentage": 0,
        "currentStage": None,
        "stageDetails": None,
        "results": None,
        "completedAt": None,
        "error": None,
    }

    bg.add_task(_run_pipeline, python_job_id, req)

    return accepted_response(
        data={
            "pythonJobId": python_job_id,
            "status": "queued",
            "acceptedAt": accepted_at,
        },
        message="Python topography job queued",
    )


# ── GET /api/v1/topography/jobs/{pythonJobId} ─────────────────
@router.get("/jobs/{pythonJobId}", response_model=Envelope[TopographyJobData])
async def get_topography_status(pythonJobId: str):
    """Poll topography job status (§3.1.2)."""
    job = _jobs.get(pythonJobId)

    if not job:
        raise HTTPException(
            status_code=404,
            detail=error_response(
                status_code=404,
                message="Job not found",
                errors=[{
                    "field": "pythonJobId",
                    "code": ErrorCode.JOB_NOT_FOUND.value,
                    "message": f"No job found with id {pythonJobId}",
                }],
            ),
        )

    if job.get("status") == "failed":
        return success_response(
            data={
                "pythonJobId": job["pythonJobId"],
                "status": "failed",
                "progressPercentage": 0,
                "currentStage": None,
                "stageDetails": (job.get("error") or {}).get("message"),
            },
            message="Job failed",
        )

    msg = "Job in progress" if job["status"] == "processing" else (
        "Job completed" if job["status"] == "completed" else "Job queued"
    )

    return success_response(
        data={
            "pythonJobId": job["pythonJobId"],
            "status": job["status"],
            "progressPercentage": job["progressPercentage"],
            "currentStage": job.get("currentStage"),
            "stageDetails": job.get("stageDetails"),
            "results": job.get("results"),
            "completedAt": job.get("completedAt"),
        },
        message=msg,
    )


# ── Background pipeline ───────────────────────────────────────
async def _run_pipeline(python_job_id: str, req: TopographyJobRequest):
    bbox_list = [req.bbox.minX, req.bbox.minY, req.bbox.maxX, req.bbox.maxY]
    out_dir = f"{settings.local_out_dir}/{python_job_id}"
    os.makedirs(out_dir, exist_ok=True)

    def upd(status: str, progress: int,
            stage: str | None = None, stage_details: str | None = None,
            results=None, error=None):
        _jobs[python_job_id].update({
            "status": status,
            "progressPercentage": progress,
            "currentStage": stage,
            "stageDetails": stage_details,
            "results": results,
            "error": error,
        })

    try:
        t0 = time.time()

        # Convert contract options to internal options
        internal_opts = TopographyOptions(
            contour_interval_m=req.options.contourInterval,
        )

        # Build legacy request for existing run_analysis
        legacy_req = LegacyRequest(
            parcel_id=req.parcelId,
            bbox=bbox_list,
            geo_json=req.geoJson.model_dump(),
            options=internal_opts,
        )

        # Step 1: export DEM
        upd("processing", 10, "DEM Export", "Exporting DEM tile from Copernicus")
        task_id = export_dem_for_parcel(bbox_list, python_job_id, out_dir)

        # Step 2: full analysis
        upd("processing", 30, "Elevation Analysis", "Computing elevation statistics")
        analysis = run_analysis(legacy_req, _get_model_b(), raster_dir=RASTER_DIR)

        upd("processing", 70, "Slope Classification", "Classifying pixels into slope categories")

        processing_time = round(time.time() - t0)

        # Map to contract result shape
        terrain = analysis.get("terrain", {})
        slope_zones = terrain.get("slope_zones", {}) if terrain else {}

        elevation_result = {
            "min": terrain.get("elevation_min_m", 0) if terrain else 0,
            "max": terrain.get("elevation_max_m", 0) if terrain else 0,
            "mean": terrain.get("elevation_mean_m", 0) if terrain else 0,
        }

        slope_distribution = [
            {"category": "flat", "percentage": slope_zones.get("flat_pct", 0)},
            {"category": "gentle", "percentage": slope_zones.get("gentle_pct", 0)},
            {"category": "moderate", "percentage": slope_zones.get("moderate_pct", 0)},
            {"category": "steep", "percentage": slope_zones.get("steep_pct", 0)},
        ]

        cut_fill = None
        if req.options.generateCutFill and terrain:
            water = terrain.get("water_accumulation", {})
            cut_fill = {
                "cut": water.get("high_risk_pct", 0) * 100,
                "fill": water.get("low_risk_pct", 0) * 100,
                "net": (water.get("high_risk_pct", 0) - water.get("low_risk_pct", 0)) * 100,
            }

        results = {
            "elevation": elevation_result,
            "slopeDistribution": slope_distribution,
            "cutFill": cut_fill,
            "tileUrls": {
                "elevation": f"s3://bucket/elevation/{{z}}/{{x}}/{{y}}.png",
                "slope": f"s3://bucket/slope/{{z}}/{{x}}/{{y}}.png",
            },
            "geoJsonUrls": {
                "contours": f"s3://bucket/contours.geojson",
                "ponding": f"s3://bucket/ponding.geojson",
            },
            "metadata": {
                "copernicusDemVersion": "COP-30",
                "processingTime": processing_time,
                "pixelResolution": 30,
            },
        }

        upd("completed", 100, results=results)
        _jobs[python_job_id]["completedAt"] = utc_now_iso()

    except Exception as e:
        logger.error(f"Pipeline failed for {python_job_id}: {e}", exc_info=True)
        upd("failed", 0, error={
            "code": ErrorCode.INTERNAL_ERROR.value,
            "message": str(e),
        })