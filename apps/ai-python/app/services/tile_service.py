"""
services/tile_service.py — GeoSense AI
Generates XYZ PNG map tiles from Egypt GeoTIFFs and uploads to S3.

Tile specs (from API contract):
  Format:     PNG 256×256
  Zoom range: 12–18
  Colormaps:
    elevation    → viridis
    slope        → RdYlGn_r  (green=flat, red=steep)
    soil_heatmap → custom    (Sand=#F4D03F, Silt=#A0522D, Clay=#C0392B)
    risk_heatmap → RdYlGn_r  (green=low risk, red=high)

GeoTIFF band layout:
  copernicus_dem_features_egypt_250m.tif:
    band 1 = elevation, band 2 = slope, band 3 = aspect,
    band 4 = curvature, band 5 = TWI, band 6 = TRI, band 7 = SRR
  risk_features_egypt_250m.tif:
    band 1 = seismic_risk, band 2 = flood_risk,
    band 3 = flood_frequency, band 4 = expansive_soil_risk
"""

from __future__ import annotations

import io
import logging
import math
import os
from pathlib import Path
from typing import Optional

import numpy as np

logger = logging.getLogger(__name__)

# ── Tile spec constants ────────────────────────────────────────────────────
TILE_SIZE  = 256
ZOOM_MIN   = 12
ZOOM_MAX   = 18

# ── Band indices (1-indexed for rasterio) ─────────────────────────────────
DEM_BAND_ELEVATION = 1
DEM_BAND_SLOPE     = 2
RISK_BAND_FLOOD    = 2   # highest-weight risk band for the composite heatmap

# ── Fixed display ranges ───────────────────────────────────────────────────
RANGE_ELEVATION = (0.0,   300.0)   # metres (covers all Egypt)
RANGE_SLOPE     = (0.0,    30.0)   # degrees
RANGE_RISK      = (0.0,     4.0)   # class value (0–4)


# ═══════════════════════════════════════════════════════════════════════════
# PUBLIC ENTRY POINT
# ═══════════════════════════════════════════════════════════════════════════

def generate_all_tiles(
    parcel_id: str,
    bbox: list[float],                 # [minLon, minLat, maxLon, maxLat]
    raster_dir: str,
    soil_data: dict,
    s3_service,
) -> dict[str, str]:
    """
    Generate all 4 tile layers for a parcel and upload to S3.

    Returns dict of tile URL templates:
        { "elevation_tiles": "s3://...", "slope_tiles": ..., ... }
    """
    dem_path  = Path(raster_dir) / "copernicus_dem_features_egypt_250m.tif"
    risk_path = Path(raster_dir) / "risk_features_egypt_250m.tif"

    urls: dict[str, str] = {}

    # ── Elevation ──────────────────────────────────────────────────────────
    if dem_path.exists():
        n = _generate_layer(
            parcel_id, bbox, s3_service,
            layer="elevation",
            geotiff_path=str(dem_path),
            band=DEM_BAND_ELEVATION,
            colormap="viridis",
            vmin=RANGE_ELEVATION[0],
            vmax=RANGE_ELEVATION[1],
        )
        logger.info("Elevation tiles: %d uploaded", n)
        urls["elevation_tiles"] = s3_service.tile_url_template(parcel_id, "elevation")
    else:
        logger.warning("DEM raster not found: %s — skipping elevation tiles", dem_path)
        urls["elevation_tiles"] = ""

    # ── Slope ──────────────────────────────────────────────────────────────
    if dem_path.exists():
        n = _generate_layer(
            parcel_id, bbox, s3_service,
            layer="slope",
            geotiff_path=str(dem_path),
            band=DEM_BAND_SLOPE,
            colormap="RdYlGn_r",
            vmin=RANGE_SLOPE[0],
            vmax=RANGE_SLOPE[1],
        )
        logger.info("Slope tiles: %d uploaded", n)
        urls["slope_tiles"] = s3_service.tile_url_template(parcel_id, "slope")
    else:
        urls["slope_tiles"] = ""

    # ── Risk heatmap ───────────────────────────────────────────────────────
    if risk_path.exists():
        n = _generate_layer(
            parcel_id, bbox, s3_service,
            layer="risk_heatmap",
            geotiff_path=str(risk_path),
            band=RISK_BAND_FLOOD,
            colormap="RdYlGn_r",
            vmin=RANGE_RISK[0],
            vmax=RANGE_RISK[1],
        )
        logger.info("Risk heatmap tiles: %d uploaded", n)
        urls["risk_tiles"] = s3_service.tile_url_template(parcel_id, "risk_heatmap")
    else:
        logger.warning("Risk raster not found: %s — skipping risk tiles", risk_path)
        urls["risk_tiles"] = ""

    # ── Soil heatmap (generated from soil composition, no raster needed) ──
    n = _generate_soil_tiles(parcel_id, bbox, s3_service, soil_data)
    logger.info("Soil heatmap tiles: %d uploaded", n)
    urls["soil_tiles"] = s3_service.tile_url_template(parcel_id, "soil_heatmap")

    return urls


# ═══════════════════════════════════════════════════════════════════════════
# LAYER GENERATOR (elevation / slope / risk)
# ═══════════════════════════════════════════════════════════════════════════

