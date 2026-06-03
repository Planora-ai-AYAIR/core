"""
PNG tile generation for MapLibre GL JS rendering.
"""

import logging
import os

import numpy as np
import rasterio
import matplotlib.pyplot as plt
import matplotlib.colors as mcolors
from PIL import Image

logger = logging.getLogger(__name__)


def export_png_tile(raster_path: str,
                    out_path:    str,
                    colormap:    str) -> str:
    """
    Convert single-band raster to color PNG.
    Uses 2nd–98th percentile stretch to avoid outlier washing.
    """
    with rasterio.open(raster_path) as src:
        data   = src.read(1).astype(np.float32)
        nodata = src.nodata

    if nodata is not None:
        data[data == nodata] = np.nan

    vmin, vmax = np.nanpercentile(data, [2, 98])
    norm  = mcolors.Normalize(vmin=vmin, vmax=vmax)
    cmap  = plt.get_cmap(colormap)

    rgba = cmap(norm(np.nan_to_num(data, nan=0.0)))
    rgb  = (rgba[:, :, :3] * 255).astype(np.uint8)

    os.makedirs(os.path.dirname(out_path), exist_ok=True)
    Image.fromarray(rgb).save(out_path)

    logger.debug(f"Tile saved: {out_path}")
    return out_path


def export_all_tiles(dem_path:   str,
                     slope_path: str,
                     out_dir:    str,
                     parcel_id:  str) -> dict:
    """
    Export elevation + slope PNG tiles.
    Returns dict of {layer_name: file_path}.
    """
    configs = [
        {"path": dem_path,   "cmap": "viridis",  "name": "elevation"},
        {"path": slope_path, "cmap": "RdYlGn_r", "name": "slope"},
    ]

    urls = {}
    for cfg in configs:
        out = os.path.join(out_dir, "tiles", parcel_id,
                           cfg["name"], "overview.png")
        export_png_tile(cfg["path"], out, cfg["cmap"])
        urls[cfg["name"]] = out

    logger.info(f"✅ Tiles exported: {list(urls.keys())}")
    return urls