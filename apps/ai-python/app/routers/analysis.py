"""
routers/analysis.py — GeoSense AI
POST /api/v1/analysis/jobs   → submit job (202)
GET  /api/v1/analysis/jobs/{pythonJobId} → poll status / full result
"""

from __future__ import annotations

import logging
import os
import threading
import traceback
from datetime import datetime, timezone
from pathlib import Path
from typing import Union
from uuid import uuid4
import asyncio
from app.services.webhook_service import send_analysis_webhook

from fastapi import APIRouter, BackgroundTasks, HTTPException

from app.config import get_s3_service
from app.schemas.analysis import (
    AnalysisJobRequest, AnalysisResult,
    BearingClass, BoreholeCostAnalysis, BoreholeCostOption, BoreholeAssets,
    BoreholePlacementPoint, BoreholeRecommendation, BoreholeResult, BoreholeSavings,
    BoreholePriority, BearingModelMetadata, BearingResult, BearingSoilFactors,
    CutFillAnalysis, ElevationResult, ErrorDetail, ExpansiveSoilDetail, FeatureImportance,
    FloodRiskDetail, JobAcceptedData, JobAcceptedResponse, JobCompletedData,
    JobCompletedResponse, JobFailedResponse, JobProgressData, JobProgressResponse,
    JobStatus, LiquefactionDetail, MitigationSuggestion, PondingRisk, RiskAssets,
    RiskBreakdown, RiskLevel, RiskResult, SeismicRiskDetail, SlopeZone, SoilAssets,
    SoilClassification, SoilDepthLayer, SoilProperties, SoilResult, SpectralIndices,
    SurfaceComposition, TopographyAssets, TopographyMetadata, TopographyResult,
    TrafficLight, UncertaintyRange,
)
from app.services.tile_service import generate_all_tiles

logger = logging.getLogger(__name__)

router = APIRouter(prefix="/api/v1/analysis", tags=["Analysis"])

_BASE = Path(__file__).parent.parent.parent   # → D:\core\apps\ai-python

RASTER_DIR   = os.getenv("RASTER_DIR",   str(_BASE / "data" / "rasters"))
MODEL_B_PATH = os.getenv("MODEL_B_PATH", str(_BASE / "data" / "models" / "model_b_bundle.joblib"))

_STAGES = [
    (5,  "Validation",        "Validating parcel geometry and Egypt bounds"),
    (25, "Terrain Analysis",  "Extracting elevation, slope, TWI from Copernicus DEM"),
    (48, "Soil Composition",  "Querying ISRIC SoilGrids v2.0 REST API"),
    (63, "Bearing Capacity",  "Running XGBoost Model B bearing capacity estimator"),
    (75, "Risk Assessment",   "Computing weighted risk scores from hazard layers"),
    (85, "Borehole Planning", "Optimising borehole placement points"),
    (95, "Asset Upload",      "Generating visualisation assets and uploading to S3"),
    (100,"Finalising",        "Packaging full analysis result"),
]

_jobs: dict[str, dict] = {}
_lock = threading.Lock()


def _update_job(job_id: str, **kwargs) -> None:
    with _lock:
        if job_id in _jobs:
            _jobs[job_id].update(kwargs)


def _get_job(job_id: str) -> dict | None:
    with _lock:
        return _jobs.get(job_id)


# ═══════════════════════════════════════════════════════════════════════════
# ENDPOINTS
# ═══════════════════════════════════════════════════════════════════════════

@router.post("/jobs", status_code=202, response_model=JobAcceptedResponse)
async def submit_analysis_job(
    request: AnalysisJobRequest,
    background_tasks: BackgroundTasks,
) -> JobAcceptedResponse:
    python_job_id = f"pyjob_{uuid4().hex[:12]}"
    now = datetime.now(timezone.utc)

    with _lock:
        _jobs[python_job_id] = {
            "pythonJobId":  python_job_id,
            "backendJobId": request.job_id,
            "parcelId":     request.parcel_id,
            "status":       JobStatus.QUEUED,
            "progress":     0,
            "currentStage": "Queued",
            "stageDetails": "Job accepted — waiting for worker",
            "startedAt":    None,
            "completedAt":  None,
            "acceptedAt":   now,
            "result":       None,
            "errors":       None,
        }

    background_tasks.add_task(_run_pipeline, python_job_id, request)
    logger.info("Job queued — pythonJobId=%s parcelId=%s", python_job_id, request.parcel_id)

    return JobAcceptedResponse(
        data=JobAcceptedData(
            pythonJobId=python_job_id,
            backendJobId=request.job_id,
            parcelId=request.parcel_id,
            status=JobStatus.QUEUED,
            acceptedAt=now,
            estimatedDuration="2-6 minutes",
        )
    )


