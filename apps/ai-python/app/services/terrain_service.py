"""
Terrain derivative computation using WhiteboxTools.
"""

import logging
import os

logger = logging.getLogger(__name__)

# ── Lazy import WhiteboxTools ─────────────────────────────────
_wbt = None

def _get_wbt():
    global _wbt
    if _wbt is None:
        import whitebox
        _wbt = whitebox.WhiteboxTools()
        _wbt.verbose = False
    return _wbt


def compute_terrain_derivatives(dem_path: str, out_dir: str) -> dict:
    """
    Compute all terrain derivatives from DEM.

    Order matters:
      1. breach_depressions  — fix sinks before hydrology
      2. slope / aspect / curvature / TRI  — basic derivatives
      3. d8_flow_accumulation — needs filled DEM
      4. wetness_index (TWI)  — needs flow accumulation

    Args:
        dem_path: Path to dem_raw.tif (EPSG:32636, 30m)
        out_dir:  Output directory for derivative rasters

    Returns:
        dict of {name: file_path} for all derivatives
    """
    wbt = _get_wbt()

    paths = {
        "dem_filled": os.path.join(out_dir, "dem_filled.tif"),
        "slope":      os.path.join(out_dir, "slope.tif"),
        "aspect":     os.path.join(out_dir, "aspect.tif"),
        "curvature":  os.path.join(out_dir, "curvature.tif"),
        "flow_accum": os.path.join(out_dir, "flow_accum.tif"),
        "TWI":        os.path.join(out_dir, "TWI.tif"),
        "TRI":        os.path.join(out_dir, "TRI.tif"),
    }

    logger.info("Computing terrain derivatives...")

    # 1. Fill sinks — MUST be first
    logger.debug("  breach_depressions...")
    wbt.breach_depressions(dem_path, paths["dem_filled"])

    # 2. Basic derivatives
    logger.debug("  slope...")
    wbt.slope(paths["dem_filled"], paths["slope"], units="degrees")

    logger.debug("  aspect...")
    wbt.aspect(paths["dem_filled"], paths["aspect"])

    logger.debug("  plan_curvature...")
    wbt.plan_curvature(paths["dem_filled"], paths["curvature"])

    logger.debug("  ruggedness_index...")
    wbt.ruggedness_index(paths["dem_filled"], paths["TRI"])

    # 3. Hydrological
    logger.debug("  d8_flow_accumulation...")
    wbt.d8_flow_accumulation(paths["dem_filled"], paths["flow_accum"],
                              out_type="cells")

    logger.debug("  wetness_index (TWI)...")
    wbt.wetness_index(paths["dem_filled"], paths["flow_accum"], paths["TWI"])

    logger.info("✅ Terrain derivatives complete")
    return paths