"""
Google Earth Engine service for GeoSense AI.

Handles:
- Service account authentication
- DEM export for parcel boundaries
- Bbox validation against Egypt bounds
"""

import ee
import os
import math
import logging

logger = logging.getLogger(__name__)


def init_gee(gee_project: str, 
             service_account_email: str,
             service_account_key: str) -> None:
    """
    Initialize Google Earth Engine with service account credentials.
    
    Authenticates to GEE on application startup. If service account key file
    exists, uses that (preferred for server/Docker). Otherwise falls back to
    user credentials (gcloud auth).
    
    Args:
        gee_project: GEE project ID (e.g., "my-geosense-project")
        service_account_email: Service account email from GEE console
        service_account_key: Path to service account JSON key file
    
    Raises:
        RuntimeError: If GEE authentication fails validation
    
    Example:
        >>> init_gee(
        ...     gee_project="geosense-prod",
        ...     service_account_email="geosense@my-project.iam.gserviceaccount.com",
        ...     service_account_key="./secrets/gee_key.json"
        ... )
    """
    try:
        # Path 1: Service account authentication (preferred for production)
        if service_account_key and os.path.exists(service_account_key):
            logger.info(f"🔐 Authenticating GEE with service account: {service_account_email}")
            
            creds = ee.ServiceAccountCredentials(
                email=service_account_email,
                key_file=service_account_key
            )
            ee.Initialize(creds, project=gee_project)
            logger.info("✅ GEE service account authentication successful")
        
        # Path 2: Fallback to user credentials (development only)
        else:
            logger.warning(
                f"⚠️  Service account key not found: {service_account_key}. "
                "Attempting fallback to gcloud auth (development mode)."
            )
            ee.Initialize(project=gee_project)
            logger.info("✅ GEE user authentication successful (dev mode)")
        
        # Validation: Test authentication with a tiny query
        logger.debug("🔍 Validating GEE access with test query...")
        _cairo = ee.Geometry.Point([31.2, 30.0])
        test_result = (
            ee.ImageCollection('COPERNICUS/DEM/GLO30')
            .filterBounds(_cairo)          # only tiles that cover Cairo
            .first()                       # one tile guaranteed to overlap
            .sample(_cairo, 30)
            .first()
            .getInfo()
        )

        if test_result is None:
            raise RuntimeError("GEE validation query returned None")
        
        logger.info("✅ GEE validation successful - ready for queries")
    
    except ee.EEException as e:
        logger.error(f"❌ GEE authentication failed: {str(e)}")
        raise RuntimeError(f"GEE authentication failed: {str(e)}") from e
    except Exception as e:
        logger.error(f"❌ Unexpected error during GEE initialization: {str(e)}")
        raise RuntimeError(f"Unexpected GEE initialization error: {str(e)}") from e


def validate_bbox_egypt(bbox: list[float]) -> bool:
    """
    Validate that a bounding box is completely within Egypt.
    
    Prevents accidental queries outside Egypt bounds, which would fail
    or return incorrect DEM data.
    
    Args:
        bbox: [minLon, minLat, maxLon, maxLat] in EPSG:4326 (WGS84)
    
    Returns:
        True if bbox is completely within Egypt, False otherwise
    
    Example:
        >>> validate_bbox_egypt([31.2, 30.0, 31.5, 30.3])
        True
        >>> validate_bbox_egypt([31.2, 20.0, 31.5, 30.3])  # minLat outside Egypt
        False
    """
    # Egypt bounds: [24°E to 37°E, 22°N to 32°N]
    egypt_bounds = [24.0, 22.0, 37.0, 32.0]  # [minLon, minLat, maxLon, maxLat]
    
    # Destructure bbox for clarity
    min_lon, min_lat, max_lon, max_lat = bbox
    
    # Check if bbox is completely within Egypt
    is_valid = (
        egypt_bounds[0] <= min_lon and max_lon <= egypt_bounds[2] and
        egypt_bounds[1] <= min_lat and max_lat <= egypt_bounds[3]
    )
    
    if not is_valid:
        logger.warning(
            f"❌ Bbox outside Egypt bounds: {bbox}. "
            f"Must be within {egypt_bounds}"
        )
    
    return is_valid