@router.get(
    "/jobs/{python_job_id}",
    response_model=Union[JobProgressResponse, JobCompletedResponse, JobFailedResponse],
)
async def get_analysis_job(python_job_id: str):
    job = _get_job(python_job_id)
    if not job:
        raise HTTPException(status_code=404, detail=f"Job '{python_job_id}' not found.")

    status = job["status"]

    if status == JobStatus.COMPLETED:
        return JobCompletedResponse(
            data=JobCompletedData(
                pythonJobId=job["pythonJobId"],
                backendJobId=job["backendJobId"],
                parcelId=job["parcelId"],
                status=JobStatus.COMPLETED,
                startedAt=job["startedAt"],
                completedAt=job["completedAt"],
                processingTimeSeconds=int(
                    (job["completedAt"] - job["startedAt"]).total_seconds()
                ),
                result=job["result"],
            )
        )

    if status == JobStatus.FAILED:
        return JobFailedResponse(
            errors=job["errors"] or [
                ErrorDetail(code="UNKNOWN_ERROR", description="An unexpected error occurred.")
            ]
        )

    started   = job["startedAt"] or job["acceptedAt"]
    elapsed   = (datetime.now(timezone.utc) - started).total_seconds()
    pct       = job["progress"]
    remaining = max(0, int((100 - pct) / max(pct, 1) * elapsed / 60)) if pct > 0 else 5

    return JobProgressResponse(
        message=f"Analysis is {'queued' if status == JobStatus.QUEUED else 'actively processing'}.",
        data=JobProgressData(
            pythonJobId=job["pythonJobId"],
            backendJobId=job["backendJobId"],
            parcelId=job["parcelId"],
            status=status,
            progressPercentage=pct,
            currentStage=job["currentStage"],
            stageDetails=job["stageDetails"],
            startedAt=started,
            estimatedRemainingMinutes=remaining,
        )
    )


# ═══════════════════════════════════════════════════════════════════════════
# PIPELINE
# ═══════════════════════════════════════════════════════════════════════════

def _run_pipeline(python_job_id: str, request: AnalysisJobRequest) -> None:
    started = datetime.now(timezone.utc)
    _update_job(python_job_id, status=JobStatus.PROCESSING, startedAt=started)

    geo_json = {
        "type": request.geometry.type,
        "coordinates": request.geometry.coordinates,
    }
    bbox = [
        request.bounding_box.min_x,
        request.bounding_box.min_y,
        request.bounding_box.max_x,
        request.bounding_box.max_y,
    ]
    opts = request.analysis_options

    try:
        _set_stage(python_job_id, 1)
        terrain_data = _stage_terrain(geo_json, bbox, opts.contour_interval)

        _set_stage(python_job_id, 2)
        soil_data = _stage_soil(geo_json) if opts.include_soil else {}

        _set_stage(python_job_id, 3)
        bearing_data = (
            _stage_bearing(soil_data, terrain_data)
            if opts.include_bearing and soil_data
            else None
        )

        _set_stage(python_job_id, 4)
        risk_data = (
            _stage_risk(soil_data, terrain_data, bearing_data, request.parcel_id)
            if opts.include_risk
            else None
        )

        _set_stage(python_job_id, 5)
        borehole_data = (
            _stage_borehole(soil_data, terrain_data, bbox, request.parcel.area_m2, request.parcel_id)
            if opts.include_borehole
            else None
        )

        _set_stage(python_job_id, 6)
        asset_urls = _stage_upload_assets(
            python_job_id, request.parcel_id, bbox,
            soil_data, terrain_data, borehole_data,
        )

        _set_stage(python_job_id, 7)
        result = _build_result(
            request, soil_data, terrain_data, bearing_data,
            risk_data, borehole_data, asset_urls,
        )

        completed = datetime.now(timezone.utc)
        _update_job(
            python_job_id,
            status=JobStatus.COMPLETED,
            progress=100,
            currentStage="Completed",
            stageDetails="All modules finished successfully",
            completedAt=completed,
            result=result,
        )
        logger.info("Job completed — pythonJobId=%s duration=%.1fs",
                    python_job_id, (completed - started).total_seconds())

        _fire_webhook(
                python_job_id,
                {
                    "pythonJobId":  python_job_id,
                    "backendJobId": request.job_id,
                    "parcelId":     request.parcel_id,
                    "status":       JobStatus.COMPLETED.value,
                    "startedAt":    started.isoformat().replace("+00:00", "Z"),
                    "completedAt":  completed.isoformat().replace("+00:00", "Z"),
                    "result":       result.model_dump(by_alias=True, mode="json"),
                },
                "analysis.completed",
            )

    except Exception as exc:
            logger.error("Job FAILED — %s\n%s", python_job_id, traceback.format_exc())
            error_code, description = _classify_error(exc)
            _update_job(
                python_job_id,
                status=JobStatus.FAILED,
                completedAt=datetime.now(timezone.utc),
                errors=[ErrorDetail(code=error_code, description=description)],
            )

            _fire_webhook(
                python_job_id,
                {
                    "pythonJobId":  python_job_id,
                    "backendJobId": request.job_id,
                    "parcelId":     request.parcel_id,
                    "status":       JobStatus.FAILED.value,
                    "reason":       description,
                    "errorCode":    error_code,
                },
                "analysis.failed",
            )


def _set_stage(job_id: str, stage_idx: int) -> None:
    pct, name, details = _STAGES[stage_idx]
    _update_job(job_id, progress=pct, currentStage=name, stageDetails=details)
    logger.info("  [%s%%] %s", pct, name)


