"""
Topography orchestration service for GeoSense AI.

Coordinates the three analysis modules — SoilGrids composition, DEM terrain
analysis, and the Model B bearing-capacity estimator — and assembles the
final unified JSON response. Each module is fault-isolated: a failure in one
is recorded in ``metadata.processing_notes`` rather than aborting the others.
"""

import logging
from datetime import datetime, timezone

import numpy as np

from app.services.soilgrids_service import get_soil_composition
from app.services.terrain_service import analyze_terrain

logger = logging.getLogger(__name__)

CLASS_TO_FLOORS = {
    "Low": "1-2",
    "Medium": "3-10",
    "High": "10+",
}

CLASS_TO_FOUNDATION = {
    "Low": "Deep foundation / piles likely required",
    "Medium": "Shallow possible; verify settlement",
    "High": "Shallow foundation likely adequate",
}

DISCLAIMER = (
    "Pre-qualification estimate. Physical borehole verification required "
    "before structural design."
)


def load_model_b(model_path: str) -> dict:
    """Load the Model B joblib bundle. Returns the bundle dict."""
    import joblib  # lazy import — only needed when a bundle is actually loaded

    logger.info("📦 Loading Model B bundle from %s", model_path)
    bundle = joblib.load(model_path)
    logger.info("✅ Model B loaded (version=%s)", bundle.get("version", "unknown"))
    return bundle


def _classify_bearing(value: float, config: dict) -> str:
    """Map a bearing-capacity value to a class via the bundle's bins."""
    bins = config["CLASS_BINS"]
    names = config["CLASS_NAMES"]
    idx = int(np.digitize(value, bins)) - 1
    idx = max(0, min(idx, len(names) - 1))
    return names[idx]


def _estimate_bearing_capacity(soil: dict, terrain: dict, bundle: dict) -> dict:
    """Run Model B (median/p10/p90) and build the bearing-capacity section."""
    feature_values = {
        "clay_0_5": soil.get("clay_0_5"),
        "sand_0_5": soil.get("sand_0_5"),
        "silt_0_5": soil.get("silt_0_5"),
        "bdod_0_5": soil.get("bdod_0_5"),
        "clay_30_60": soil.get("clay_30_60"),
        "sand_30_60": soil.get("sand_30_60"),
        "bdod_30_60": soil.get("bdod_30_60"),
        "slope": terrain.get("slope_mean_deg"),
        "TWI": terrain.get("twi_mean"),
    }

    feature_order = bundle["features"]
    row = [feature_values[name] for name in feature_order]
    missing = [name for name, value in zip(feature_order, row) if value is None]
    if missing:
        raise ValueError(f"Missing Model B features: {missing}")
    X = np.array([row], dtype="float64")

    median = float(bundle["model_median"].predict(X)[0])
    p10 = float(bundle["model_p10"].predict(X)[0])
    p90 = float(bundle["model_p90"].predict(X)[0])

    uncertainty_pct = round((p90 - p10) / median * 100, 2) if median else 0.0
    bearing_class = _classify_bearing(median, bundle["config"])

    return {
        "bearing_capacity_kpa": round(median, 2),
        "range_p10_p90_kpa": [round(p10, 2), round(p90, 2)],
        "uncertainty_pct": uncertainty_pct,
        "class": bearing_class,
        "floor_count": CLASS_TO_FLOORS.get(bearing_class, "1-2"),
        "foundation_type": CLASS_TO_FOUNDATION.get(bearing_class, CLASS_TO_FOUNDATION["Low"]),
        "disclaimer": DISCLAIMER,
    }


def _overall_risk(bearing: dict | None, terrain: dict | None) -> str:
    """Combine bearing class, flood share, and slope into an overall risk band."""
    risk_score = 0

    bearing_class = bearing.get("class") if bearing else None
    if bearing_class == "Low":
        risk_score += 3
    elif bearing_class == "Medium":
        risk_score += 1

    if terrain:
        flood_high = terrain["water_accumulation"]["high_risk_pct"]
        if flood_high > 30:
            risk_score += 2
        elif flood_high > 15:
            risk_score += 1

        slope_mean = terrain["slope_mean_deg"]
        if slope_mean > 15:
            risk_score += 2
        elif slope_mean > 5:
            risk_score += 1

    if risk_score >= 4:
        return "High"
    if risk_score >= 2:
        return "Medium"
    return "Low"


def _risk_summary(bearing: dict | None, terrain: dict | None) -> dict:
    """Build the top-level risk summary block."""
    flood_risk = False
    slope_risk = False
    if terrain:
        flood_risk = terrain["water_accumulation"]["high_risk_pct"] > 20
        slope_risk = terrain["site_summary"]["terrain_class"] in (
            "Moderately Sloped",
            "Steep",
        )
    return {
        "flood_risk": flood_risk,
        "slope_risk": slope_risk,
        "overall_risk": _overall_risk(bearing, terrain),
    }


def run_analysis(
    request: "object",
    model_b_bundle: dict,
    raster_dir: str = "/data/rasters/",
) -> dict:
    """Orchestrate soil + terrain + bearing analysis into the unified response."""
    notes: list[str] = []

    soil = None
    try:
        soil = get_soil_composition(request.geo_json)
    except Exception as e:  # fault isolation — record and continue
        logger.error("Soil analysis failed: %s", e, exc_info=True)
        notes.append(f"Soil analysis failed: {e}")

    terrain = None
    try:
        terrain = analyze_terrain(request.bbox, request.options, raster_dir)
    except Exception as e:
        logger.error("Terrain analysis failed: %s", e, exc_info=True)
        notes.append(f"Terrain analysis failed: {e}")

    bearing = None
    if soil is not None and terrain is not None and model_b_bundle is not None:
        try:
            bearing = _estimate_bearing_capacity(soil, terrain, model_b_bundle)
        except Exception as e:
            logger.error("Model B estimation failed: %s", e, exc_info=True)
            notes.append(f"Bearing capacity estimation failed: {e}")
    elif model_b_bundle is None:
        notes.append("Bearing capacity skipped: Model B bundle not loaded")
    else:
        notes.append("Bearing capacity skipped: missing soil or terrain inputs")

    if soil is None and terrain is None and bearing is None:
        raise RuntimeError("Complete analysis failure")

    model_b_version = (
        model_b_bundle.get("version", "unknown") if model_b_bundle else "unknown"
    )

    return {
        "parcel_id": request.parcel_id,
        "analyzed_at": datetime.now(timezone.utc).isoformat(),
        "soil": soil,
        "terrain": terrain,
        "bearing_capacity": bearing,
        "risk_summary": _risk_summary(bearing, terrain),
        "metadata": {
            "soil_source": "ISRIC SoilGrids v2.0",
            "dem_source": "Copernicus GLO-30",
            "dem_resolution_m": 250,
            "model_b_version": model_b_version,
            "processing_notes": notes,
        },
    }