def _terrain_class(slope_mean: float) -> str:
    """Map mean slope (degrees) to a qualitative terrain class."""
    if slope_mean < 2:
        return "Flat"
    if slope_mean < 5:
        return "Gently Sloped"
    if slope_mean < 15:
        return "Moderately Sloped"
    return "Steep"


def _gee_contour_zones(dem, geom, min_elev: float, max_elev: float,
                       contour_interval_m: float = 0.5) -> list[dict]:
    """
    Split the elevation range into N equal bands and report each band's area
    share, computed live from the DEM via Google Earth Engine.
    """
    import string

    import numpy as np

    span = max_elev - min_elev
    if span <= 0 or contour_interval_m <= 0:
        n_zones = 4
    else:
        n_zones = max(4, min(6, int(span / contour_interval_m)))

    edges = np.linspace(min_elev, max_elev, n_zones + 1)

    masks = None
    meta: list[tuple[str, float, float, int]] = []
    for i in range(n_zones):
        lo, hi = float(edges[i]), float(edges[i + 1])
        if i == n_zones - 1:
            band = dem.gte(lo).And(dem.lte(hi))
        else:
            band = dem.gte(lo).And(dem.lt(hi))
        name = f"z{i}"
        band = band.rename(name)
        masks = band if masks is None else masks.addBands(band)
        meta.append((name, lo, hi, i))

    fracs = masks.reduceRegion(
        reducer=ee.Reducer.mean(), geometry=geom, scale=30,
        maxPixels=int(1e9), bestEffort=True,
    ).getInfo()

    zones: list[dict] = []
    for name, lo, hi, i in meta:
        v = fracs.get(name)
        zones.append({
            "label": f"Zone {string.ascii_uppercase[i]} ({lo:.1f}–{hi:.1f}m)",
            "min_elev_m": round(lo, 2),
            "max_elev_m": round(hi, 2),
            "area_pct": round(float(v) * 100, 2) if v is not None else 0.0,
        })
    return zones