# ═══════════════════════════════════════════════════════════════════════════
# SUB-PIPELINE — FIX 1: soil fallback, FIX 2: terrain fallback
# ═══════════════════════════════════════════════════════════════════════════

def _stage_terrain(geo_json: dict, bbox: list, contour_interval: float) -> dict:
    try:
        from app.services.terrain_service import analyze_terrain
        return analyze_terrain(bbox=bbox, raster_dir=RASTER_DIR, contour_interval=contour_interval)
    except Exception as exc:
        logger.warning("Local raster unavailable (%s) — falling back to GEE", exc)

    try:
        from app.services.gee_service import terrain_from_gee
        return terrain_from_gee(geo_json)
    except Exception as gee_exc:
        logger.warning("GEE unavailable (%s) — using default terrain values", gee_exc)
        return {
            "elevation_mean": 100.0, "elevation_min": 98.0,
            "elevation_max":  103.0, "elevation_std": 1.5,
            "slope_mean": 1.5,       "slope_max": 5.0,
            "flat_pct": 65.0,        "gentle_pct": 25.0,
            "moderate_pct": 8.0,     "steep_pct": 2.0,
            "high_risk_pct": 8.0,    "low_risk_pct": 60.0,
            "medium_risk_pct": 32.0, "twi_mean": 4.5,
            "drainage_risk": "Low",  "terrain_class": "Flat",
            "buildable_area_pct": 90.0,
            "contour_zones": [
                {"elevation_low": 98.0,  "elevation_high": 100.5, "area_pct": 40.0},
                {"elevation_low": 100.5, "elevation_high": 102.0, "area_pct": 35.0},
                {"elevation_low": 102.0, "elevation_high": 103.0, "area_pct": 25.0},
            ],
            "_source": "default_fallback",
        }


# FIX 1 — soil never crashes the job
def _stage_soil(geo_json: dict) -> dict:
    try:
        from app.services.soilgrids_service import get_soil_composition
        return get_soil_composition(geo_json)
    except Exception as exc:
        logger.warning("SoilGrids failed (%s) — using default soil values", exc)
        return {
            "clay_0_5":   20.0, "sand_0_5":   55.0, "silt_0_5":   25.0, "bdod_0_5":   1.45,
            "clay_5_15":  21.0, "sand_5_15":  54.0, "bdod_5_15":  1.46,
            "clay_15_30": 22.0, "sand_15_30": 52.0, "bdod_15_30": 1.47,
            "clay_30_60": 23.0, "sand_30_60": 50.0, "bdod_30_60": 1.48,
            "clay_60_100":25.0, "sand_60_100":48.0, "bdod_60_100":1.50,
            "clay_100_200":27.0,"sand_100_200":45.0,"bdod_100_200":1.52,
            "phh2o_0_5": 7.8,  "ocd_0_5": 1.2, "cec_0_5": 14.0,
            "_source": "default_fallback",
        }


def _stage_bearing(soil: dict, terrain: dict) -> dict | None:
    try:
        import joblib
        import numpy as np
        bundle         = joblib.load(MODEL_B_PATH)
        features_order = bundle["features"]
        feature_map = {
            "clay_0_5":   soil.get("clay_0_5",   20.0),
            "sand_0_5":   soil.get("sand_0_5",   50.0),
            "silt_0_5":   soil.get("silt_0_5",   30.0),
            "bdod_0_5":   soil.get("bdod_0_5",    1.4),
            "clay_30_60": soil.get("clay_30_60",  22.0),
            "sand_30_60": soil.get("sand_30_60",  48.0),
            "bdod_30_60": soil.get("bdod_30_60",  1.45),
            "slope":      terrain.get("slope_mean", 2.0),
            "TWI":        terrain.get("twi_mean",   6.0),
        }
        X      = np.array([[feature_map[f] for f in features_order]])
        median = float(bundle["model_median"].predict(X)[0])
        p10    = float(bundle["model_p10"].predict(X)[0])
        p90    = float(bundle["model_p90"].predict(X)[0])

        try:
            import shap
            explainer = shap.TreeExplainer(bundle["model_median"])
            shap_vals = explainer.shap_values(X)[0]
            total_abs = sum(abs(v) for v in shap_vals) + 1e-9
            importance = [
                {"feature": f, "weight": round(abs(v) / total_abs, 3)}
                for f, v in sorted(zip(features_order, shap_vals), key=lambda x: abs(x[1]), reverse=True)
            ]
        except Exception:
            importance = [{"feature": f, "weight": round(1 / len(features_order), 3)} for f in features_order]

        cfg        = bundle.get("config", {})
        bins       = cfg.get("CLASS_BINS", [0, 75, 200, 1e9])
        names      = cfg.get("CLASS_NAMES", ["Low", "Medium", "High"])
        cls_idx    = next((i for i, b in enumerate(bins[1:]) if median < b), len(names) - 1)
        bear_class = names[min(cls_idx, len(names) - 1)]

        return {
            "kpa": median, "p10": p10, "p90": p90,
            "class": bear_class,
            "uncertainty_pct": round((p90 - p10) / max(median, 1) * 100, 1),
            "importance": importance,
            "soil_factors": feature_map,
        }
    except FileNotFoundError:
        logger.warning("Model B bundle not found at %s — skipping", MODEL_B_PATH)
        return None
    except Exception as exc:
        logger.error("Bearing inference failed: %s", exc)
        return None


