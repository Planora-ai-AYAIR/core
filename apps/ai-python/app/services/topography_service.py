"""
Topography analysis functions:
elevation stats, slope classification,
cut/fill volumes, contour lines, ponding zones.
"""

import json
import logging
import os

import numpy as np
import rasterio
from rasterio.features import shapes
import geopandas as gpd

logger = logging.getLogger(__name__)


# ── Elevation Stats ───────────────────────────────────────────
def compute_elevation_stats(dem_path: str) -> dict:
    with rasterio.open(dem_path) as src:
        data   = src.read(1).astype(np.float32)
        nodata = src.nodata

    valid = data[data != nodata] if nodata is not None else data.flatten()
    valid = valid[np.isfinite(valid)]

    return {
        "min_m":  round(float(valid.min()),  2),
        "max_m":  round(float(valid.max()),  2),
        "mean_m": round(float(valid.mean()), 2),
        "std_m":  round(float(valid.std()),  2),
    }


# ── Slope Classification ──────────────────────────────────────
SLOPE_CATEGORIES = [
    {"label": "< 2%",  "min": 0,  "max": 2,     "color": "#22c55e", "category": "flat"},
    {"label": "2-5%",  "min": 2,  "max": 5,     "color": "#84cc16", "category": "gentle"},
    {"label": "5-15%", "min": 5,  "max": 15,    "color": "#eab308", "category": "moderate"},
    {"label": "> 15%", "min": 15, "max": 99999, "color": "#ef4444", "category": "steep"},
]

def classify_slope(slope_path: str) -> tuple:
    """Returns (classified_array, distribution_list)"""
    with rasterio.open(slope_path) as src:
        slope  = src.read(1).astype(np.float32)
        nodata = src.nodata

    valid_mask = np.isfinite(slope)
    if nodata is not None:
        valid_mask &= (slope != nodata)

    total     = valid_mask.sum()
    classified = np.zeros_like(slope, dtype=np.uint8)
    dist       = []

    for i, cat in enumerate(SLOPE_CATEGORIES, start=1):
        mask = valid_mask & (slope >= cat["min"]) & (slope < cat["max"])
        classified[mask] = i
        pct = round(float(mask.sum() / total * 100), 1) if total > 0 else 0.0
        dist.append({
            "category": cat["category"],
            "label":    cat["label"],
            "pct_area": pct,
            "color":    cat["color"],
        })

    return classified, dist


# ── Cut & Fill ────────────────────────────────────────────────
def compute_cut_fill(dem_path: str,
                     reference_elevation: float = None) -> dict:
    with rasterio.open(dem_path) as src:
        dem    = src.read(1).astype(np.float64)
        nodata = src.nodata
        px_w   = abs(src.transform.a)
        px_h   = abs(src.transform.e)
        px_area = px_w * px_h

    valid = np.isfinite(dem)
    if nodata is not None:
        valid &= (dem != nodata)

    ref = reference_elevation if reference_elevation else float(dem[valid].mean())

    diff = np.zeros_like(dem)
    diff[valid] = dem[valid] - ref

    cut_m3  = round(float(diff[diff > 0].sum() * px_area), 1)
    fill_m3 = round(float(abs(diff[diff < 0].sum()) * px_area), 1)

    return {
        "cut_m3":           cut_m3,
        "fill_m3":          fill_m3,
        "net_volume_m3":    round(cut_m3 - fill_m3, 1),
        "reference_elev_m": round(ref, 2),
        "pixel_area_m2":    round(px_area, 1),
        "method": "mean_elevation" if not reference_elevation else "user_defined",
    }


# ── Contour Lines ─────────────────────────────────────────────
def generate_contours(dem_path: str,
                      out_dir:  str,
                      interval_m: float = 0.5) -> str:
    """Generate contour lines using GDAL and export as GeoJSON."""
    try:
        from osgeo import gdal, ogr
    except ImportError:
        logger.warning("⚠️  GDAL not available — skipping contours")
        return ""

    shp_path  = os.path.join(out_dir, "contours.shp")
    json_path = os.path.join(out_dir, "contours.geojson")

    ds   = gdal.Open(dem_path)
    band = ds.GetRasterBand(1)
    drv  = ogr.GetDriverByName("ESRI Shapefile")

    dst_ds    = drv.CreateDataSource(shp_path)
    dst_layer = dst_ds.CreateLayer("contours")
    dst_layer.CreateField(ogr.FieldDefn("elevation", ogr.OFTReal))

    gdal.ContourGenerate(
        band, interval_m, 0, [], 1,
        band.GetNoDataValue(), dst_layer, -1, 0
    )
    dst_ds = None  # close + flush

    # Simplify + export GeoJSON
    gdf = gpd.read_file(shp_path)
    gdf["geometry"] = gdf["geometry"].simplify(
        tolerance=0.00005, preserve_topology=True
    )
    gdf.to_file(json_path, driver="GeoJSON")

    logger.info(f"✅ Contours saved: {json_path}")
    return json_path


# ── Ponding Zones ─────────────────────────────────────────────
def detect_ponding_zones(twi_path:        str,
                          slope_path:      str,
                          out_dir:         str,
                          twi_threshold:   float = 8.0,
                          slope_threshold: float = 2.0) -> dict:
    with rasterio.open(twi_path) as src:
        twi      = src.read(1).astype(np.float32)
        profile  = src.profile
        nodata_t = src.nodata

    with rasterio.open(slope_path) as src:
        slope    = src.read(1).astype(np.float32)
        nodata_s = src.nodata

    valid = np.isfinite(twi) & np.isfinite(slope)
    if nodata_t is not None: valid &= (twi   != nodata_t)
    if nodata_s is not None: valid &= (slope != nodata_s)

    risk_mask = valid & (twi > twi_threshold) & (slope < slope_threshold)

    polygons = []
    for geom, val in shapes(risk_mask.astype(np.uint8),
                            transform=profile["transform"]):
        if val == 1:
            polygons.append({
                "type":       "Feature",
                "geometry":   geom,
                "properties": {"risk": "ponding"},
            })

    px_area    = abs(profile["transform"].a) * abs(profile["transform"].e)
    total_area = round(float(risk_mask.sum() * px_area), 1)

    json_path = os.path.join(out_dir, "ponding_zones.geojson")
    with open(json_path, "w") as f:
        json.dump({"type": "FeatureCollection", "features": polygons}, f)

    return {
        "zones_count":   len(polygons),
        "total_area_m2": total_area,
        "geo_json_path": json_path,
        "twi_threshold": twi_threshold,
    }