def _generate_layer(
    parcel_id: str,
    bbox: list[float],
    s3,
    layer: str,
    geotiff_path: str,
    band: int,
    colormap: str,
    vmin: float,
    vmax: float,
) -> int:
    """Generate + upload XYZ tiles for one GeoTIFF band. Returns tile count."""
    try:
        from rio_tiler.io import COGReader
        import morecantile
    except ImportError:
        logger.error("rio-tiler / morecantile not installed — cannot generate tiles")
        return 0

    tms      = morecantile.tms.get("WebMercatorQuad")
    west, south, east, north = bbox
    count    = 0

    try:
        with COGReader(geotiff_path) as cog:
            for zoom in range(ZOOM_MIN, ZOOM_MAX + 1):
                tiles = list(tms.tiles(west, south, east, north, zooms=[zoom]))
                for tile in tiles:
                    try:
                        img = cog.tile(
                            tile.x, tile.y, tile.z,
                            tilesize=TILE_SIZE,
                            indexes=[band],
                        )
                        png = _array_to_png(img.data[0], img.mask, colormap, vmin, vmax)
                        s3.upload_tile(png, parcel_id, layer, tile.z, tile.x, tile.y)
                        count += 1
                    except Exception as tile_exc:
                        logger.debug("Tile (%d/%d/%d) skipped: %s", zoom, tile.x, tile.y, tile_exc)
    except Exception as exc:
        logger.error("Tile generation failed for layer=%s: %s", layer, exc)

    return count


# ═══════════════════════════════════════════════════════════════════════════
# SOIL HEATMAP (no GeoTIFF — synthesized from composition percentages)
# ═══════════════════════════════════════════════════════════════════════════

# Contract colormap:  Sand=#F4D03F  Silt=#A0522D  Clay=#C0392B
_SAND_RGB  = np.array([244, 208,  63], dtype=np.float32)
_SILT_RGB  = np.array([160,  82,  45], dtype=np.float32)
_CLAY_RGB  = np.array([192,  57,  43], dtype=np.float32)


def _generate_soil_tiles(
    parcel_id: str,
    bbox: list[float],
    s3,
    soil: dict,
) -> int:
    """
    Generate solid-colour soil heatmap tiles blended from sand/silt/clay %.
    Each tile for the parcel is the same blended colour (uniform composition).
    """
    try:
        import morecantile
    except ImportError:
        return 0

    sand = soil.get("sand_0_5", 50.0) / 100.0
    silt = soil.get("silt_0_5", 30.0) / 100.0
    clay = soil.get("clay_0_5", 20.0) / 100.0
    total = sand + silt + clay + 1e-9

    # Weighted RGB blend
    rgb = (
        sand / total * _SAND_RGB +
        silt / total * _SILT_RGB +
        clay / total * _CLAY_RGB
    ).astype(np.uint8)

    # Solid-colour 256×256 RGBA tile
    tile_arr   = np.full((TILE_SIZE, TILE_SIZE, 3), rgb, dtype=np.uint8)
    alpha_arr  = np.full((TILE_SIZE, TILE_SIZE, 1), 200, dtype=np.uint8)   # slight transparency
    rgba_arr   = np.concatenate([tile_arr, alpha_arr], axis=2)

    from PIL import Image
    img = Image.fromarray(rgba_arr, "RGBA")
    buf = io.BytesIO()
    img.save(buf, format="PNG", optimize=True)
    png_bytes = buf.getvalue()

    tms   = morecantile.tms.get("WebMercatorQuad")
    west, south, east, north = bbox
    count = 0

    for zoom in range(ZOOM_MIN, ZOOM_MAX + 1):
        for tile in tms.tiles(west, south, east, north, zooms=[zoom]):
            try:
                s3.upload_tile(png_bytes, parcel_id, "soil_heatmap", tile.z, tile.x, tile.y)
                count += 1
            except Exception as exc:
                logger.debug("Soil tile (%d/%d/%d) failed: %s", zoom, tile.x, tile.y, exc)

    return count


# ═══════════════════════════════════════════════════════════════════════════
# COLORMAP APPLICATION
# ═══════════════════════════════════════════════════════════════════════════

def _array_to_png(
    data: np.ndarray,     # shape (H, W), float
    mask: np.ndarray,     # shape (H, W), 0=nodata 255=valid
    colormap: str,
    vmin: float,
    vmax: float,
) -> bytes:
    """Normalise data, apply matplotlib colormap, encode as 256×256 RGBA PNG."""
    import matplotlib.cm as mpl_cm
    from PIL import Image

    # Normalise to [0, 1]
    span = max(vmax - vmin, 1e-9)
    norm = np.clip((data.astype(np.float32) - vmin) / span, 0.0, 1.0)

    # Apply colormap (returns RGBA float 0–1)
    cmap_fn = mpl_cm.get_cmap(colormap)
    rgba_f  = cmap_fn(norm)                            # (H, W, 4)
    rgb_u8  = (rgba_f[:, :, :3] * 255).astype(np.uint8)

    # Alpha: transparent where no data
    alpha = np.where(mask > 0, 220, 0).astype(np.uint8)
    rgba_u8 = np.dstack([rgb_u8, alpha])

    img = Image.fromarray(rgba_u8, "RGBA")
    buf = io.BytesIO()
    img.save(buf, format="PNG", optimize=True)
    return buf.getvalue()


# ═══════════════════════════════════════════════════════════════════════════
# UTILITY — count tiles for a bbox + zoom range
# ═══════════════════════════════════════════════════════════════════════════

def count_tiles(bbox: list[float], zoom_min: int = ZOOM_MIN, zoom_max: int = ZOOM_MAX) -> int:
    """Return expected tile count for a bounding box across zoom levels."""
    try:
        import morecantile
        tms = morecantile.tms.get("WebMercatorQuad")
        west, south, east, north = bbox
        return sum(
            len(list(tms.tiles(west, south, east, north, zooms=[z])))
            for z in range(zoom_min, zoom_max + 1)
        )
    except ImportError:
        return 0