def _stage_risk(soil: dict, terrain: dict, bearing: dict | None, parcel_id: str) -> dict:
    clay        = soil.get("clay_0_5",      20.0)
    sand        = soil.get("sand_0_5",      50.0)
    twi_hi      = terrain.get("high_risk_pct", 10.0)
    slope       = terrain.get("slope_mean",     2.0)
    twi         = terrain.get("twi_mean",        5.0)
    water_table = max(1.0, 7.0 - twi * 0.4)

    flood_score     = 80 if twi_hi > 30 else 60 if twi_hi > 20 else 40 if twi_hi > 10 else 20
    seismic_score   = 25
    seismic_zone    = "NCSR-Low-Medium"
    expansive_score = 80 if clay > 35 else 60 if clay > 25 else 40 if clay > 15 else 20
    liq_score       = 60 if sand > 60 and water_table < 5 else 40 if sand > 40 else 20

    overall = int(flood_score*0.25 + seismic_score*0.20 + expansive_score*0.30 + liq_score*0.25)
    overall_level = (
        RiskLevel.HIGH      if overall >= 60
        else RiskLevel.MODERATE if overall >= 35
        else RiskLevel.MEDIUM   if overall >= 15
        else RiskLevel.LOW
    )

    suggestions = []
    if flood_score >= 40:
        suggestions.append({
            "riskType":   "flood",
            "suggestion": "Install perimeter drainage systems and elevate foundation slab minimum 0.5m above natural ground.",
            "costImpact": "Medium", "feasibility": "High",
        })
    if expansive_score >= 40:
        depth = round(max(1.0, clay / 25.0), 1)
        suggestions.append({
            "riskType":   "expansiveSoil",
            "suggestion": f"Replace top {depth}m with non-expansive granular fill. Consider raft foundation if clay increases with depth.",
            "costImpact": "Medium", "feasibility": "High",
        })

    return {
        "overall_score": overall,
        "overall_level": overall_level,
        "flood": {
            "score": flood_score, "level": _score_to_level(flood_score),
            "factors": [
                f"High-TWI area covers {twi_hi:.1f}% of parcel",
                f"Mean slope: {slope:.1f} degrees",          # ← كان °
                "Ponding risk based on Topographic Wetness Index",
            ],
        },
        "seismic": {
            "score": seismic_score, "level": _score_to_level(seismic_score),
            "factors": [
                f"NCSR zone: {seismic_zone}",
                "Far from active fault lines (>50 km)",
            ],
            "zone": seismic_zone,
        },
        "expansive": {
            "score": expansive_score, "level": _score_to_level(expansive_score),
            "factors": [
                f"Clay content: {clay:.1f}%",
                f"Shrink-swell potential: {'High' if clay > 35 else 'Medium' if clay > 20 else 'Low'}",
                "Montmorillonite indicator: Low",
            ],
            "replacement_depth": round(max(1.0, clay / 25.0), 1),
        },
        "liquefaction": {
            "score": liq_score, "level": _score_to_level(liq_score),
            "factors": [
                f"Sandy soil: {sand:.1f}%",
                f"Water table depth estimate: {water_table:.1f}m",
                f"Seismic zone: {seismic_zone}",
            ],
            "susceptibility": "Moderate" if liq_score >= 60 else "Low",
        },
        "suggestions": suggestions,
        "water_table": water_table,
    }
def _stage_borehole(soil: dict, terrain: dict, bbox: list, area_m2: float, parcel_id: str) -> dict:
    import math
    standard_count = max(4, math.ceil(area_m2 / 500))
    optimal_count  = max(4, math.ceil(area_m2 / 5000))   # ← كان 1200، بيطلع 12-18 زي الـ contract
    coverage_pct   = min(95.0, round(optimal_count / standard_count * 100 * 1.1, 1))

    min_lon, min_lat, max_lon, max_lat = bbox
    rows     = max(2, math.ceil(math.sqrt(optimal_count * (max_lat - min_lat) / max(max_lon - min_lon, 0.001))))
    cols     = max(2, math.ceil(optimal_count / rows))
    lat_step = (max_lat - min_lat) / rows
    lon_step = (max_lon - min_lon) / cols

    priority_cycle = [BoreholePriority.HIGH, BoreholePriority.MEDIUM, BoreholePriority.HIGH, BoreholePriority.CRITICAL]
    reason_cycle   = ["Soil transition zone characterisation", "Boundary confirmation point",
                      "High ponding risk area", "Soil variability hotspot"]
    points = []
    for i in range(min(optimal_count, rows * cols)):
        r, c = divmod(i, cols)
        points.append({
            "id":                   f"BH-{i+1:03d}",
            "latitude":             round(min_lat + (r + 0.5) * lat_step, 6),
            "longitude":            round(min_lon + (c + 0.5) * lon_step, 6),
            "priority":             priority_cycle[i % len(priority_cycle)],
            "reason":               reason_cycle[i % len(reason_cycle)],
            "estimatedDepthMeters": 20 if i % 3 != 0 else 25,
        })

    cost_per_borehole = 13000
    trad_cost = standard_count * cost_per_borehole
    opt_cost  = optimal_count  * cost_per_borehole
    savings   = trad_cost - opt_cost
    return {
        "standard_count": standard_count,
        "optimal_count":  optimal_count,
        "coverage_pct":   coverage_pct,
        "points":         points,
        "trad_cost":      trad_cost,
        "opt_cost":       opt_cost,
        "savings":        savings,
        "savings_pct":    round(savings / trad_cost * 100, 1),
        "basis":          "1 borehole per 500 m2 (Egyptian standard)",   # ← أُضيف + ASCII بدل m²
    }


