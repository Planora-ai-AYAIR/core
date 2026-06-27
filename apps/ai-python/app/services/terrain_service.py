"""
Terrain analysis service for GeoSense AI.

Clips the pre-computed Egypt-wide Copernicus DEM feature raster to the input
parcel bounding box, computes elevation / slope / wetness statistics, derives
slope and water-accumulation zones, and builds elevation contour zones for
map rendering.

Input bbox is WGS84 (EPSG:4326); the raster is UTM Zone 36N (EPSG:32636),
so the bbox is reprojected before any raster operation.
"""

import logging
import os
import string

import numpy as np
import rasterio
from rasterio.mask import mask
from pyproj import Transformer
from shapely.geometry import box, mapping

logger = logging.getLogger(__name__)

RASTER_FILENAME = "copernicus_dem_features_egypt_250m.tif"
RASTER_RESOLUTION_M = 250
RASTER_CRS = "EPSG:32636"

# Band index map (1-indexed for rasterio)
BANDS = {
    "elevation": 1,
    "slope": 2,
    "aspect": 3,
    "curvature": 4,
    "TWI": 5,
    "TRI": 6,
    "SRR": 7,
}

_TRANSFORMER = Transformer.from_crs("EPSG:4326", RASTER_CRS, always_xy=True)


def _reproject_bbox(bbox: list[float]) -> tuple[float, float, float, float]:
    """Reproject a WGS84 bbox to EPSG:32636 (minx, miny, maxx, maxy)."""
    minx, miny = _TRANSFORMER.transform(bbox[0], bbox[1])
    maxx, maxy = _TRANSFORMER.transform(bbox[2], bbox[3])
    return minx, miny, maxx, maxy


def clip_band(raster_path: str, bbox_32636: tuple, band_idx: int) -> np.ndarray:
    """Clip a single raster band to the projected bbox, returning a 2-D array."""
    with rasterio.open(raster_path) as src:
        geom = mapping(box(*bbox_32636))
        clipped, _ = mask(src, [geom], crop=True, nodata=np.nan, indexes=band_idx)
        return np.asarray(clipped, dtype="float64").squeeze()


def _pct(condition: np.ndarray, total: int) -> float:
    """Percentage of valid pixels satisfying a boolean condition."""
    if total == 0:
        return 0.0
    return round(float(np.count_nonzero(condition)) / total * 100, 2)


def _slope_zones(slope: np.ndarray, valid: np.ndarray, total: int) -> dict:
    """Distribution of slope across flat/gentle/moderate/steep bands."""
    flat = valid & (slope < 2)
    gentle = valid & (slope >= 2) & (slope < 5)
    moderate = valid & (slope >= 5) & (slope < 15)
    steep = valid & (slope >= 15)
    return {
        "flat_pct": _pct(flat, total),
        "gentle_pct": _pct(gentle, total),
        "moderate_pct": _pct(moderate, total),
        "steep_pct": _pct(steep, total),
    }


def _water_accumulation(twi: np.ndarray, valid: np.ndarray, total: int, threshold: float) -> dict:
    """Water accumulation risk zones derived from the Topographic Wetness Index."""
    high = valid & (twi > threshold)
    low = valid & (twi < 5)
    high_pct = _pct(high, total)
    low_pct = _pct(low, total)
    medium_pct = round(max(0.0, 100.0 - high_pct - low_pct), 2)
    return {
        "high_risk_pct": high_pct,
        "medium_risk_pct": medium_pct,
        "low_risk_pct": low_pct,
        "twi_threshold_used": threshold,
    }