def terrain_from_gee(geo_json: dict) -> dict:
    """
    Compute terrain statistics live from Google Earth Engine.

    Uses the Copernicus GLO-30 DEM directly (no local raster file needed).
    Returns elevation + slope statistics, slope-band distribution, terrain
    class and buildable-area share for the given polygon.

    Args:
        geo_json: a GeoJSON Polygon dict (coordinates in [lon, lat] order)

    Returns:
        A terrain dict matching the shape the UI expects.
    """
    geom = ee.Geometry(geo_json)

    collection = (
        ee.ImageCollection("COPERNICUS/DEM/GLO30")
        .filterBounds(geom)
        .select("DEM")
    )
    # mosaic() drops projection info, which makes ee.Terrain.slope degenerate.
    # Restore the native 30 m projection from the first tile before computing slope.
    native_proj = collection.first().projection()
    dem = collection.mosaic().setDefaultProjection(native_proj).clip(geom)
    slope = ee.Terrain.slope(dem)

    elev_stats = dem.reduceRegion(
        reducer=ee.Reducer.mean()
        .combine(ee.Reducer.minMax(), sharedInputs=True)
        .combine(ee.Reducer.stdDev(), sharedInputs=True),
        geometry=geom, scale=30, maxPixels=int(1e9), bestEffort=True,
    )
    slope_stats = slope.reduceRegion(
        reducer=ee.Reducer.mean().combine(ee.Reducer.max(), sharedInputs=True),
        geometry=geom, scale=30, maxPixels=int(1e9), bestEffort=True,
    )

    # Slope-band fractions: mean of a 0/1 mask over the region = pixel share.
    zone_masks = (
        slope.lt(2).rename("flat")
        .addBands(slope.gte(2).And(slope.lt(5)).rename("gentle"))
        .addBands(slope.gte(5).And(slope.lt(15)).rename("moderate"))
        .addBands(slope.gte(15).rename("steep"))
    )
    zone_fracs = zone_masks.reduceRegion(
        reducer=ee.Reducer.mean(), geometry=geom, scale=30,
        maxPixels=int(1e9), bestEffort=True,
    )

    # ── TWI (Topographic Wetness Index) ───────────────────────
    # TWI = ln(a / tan β), where a = specific catchment area (upslope
    # contributing area per unit contour width) and β = local slope.
    # Flow accumulation isn't a native GEE function, so we take the
    # pre-computed upstream drainage area ('upa', km²) from MERIT Hydro and
    # combine it with the GLO-30 slope.
    twi_threshold = 8.0
    merit_cell_m = 92.77  # MERIT Hydro pixel size (~3 arc-sec at the equator)

    upa_m2 = ee.Image("MERIT/Hydro/v1_0_1").select("upa").multiply(1e6)
    sca = upa_m2.divide(merit_cell_m)                       # specific catchment area
    tan_beta = slope.multiply(math.pi / 180.0).tan().max(0.001)  # avoid div-by-zero on flats
    twi = sca.divide(tan_beta).log().rename("twi")

    twi_bands = (
        twi
        .addBands(twi.gt(twi_threshold).rename("twi_high"))
        .addBands(twi.lt(5).rename("twi_low"))
    )
    twi_stats = twi_bands.reduceRegion(
        reducer=ee.Reducer.mean(), geometry=geom, scale=30,
        maxPixels=int(1e9), bestEffort=True,
    )

    combined = (
        ee.Dictionary({})
        .combine(elev_stats)
        .combine(slope_stats)
        .combine(zone_fracs)
        .combine(twi_stats)
    )

    logger.info("⛰️  Computing terrain from GEE (GLO-30 + MERIT Hydro TWI)")
    info = combined.getInfo()

    if info.get("DEM_mean") is None:
        raise RuntimeError("No DEM pixels found for this parcel (outside coverage?)")

    def pct(key):
        v = info.get(key)
        return round(float(v) * 100, 2) if v is not None else 0.0

    slope_mean = round(float(info["slope_mean"]), 2)
    flat_pct, gentle_pct = pct("flat"), pct("gentle")
    buildable = round(flat_pct + gentle_pct, 2)

    # ── Contour zones (second pass — needs the elevation range) ──
    elev_min = float(info["DEM_min"])
    elev_max = float(info["DEM_max"])
    contour_zones = _gee_contour_zones(dem, geom, elev_min, elev_max)

    # ── Water accumulation from TWI ───────────────────────────
    twi_mean = round(float(info["twi"]), 2) if info.get("twi") is not None else None
    high_pct = pct("twi_high")
    low_pct = pct("twi_low")
    medium_pct = round(max(0.0, 100.0 - high_pct - low_pct), 2)

    # Drainage risk derived from the share of high-wetness (high-TWI) area.
    drainage = "High" if high_pct > 20 else "Medium" if high_pct > 5 else "Low"

    return {
        "elevation_mean_m": round(float(info["DEM_mean"]), 2),
        "elevation_min_m": round(float(info["DEM_min"]), 2),
        "elevation_max_m": round(float(info["DEM_max"]), 2),
        "elevation_std_m": round(float(info.get("DEM_stdDev") or 0.0), 2),
        "slope_mean_deg": slope_mean,
        "slope_max_deg": round(float(info["slope_max"]), 2),
        "twi_mean": twi_mean,
        "slope_zones": {
            "flat_pct": flat_pct,
            "gentle_pct": gentle_pct,
            "moderate_pct": pct("moderate"),
            "steep_pct": pct("steep"),
        },
        "water_accumulation": {
            "high_risk_pct": high_pct,
            "medium_risk_pct": medium_pct,
            "low_risk_pct": low_pct,
            "twi_threshold_used": 8.0,
        },
        "contour_zones": contour_zones,
        "site_summary": {
            "terrain_class": _terrain_class(slope_mean),
            "drainage_risk": drainage,
            "buildable_area_pct": buildable,
        },
        "raster_resolution_m": 30,
        "crs": "EPSG:4326",
        "source": "Copernicus GLO-30 + MERIT Hydro (live via Google Earth Engine)",
    }