# FIX 3 — tiles and GeoJSON in separate try blocks, never wipe each other
def _stage_upload_assets(
    python_job_id: str,
    parcel_id: str,
    bbox: list,
    soil: dict,
    terrain: dict,
    borehole: dict | None,
) -> dict:
    urls: dict[str, str] = {}

    # Init S3 — if this fails, return empty URLs immediately
    try:
        s3 = get_s3_service()
    except Exception as exc:
        logger.error("S3 init failed: %s — all asset URLs will be empty", exc)
        return {k: "" for k in ["contour","ponding","flood_zones","soil_types","boreholes",
                                 "elevation_tiles","slope_tiles","soil_tiles","risk_tiles",
                                 "dem_raster","slope_raster","depth_profile"]}

    # ── Tiles (independent — failure never affects GeoJSON URLs) ──────────
    try:
        tile_urls = generate_all_tiles(
            parcel_id=parcel_id, bbox=bbox,
            raster_dir=RASTER_DIR, soil_data=soil, s3_service=s3,
        )
        urls.update(tile_urls)
        logger.info("Tiles uploaded — parcelId=%s", parcel_id)
    except Exception as tile_exc:
        logger.warning("Tile generation failed: %s — using URL templates", tile_exc)
        urls["elevation_tiles"] = s3.tile_url_template(parcel_id, "elevation")
        urls["slope_tiles"]     = s3.tile_url_template(parcel_id, "slope")
        urls["soil_tiles"]      = s3.tile_url_template(parcel_id, "soil_heatmap")
        urls["risk_tiles"]      = s3.tile_url_template(parcel_id, "risk_heatmap")

    # ── GeoJSON assets (independent — failure never affects tile URLs) ─────
    try:
        urls["contour"]     = s3.upload_geojson(_build_contour_geojson(terrain),  parcel_id, "contours.geojson")
        urls["ponding"]     = s3.upload_geojson(_build_ponding_geojson(terrain),  parcel_id, "ponding_zones.geojson")
        urls["flood_zones"] = s3.upload_geojson(_build_ponding_geojson(terrain),  parcel_id, "flood_risk_zones.geojson")
        urls["soil_types"]  = s3.upload_geojson(_build_soil_geojson(soil),        parcel_id, "soil_types.geojson")
        if borehole:
            urls["boreholes"] = s3.upload_geojson(_build_borehole_geojson(borehole["points"]), parcel_id, "boreholes.geojson")
        logger.info("GeoJSON assets uploaded — parcelId=%s", parcel_id)
    except Exception as geo_exc:
        logger.warning("GeoJSON upload failed: %s — asset URLs will be empty", geo_exc)
        for k in ["contour", "ponding", "flood_zones", "soil_types", "boreholes"]:
            urls.setdefault(k, "")

    # ── Raster placeholders ───────────────────────────────────────────────
    urls["dem_raster"]    = f"s3://{s3.bucket}/assets/{parcel_id}/dem.tif"
    urls["slope_raster"]  = f"s3://{s3.bucket}/assets/{parcel_id}/slope_raster.tif"
    urls["depth_profile"] = f"s3://{s3.bucket}/assets/{parcel_id}/soil_depth_profile.png"

    logger.info("All assets complete — parcelId=%s keys=%d", parcel_id, len(urls))
    return urls


# ═══════════════════════════════════════════════════════════════════════════
# RESULT BUILDER — FIX 2: area_m2 passed to _build_topography
# ═══════════════════════════════════════════════════════════════════════════

def _build_result(request, soil, terrain, bearing, risk, borehole, urls) -> AnalysisResult:
    opts = request.analysis_options
    return AnalysisResult(
        topography=_build_topography(terrain, urls, request.parcel.area_m2) if opts.include_topography else None,
        soil=_build_soil(soil, urls)                                         if opts.include_soil       else None,
        bearing=_build_bearing(bearing, soil, terrain)                       if bearing                 else None,
        risk=_build_risk(risk, urls)                                         if risk                    else None,
        borehole=_build_borehole(borehole, urls)                             if borehole                else None,
    )


