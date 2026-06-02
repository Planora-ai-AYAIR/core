"""
Google Earth Engine service for GeoSense AI.

Handles:
- Service account authentication
- DEM export for parcel boundaries
- Bbox validation against Egypt bounds
"""

import ee
import os
import logging
from pathlib import Path
from typing import Optional

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
        test_result = ee.Image('COPERNICUS/DEM/GLO30').sample(
            ee.Geometry.Point([0, 0]), 
            30
        ).first().getInfo()
        
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