def _contour_zones(
    elevation: np.ndarray,
    valid: np.ndarray,
    total: int,
    min_elev: float,
    max_elev: float,
    contour_interval_m: float,
) -> list[dict]:
    """Split the elevation range into N equal bands and report area share."""
    span = max_elev - min_elev
    if span <= 0 or contour_interval_m <= 0:
        n_zones = 4
    else:
        n_zones = max(4, min(6, int(span / contour_interval_m)))
        n_zones = max(1, n_zones)

    edges = np.linspace(min_elev, max_elev, n_zones + 1)
    zones: list[dict] = []
    for i in range(n_zones):
        lo, hi = float(edges[i]), float(edges[i + 1])
        if i == n_zones - 1:
            in_band = valid & (elevation >= lo) & (elevation <= hi)
        else:
            in_band = valid & (elevation >= lo) & (elevation < hi)
        zones.append({
            "label": f"Zone {string.ascii_uppercase[i]} ({lo:.1f}–{hi:.1f}m)",
            "min_elev_m": round(lo, 2),
            "max_elev_m": round(hi, 2),
            "area_pct": _pct(in_band, total),
        })
    return zones


def _terrain_class(slope_mean: float) -> str:
    """Map mean slope to a qualitative terrain class."""
    if slope_mean < 2:
        return "Flat"
    if slope_mean < 5:
        return "Gently Sloped"
    if slope_mean < 15:
        return "Moderately Sloped"
    return "Steep"


def _drainage_risk(high_risk_pct: float) -> str:
    """Map high-risk water accumulation share to a drainage risk level."""
    if high_risk_pct > 20:
        return "High"
    if high_risk_pct > 5:
        return "Medium"
    return "Low"


def analyze_terrain(
    bbox: list[float],
    options: "object",
    raster_dir: str = "/data/rasters/",
) -> dict:
    """Clip Egypt DEM raster to bbox and compute terrain statistics."""
    raster_path = os.path.join(raster_dir, RASTER_FILENAME)
    if not os.path.exists(raster_path):
        raise FileNotFoundError(f"Raster not found: {raster_path}")

    contour_interval_m = getattr(options, "contour_interval_m", 0.5)
    twi_threshold = getattr(options, "twi_threshold", 8.0)

    bbox_32636 = _reproject_bbox(bbox)
    logger.info("⛰️  Clipping DEM features for bbox (32636): %s", bbox_32636)

    try:
        elevation = clip_band(raster_path, bbox_32636, BANDS["elevation"])
        slope = clip_band(raster_path, bbox_32636, BANDS["slope"])
        twi = clip_band(raster_path, bbox_32636, BANDS["TWI"])
    except (ValueError, rasterio.errors.RasterioError) as e:
        # rasterio raises when the requested window does not overlap the raster
        logger.error("❌ Raster clip failed: %s", e)
        raise ValueError("Parcel outside Egypt raster coverage") from e

    valid = ~np.isnan(elevation)
    total = int(np.count_nonzero(valid))
    if total == 0:
        raise ValueError("No valid elevation data for this parcel")

    elev_mean = float(np.nanmean(elevation))
    elev_min = float(np.nanmin(elevation))
    elev_max = float(np.nanmax(elevation))
    elev_std = float(np.nanstd(elevation))
    slope_mean = float(np.nanmean(slope))
    slope_max = float(np.nanmax(slope))
    twi_mean = float(np.nanmean(twi))

    slope_zones = _slope_zones(slope, valid, total)
    water = _water_accumulation(twi, valid, total, twi_threshold)
    contours = _contour_zones(elevation, valid, total, elev_min, elev_max, contour_interval_m)

    buildable_pct = round(slope_zones["flat_pct"] + slope_zones["gentle_pct"], 2)

    return {
        "elevation_mean_m": round(elev_mean, 2),
        "elevation_min_m": round(elev_min, 2),
        "elevation_max_m": round(elev_max, 2),
        "elevation_std_m": round(elev_std, 2),
        "slope_mean_deg": round(slope_mean, 2),
        "slope_max_deg": round(slope_max, 2),
        "twi_mean": round(twi_mean, 2),
        "slope_zones": slope_zones,
        "water_accumulation": water,
        "contour_zones": contours,
        "site_summary": {
            "terrain_class": _terrain_class(slope_mean),
            "drainage_risk": _drainage_risk(water["high_risk_pct"]),
            "buildable_area_pct": buildable_pct,
        },
        "raster_resolution_m": RASTER_RESOLUTION_M,
        "crs": RASTER_CRS,
    }