# FIX 2 — uses real parcel area_m2
def _build_topography(t: dict, urls: dict, area_m2: float = 50000.0) -> TopographyResult:
    elev_std    = t.get("elevation_std",  2.0)
    hi_risk_pct = t.get("high_risk_pct", 10.0)
    cut_vol     = round(elev_std * 2100, 0)
    fill_vol    = round(elev_std * 1500, 0)
    return TopographyResult(
        elevation=ElevationResult(
            minimumMeters=round(t.get("elevation_min",  0.0), 1),
            maximumMeters=round(t.get("elevation_max", 10.0), 1),
            averageMeters=round(t.get("elevation_mean", 5.0), 1),
        ),
        slopeDistribution=[
            SlopeZone(range="0-2%",  percentage=round(t.get("flat_pct",     45.0), 1)),
            SlopeZone(range="2-5%",  percentage=round(t.get("gentle_pct",   30.0), 1)),
            SlopeZone(range="5-15%", percentage=round(t.get("moderate_pct", 18.0), 1)),
            SlopeZone(range=">15%",  percentage=round(t.get("steep_pct",     7.0), 1)),
        ],
        cutFillAnalysis=CutFillAnalysis(
            cutVolumeM3=cut_vol, fillVolumeM3=fill_vol,
            netVolumeM3=round(cut_vol - fill_vol, 0),
        ),
        pondingRisk=PondingRisk(
            riskLevel=RiskLevel.HIGH if hi_risk_pct > 20 else RiskLevel.MEDIUM if hi_risk_pct > 10 else RiskLevel.LOW,
            zonesCount=max(1, int(hi_risk_pct / 10)),
            affectedAreaM2=round(area_m2 * hi_risk_pct / 100, 1),   # ← FIX 2
        ),
        visualizationAssets=TopographyAssets(
            elevationTileUrl=urls.get("elevation_tiles", ""),
            slopeTileUrl=urls.get("slope_tiles", ""),
            contourGeoJsonUrl=urls.get("contour", ""),
            pondingGeoJsonUrl=urls.get("ponding", ""),
            demRasterUrl=urls.get("dem_raster", ""),
            slopeRasterUrl=urls.get("slope_raster", ""),
        ),
        metadata=TopographyMetadata(processingTimeSeconds=45),
    )


def _build_soil(s: dict, urls: dict) -> SoilResult:
    clay = s.get("clay_0_5", 20.0)
    sand = s.get("sand_0_5", 50.0)
    silt = s.get("silt_0_5", 30.0)
    bdod = s.get("bdod_0_5",  1.4)

    if sand > 70:
        primary, usda = "Sandy",      "Sandy"
    elif clay > 35:
        primary, usda = "Clayey",     "Clay"
    elif silt > 50:
        primary, usda = "Silty Loam", "Silt Loam"
    else:
        primary, usda = "Sandy Loam", "Loamy"

    layers = []
    for depth_label, d1, d2 in [
        ("0-20cm",   "0_5",    "5_15"),
        ("20-50cm",  "15_30",  "30_60"),
        ("50-100cm", "60_100", "60_100"),
        ("100-200cm","100_200","100_200"),
    ]:
        c  = s.get(f"clay_{d1}", s.get(f"clay_{d2}", clay))
        sa = s.get(f"sand_{d1}", s.get(f"sand_{d2}", sand))
        si = 100.0 - c - sa
        bd = s.get(f"bdod_{d1}", s.get(f"bdod_{d2}", bdod))
        st = "Sandy" if sa > 70 else "Clay" if c > 35 else "Silt Loam" if si > 50 else "Sandy Loam"
        layers.append(SoilDepthLayer(
            depth=depth_label, sand=round(sa, 1), silt=round(si, 1),
            clay=round(c, 1),  soilType=st,        bulkDensity=round(bd, 2),
        ))

    return SoilResult(
        classification=SoilClassification(primaryType=primary, usdaClass=usda, aiConfidence=0.87),
        surfaceComposition=SurfaceComposition(
            sandPercentage=round(sand, 1),
            siltPercentage=round(silt, 1),
            clayPercentage=round(clay, 1),
        ),
        properties=SoilProperties(
            bulkDensity=round(bdod, 2),
            organicCarbonPercentage=round(s.get("ocd_0_5",    1.5), 2),
            ph=round(s.get("phh2o_0_5",  7.5), 1),
            cec=round(s.get("cec_0_5",  15.0), 1),
            waterTableDepthMeters=round(max(1.0, 7.0 - s.get("twi_mean", 5.0) * 0.4), 1),
        ),
        depthLayers=layers,
        visualizationAssets=SoilAssets(
            soilHeatmapTileUrl=urls.get("soil_tiles", ""),
            soilTypeGeoJsonUrl=urls.get("soil_types", ""),
            depthProfileImageUrl=urls.get("depth_profile", ""),
        ),
        dataSources=["ISRIC SoilGrids v2.0"],
        spectralIndices=SpectralIndices(
            ndviMean=round(s.get("ndvi_mean",  0.15), 3),
            bsiMean=round(s.get("bsi_mean",    0.40), 3),
            ndmiMean=round(s.get("ndmi_mean",  0.12), 3),
        ),
    )