def export_dem_for_parcel(bbox: list[float],
                          job_id: str,
                          out_dir: str) -> str:
    """
    Export Copernicus DEM for a parcel to Google Drive.
    
    Fetches a 30m resolution DEM for the given parcel bounding box,
    adds a 500m buffer to capture surrounding terrain, and exports
    to Google Drive as a GeoTIFF file (EPSG:32636 - UTM Zone 36N for Egypt).
    
    The file is accessible to both backend and frontend teams via shared Drive folder.
    
    Args:
        bbox: [minLon, minLat, maxLon, maxLat] in EPSG:4326
        job_id: Unique job identifier (for file naming in Drive)
        out_dir: Not used for Drive export (kept for backward compatibility)
    
    Returns:
        Task ID for the GEE export job (can be used to monitor or cancel)
    
    Raises:
        RuntimeError: If GEE export task fails to start
    
    Example:
        >>> task_id = export_dem_for_parcel(
        ...     bbox=[31.2, 30.0, 31.5, 30.3],
        ...     job_id="job-001",
        ...     out_dir="/tmp/geosense/job-001"
        ... )
        >>> print(task_id)
        projects/ee-geosense/operations/xyz123...
    """
    try:
        logger.info(f"📥 Exporting DEM for parcel bbox: {bbox} to Google Drive")
        
        # Create geometry from bbox
        geometry = ee.Geometry.Rectangle(bbox)
        
        # Add 500m buffer (roughly 0.005° at equator)
        # This captures terrain features that affect the parcel
        buffer_degrees = 0.005
        geometry_buffered = geometry.buffer(buffer_degrees * 111320)  # Convert to meters
        
        logger.debug(f"📍 Using buffered geometry: {geometry_buffered.getInfo()}")
        
        # Fetch Copernicus DEM GLO-30 and clip to buffered geometry
        dem = (
            ee.ImageCollection("COPERNICUS/DEM/GLO30")
            .filterBounds(geometry_buffered)
            .select("DEM")
            .mosaic()
            .clip(geometry_buffered)
        )
        
        logger.debug("🎯 DEM collection filtered and clipped")
        
        # Export to Google Drive (shared with backend/frontend teams)
        # EPSG:32636 = UTM Zone 36N (suitable for Egypt - East Cairo/Nile Delta)
        task = ee.batch.Export.image.toDrive(
            image=dem,
            description=f"geosense_dem_{job_id}",
            fileNamePrefix=f"dem_{job_id}",
            scale=30,  # 30m resolution
            region=geometry_buffered,
            crs="EPSG:32636",
            maxPixels=1e9
        )
        
        task_id = task.id
        logger.info(f"⏳ Starting GEE Drive export task: {task_id}")
        task.start()
        
        logger.info(f"✅ GEE export started for job {job_id}")
        logger.info(f"📁 File will be available in Google Drive as: dem_{job_id}.tif")
        logger.info(f"🔗 Task ID: {task_id}")
        
        return task_id
    
    except ee.EEException as e:
        logger.error(f"❌ GEE export failed: {str(e)}")
        raise RuntimeError(f"GEE export failed: {str(e)}") from e
    except Exception as e:
        logger.error(f"❌ Unexpected error during DEM export: {str(e)}")
        raise RuntimeError(f"DEM export error: {str(e)}") from e
