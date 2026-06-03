"""
GeoSense topography router — Day 2 complete.
Adds: terrain derivatives, slope classification,
      elevation stats, cut/fill, ponding, contours, tiles.
"""

import logging, uuid, time, os
from datetime import datetime, timezone

from fastapi import APIRouter, HTTPException, BackgroundTasks

from app.schemas.topography import (
    TopographyRequest,
    JobAccepted,
    JobProcessing,
)
from app.services.gee_service       import validate_bbox_egypt, export_dem_for_parcel
from app.services.terrain_service   import compute_terrain_derivatives
from app.services.topography_service import (
    compute_elevation_stats,
    classify_slope,
    compute_cut_fill,
    generate_contours,
    detect_ponding_zones,
)
from app.services.tiles_service     import export_all_tiles
from app.services.redis_service     import set_status, get_status
from app.config import settings

logger = logging.getLogger(__name__)

router = APIRouter(prefix="/api/v1/topography", tags=["topography"])


# ── POST /api/v1/topography/jobs ──────────────────────────────
@router.post("/jobs", response_model=JobAccepted, status_code=202)
async def submit_topography_job(req: TopographyRequest,
                                 bg: BackgroundTasks):
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

    python_job_id = str(uuid.uuid4())
    accepted_at   = datetime.now(timezone.utc).isoformat()

    set_status(python_job_id, "queued", 0, req.parcel_id)
    bg.add_task(_run_pipeline, python_job_id, req)

    return JobAccepted(
        python_job_id = python_job_id,
        parcel_id     = req.parcel_id,
        status        = "queued",
        accepted_at   = accepted_at,
    )


# ── GET /api/v1/topography/jobs/{python_job_id} ───────────────
@router.get("/jobs/{python_job_id}", response_model=JobProcessing)
async def get_topography_status(python_job_id: str):
    job = get_status(python_job_id)

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
    out_dir = os.path.join(settings.local_out_dir, python_job_id)
    os.makedirs(out_dir, exist_ok=True)

    def upd(status: str, progress: int, results=None, error=None):
        set_status(python_job_id, status, progress,
                   req.parcel_id, results=results, error=error)

    try:
        t0 = time.time()

        # ── Step 1: Export DEM from GEE ──────────────────────
        upd("processing", 10)
        logger.info(f"[{python_job_id}] Exporting DEM from GEE...")
        dem_path = export_dem_for_parcel(req.bbox, python_job_id, out_dir)

        # ── Step 2: Terrain derivatives ──────────────────────
        upd("processing", 25)
        logger.info(f"[{python_job_id}] Computing terrain derivatives...")
        derivs = compute_terrain_derivatives(dem_path, out_dir)

        # ── Step 3: Elevation stats ───────────────────────────
        upd("processing", 40)
        logger.info(f"[{python_job_id}] Computing elevation stats...")
        elevation_stats = compute_elevation_stats(dem_path)

        # ── Step 4: Slope classification ──────────────────────
        upd("processing", 50)
        logger.info(f"[{python_job_id}] Classifying slope...")
        _, slope_distribution = classify_slope(derivs["slope"])

        # ── Step 5: Cut & Fill ────────────────────────────────
        upd("processing", 62)
        logger.info(f"[{python_job_id}] Computing cut & fill volumes...")
        cut_fill = compute_cut_fill(
            dem_path,
            req.options.reference_elevation
        )

        # ── Step 6: Ponding zones ─────────────────────────────
        upd("processing", 72)
        logger.info(f"[{python_job_id}] Detecting ponding zones...")
        ponding = detect_ponding_zones(
            derivs["TWI"],
            derivs["slope"],
            out_dir,
            req.options.twi_threshold
        )

        # ── Step 7: Contour lines ─────────────────────────────
        upd("processing", 82)
        logger.info(f"[{python_job_id}] Generating contour lines...")
        contour_path = generate_contours(
            dem_path,
            out_dir,
            req.options.contour_interval_m
        )

        # ── Step 8: PNG tiles ─────────────────────────────────
        upd("processing", 92)
        logger.info(f"[{python_job_id}] Exporting PNG tiles...")
        tile_urls = export_all_tiles(
            dem_path,
            derivs["slope"],
            out_dir,
            req.parcel_id
        )

        # ── Step 9: Assemble results ──────────────────────────
        processing_time = round(time.time() - t0, 1)
        logger.info(f"[{python_job_id}] Done in {processing_time}s")

        results = {
            "elevation": elevation_stats,
            "slope": {
                "distribution":          slope_distribution,
                "classified_raster_url": tile_urls.get("slope", "")
            },
            "cut_fill":  cut_fill,
            "ponding":   ponding,
            "contours": {
                "geo_json_path": contour_path,
                "interval_m":   req.options.contour_interval_m
            },
            "tiles":                    tile_urls,
            "processing_time_seconds":  processing_time,
        }

        upd("completed", 100, results=results)

    except Exception as e:
        logger.error(f"[{python_job_id}] Pipeline failed: {e}", exc_info=True)
        upd("failed", 0, error={
            "code":      "PROCESSING_ERROR",
            "message":   str(e),
            "retryable": True
        })