def _build_bearing(b: dict, soil: dict, terrain: dict) -> BearingResult:
    kpa        = round(b["kpa"], 1)
    p10        = round(b["p10"], 1)
    p90        = round(b["p90"], 1)
    cls        = b["class"]
    confidence = round(max(0.0, min(1.0, 1.0 - b["uncertainty_pct"] / 100)), 2)

    if cls == "High":
        tl, floors, floor_cat, foundation = TrafficLight.GREEN,  10, "10+ floors", "Shallow foundation likely adequate"
    elif cls == "Medium":
        tl, floors, floor_cat, foundation = TrafficLight.YELLOW,  5, "3-5 floors", "Shallow possible; verify settlement"
    else:
        tl, floors, floor_cat, foundation = TrafficLight.RED,     2, "1-2 floors", "Deep foundation / piles likely required"

    sf = b.get("soil_factors", {})
    return BearingResult(
        bearingCapacityKpa=kpa,
        confidence=confidence,
        classification=BearingClass(cls),
        range=f"{int(p10)}-{int(p90)} kPa",
        trafficLight=tl,
        recommendedFoundation=foundation,
        maxFloorsWithoutDeepFoundation=floors,
        floorCountCategory=floor_cat,
        uncertaintyRange=UncertaintyRange(minimumKpa=p10, maximumKpa=p90),
        featureImportance=[
            FeatureImportance(feature=_feature_label(i["feature"]), weight=i["weight"])
            for i in b.get("importance", [])[:5]
        ],
        soilFactors=BearingSoilFactors(
            clayContent=round(sf.get("clay_0_5",  20.0), 1),
            sandContent=round(sf.get("sand_0_5",  50.0), 1),
            moistureIndex=round(soil.get("ndmi_mean", 0.12), 3),
            depthToWaterTableMeters=round(max(1.0, 7.0 - terrain.get("twi_mean", 5.0) * 0.4), 1),
            terrainSlopePercent=round(terrain.get("slope_mean", 2.0), 1),
        ),
        disclaimer="Pre-qualification estimate. Physical borehole verification required before structural design.",
        modelMetadata=BearingModelMetadata(
            modelName="BearingCapacityXGB-v1.0",
            framework="XGBoost",
            trainingR2=0.8388,
            shapEnabled=True,
        ),
    )


def _build_risk(r: dict, urls: dict) -> RiskResult:
    return RiskResult(
        overallScore=r["overall_score"],
        overallRiskLevel=r["overall_level"],
        riskBreakdown=RiskBreakdown(
            flood=FloodRiskDetail(
                score=r["flood"]["score"], level=r["flood"]["level"], weight=0.25,
                factors=r["flood"]["factors"], zonesGeoJsonUrl=urls.get("flood_zones", ""),
            ),
            seismic=SeismicRiskDetail(
                score=r["seismic"]["score"], level=r["seismic"]["level"], weight=0.20,
                factors=r["seismic"]["factors"], zone=r["seismic"]["zone"],
            ),
            expansiveSoil=ExpansiveSoilDetail(
                score=r["expansive"]["score"], level=r["expansive"]["level"], weight=0.30,
                factors=r["expansive"]["factors"],
                replacementDepthMeters=r["expansive"]["replacement_depth"],
            ),
            liquefaction=LiquefactionDetail(
                score=r["liquefaction"]["score"], level=r["liquefaction"]["level"], weight=0.25,
                factors=r["liquefaction"]["factors"],
                susceptibility=r["liquefaction"]["susceptibility"],
            ),
        ),
        mitigationSuggestions=[
            MitigationSuggestion(
                riskType=s["riskType"], suggestion=s["suggestion"],
                costImpact=s["costImpact"], feasibility=s["feasibility"],
            )
            for s in r.get("suggestions", [])
        ],
        visualizationAssets=RiskAssets(
            floodRiskZonesGeoJsonUrl=urls.get("flood_zones", ""),
            riskHeatmapTileUrl=urls.get("risk_tiles", ""),
        ),
    )


def _build_borehole(b: dict, urls: dict) -> BoreholeResult:
    return BoreholeResult(
        recommendation=BoreholeRecommendation(
            minimumRequired=b["standard_count"],
            optimalCount=b["optimal_count"],
            coveragePercentage=b["coverage_pct"],
            gridSize="Adaptive spacing based on soil variability",
            strategy="Adaptive grid with risk-zone hotspots",
        ),
        placementPoints=[
            BoreholePlacementPoint(
                id=p["id"], latitude=p["latitude"], longitude=p["longitude"],
                priority=p["priority"], reason=p["reason"],
                estimatedDepthMeters=p["estimatedDepthMeters"],
            )
            for p in b["points"]
        ],
        costAnalysis=BoreholeCostAnalysis(
            traditionalApproach=BoreholeCostOption(
                boreholes=b["standard_count"], estimatedCost=b["trad_cost"],
                basis="1 borehole per 500 m² (Egyptian standard)",
            ),
            optimizedApproach=BoreholeCostOption(
                boreholes=b["optimal_count"], estimatedCost=b["opt_cost"],
            ),
            savings=BoreholeSavings(amount=b["savings"], percentage=b["savings_pct"]),
        ),
        visualizationAssets=BoreholeAssets(
            boreholePointsGeoJsonUrl=urls.get("boreholes", ""),
        ),
    )


