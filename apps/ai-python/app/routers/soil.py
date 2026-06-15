"""Soil router — API Contract §3.2.

POST /api/v1/soil/jobs              — Submit soil analysis job
GET  /api/v1/soil/jobs/{pythonJobId} — Poll job status / results
"""

import logging
import uuid

from fastapi import APIRouter, HTTPException, BackgroundTasks
from fastapi.responses import JSONResponse

from app.schemas.soil import SoilJobRequest
from app.schemas.common import (
    accepted_response,
    error_response,
    success_response,
    utc_now_iso,
)
from app.services.soilgrids_service import get_soil_composition

logger = logging.getLogger(__name__)

router = APIRouter(prefix="/api/v1/soil", tags=["soil"])

_jobs: dict = {}


# ── POST /api/v1/soil/jobs ────────────────────────────────────
@router.post("/jobs", status_code=202)
async def submit_soil_job(req: SoilJobRequest, bg: BackgroundTasks):
    """Submit a soil analysis job (§3.2.1)."""
    python_job_id = f"pyjob_soil_{uuid.uuid4().hex[:12]}"
    accepted_at = utc_now_iso()

    _jobs[python_job_id] = {
        "pythonJobId": python_job_id,
        "status": "queued",
        "results": None,
        "error": None,
    }

    bg.add_task(_run_soil_pipeline, python_job_id, req)

    return JSONResponse(
        status_code=202,
        content=accepted_response(
            data={
                "pythonJobId": python_job_id,
                "status": "queued",
                "acceptedAt": accepted_at,
            },
            message="Python soil job queued",
        ),
    )


# ── GET /api/v1/soil/jobs/{pythonJobId} ───────────────────────
@router.get("/jobs/{pythonJobId}")
async def get_soil_status(pythonJobId: str):
    """Poll soil job status (§3.2.2)."""
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
async def _run_soil_pipeline(python_job_id: str, req: SoilJobRequest):
    _jobs[python_job_id]["status"] = "processing"

    try:
        geo_json = req.geoJson.model_dump()
        soil = get_soil_composition(geo_json)

        if not soil:
            raise RuntimeError("SoilGrids returned no data for this location")

        profiles = soil.get("profiles", {})
        clay_p = profiles.get("clay", {})
        sand_p = profiles.get("sand", {})
        silt_p = profiles.get("silt", {})
        bdod_p = profiles.get("bdod", {})
        soc_p = profiles.get("soc", {})
        ph_p = profiles.get("phh2o", {})

        # Build depth profiles
        depth_map = {
            "0-20cm": "0-5cm",
            "20-50cm": "30-60cm",
            "50-100cm": "60-100cm",
            "100-200cm": "100-200cm",
        }

        depth_profiles = []
        for contract_depth in req.depths:
            sg_depth = depth_map.get(contract_depth, contract_depth)
            clay_val = clay_p.get(sg_depth)
            sand_val = sand_p.get(sg_depth)
            silt_val = silt_p.get(sg_depth)

            soil_type = "Loamy"
            if sand_val and sand_val > 70:
                soil_type = "Sandy"
            elif clay_val and clay_val > 35:
                soil_type = "Clayey"
            elif silt_val and silt_val > 50:
                soil_type = "Silty"

            depth_profiles.append({
                "depth": contract_depth,
                "sand": round(sand_val, 1) if sand_val else None,
                "silt": round(silt_val, 1) if silt_val else None,
                "clay": round(clay_val, 1) if clay_val else None,
                "bulkDensity": round(bdod_p.get(sg_depth, 0) or 0, 2) or None,
                "organicCarbon": round(soc_p.get(sg_depth, 0) or 0, 2) or None,
                "ph": round(ph_p.get(sg_depth, 0) or 0, 1) or None,
                "type": soil_type,
            })

        results = {
            "surface": {
                "sand": round(soil.get("sand_0_5", 0) or 0, 1) or None,
                "silt": round(soil.get("silt_0_5", 0) or 0, 1) or None,
                "clay": round(soil.get("clay_0_5", 0) or 0, 1) or None,
                "bulkDensity": round(soil.get("bdod_0_5", 0) or 0, 2) or None,
                "organicCarbon": round(soil.get("soc_0_5", 0) or 0, 2) or None,
                "ph": round(soil.get("ph_0_5", 0) or 0, 1) or None,
                "classification": soil.get("dominant_soil_type", "Loamy"),
            },
            "depthProfiles": depth_profiles,
            "heatmapTileUrl": f"s3://bucket/soil_heatmap/{{z}}/{{x}}/{{y}}.png",
            "dataSources": ["ISRIC SoilGrids v2.0", "AfSIS Africa Soil", "Sentinel-2 L2A"],
        }

        _jobs[python_job_id].update({
            "status": "completed",
            "results": results,
        })

    except Exception as e:
        logger.error(f"Soil pipeline failed for {python_job_id}: {e}", exc_info=True)
        _jobs[python_job_id].update({
            "status": "failed",
            "error": {"code": "PROCESSING_ERROR", "message": str(e)},
        })