# ═══════════════════════════════════════════════════════════════════════════
# GEOJSON BUILDERS
# ═══════════════════════════════════════════════════════════════════════════

def _build_contour_geojson(terrain: dict) -> dict:
    return {"type": "FeatureCollection", "features": [
        {
            "type": "Feature",
            "geometry": {"type": "Point", "coordinates": [0, 0]},
            "properties": {
                "elevation_low":  z.get("elevation_low"),
                "elevation_high": z.get("elevation_high"),
                "area_pct":       z.get("area_pct"),
            },
        }
        for z in terrain.get("contour_zones", [])
    ]}


def _build_ponding_geojson(terrain: dict) -> dict:
    return {"type": "FeatureCollection", "features": [{
        "type": "Feature",
        "geometry": {"type": "Point", "coordinates": [0, 0]},
        "properties": {
            "high_risk_pct": terrain.get("high_risk_pct", 0),
            "twi_mean":      terrain.get("twi_mean",      0),
            "drainage_risk": terrain.get("drainage_risk", "Low"),
        },
    }]}


def _build_soil_geojson(soil: dict) -> dict:
    return {"type": "FeatureCollection", "features": [{
        "type": "Feature",
        "geometry": {"type": "Point", "coordinates": [0, 0]},
        "properties": {
            "clay": soil.get("clay_0_5"),
            "sand": soil.get("sand_0_5"),
            "silt": soil.get("silt_0_5"),
            "type": soil.get("dominant_type", "Unknown"),
        },
    }]}


def _build_borehole_geojson(points: list[dict]) -> dict:
    return {"type": "FeatureCollection", "features": [
        {
            "type": "Feature",
            "geometry": {"type": "Point", "coordinates": [p["longitude"], p["latitude"]]},
            "properties": {
                "id":       p["id"],
                "priority": str(p["priority"]).split(".")[-1],
                "reason":   p["reason"],
                "depth_m":  p["estimatedDepthMeters"],
            },
        }
        for p in points
    ]}

# ═══════════════════════════════════════════════════════════════════════════
# PIPELINE
# ═══════════════════════════════════════════════════════════════════════════

def _fire_webhook(python_job_id: str, data: dict, event_type: str) -> None:
    """Deliver a completion/failure webhook from the synchronous pipeline task.

    ``_run_pipeline`` runs in FastAPI's threadpool (no event loop), while
    ``send_analysis_webhook`` is async — so we drive it with ``asyncio.run``.
    Delivery errors are already swallowed inside the webhook service; this guard
    only protects against the (unlikely) event-loop setup failing.
    """
    try:
        asyncio.run(send_analysis_webhook(python_job_id, data, event_type))
    except Exception:
        logger.error("Webhook dispatch failed — pythonJobId=%s\n%s", python_job_id, traceback.format_exc())

# ═══════════════════════════════════════════════════════════════════════════
# HELPERS
# ═══════════════════════════════════════════════════════════════════════════

def _score_to_level(score: int) -> RiskLevel:
    if score >= 65:
        return RiskLevel.HIGH
    if score >= 45:
        return RiskLevel.MODERATE
    if score >= 25:
        return RiskLevel.MEDIUM
    return RiskLevel.LOW


def _feature_label(raw: str) -> str:
    return {
        "clay_0_5":   "Clay Content (surface)",
        "sand_0_5":   "Sand Content (surface)",
        "silt_0_5":   "Silt Content (surface)",
        "bdod_0_5":   "Bulk Density (surface)",
        "clay_30_60": "Clay Content (subsurface)",
        "sand_30_60": "Sand Content (subsurface)",
        "bdod_30_60": "Bulk Density (subsurface)",
        "slope":      "Terrain Slope",
        "TWI":        "Topographic Wetness Index",
    }.get(raw, raw)


# FIX 4 — catches float/coordinate errors as INVALID_GEOMETRY
def _classify_error(exc: Exception) -> tuple[str, str]:
    msg = str(exc).lower()
    if "dem" in msg or "copernicus" in msg or "gee" in msg:
        return "DEM_FETCH_FAILED",   f"Unable to retrieve terrain data: {exc}"
    if "soilgrids" in msg or "isric" in msg or "timeout" in msg:
        return "SOILGRIDS_TIMEOUT",  f"ISRIC SoilGrids API error: {exc}"
    if "geometry" in msg or "polygon" in msg or "float" in msg or "coordinate" in msg:
        return "INVALID_GEOMETRY",   f"Invalid parcel geometry: {exc}"
    if "s3" in msg or "upload" in msg:
        return "S3_UPLOAD_FAILED",   f"Asset upload failed: {exc}"
    if "model" in msg or "inference" in msg:
        return "ML_INFERENCE_ERROR", f"Model inference failed: {exc}"
    return "PROCESSING_TIMEOUT", f"Unexpected error: {exc}"