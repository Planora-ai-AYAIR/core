"""
Simple synchronous analysis endpoint (UI + Postman / API clients).

Takes a list of polygon coordinates and returns the three client-facing
sections — Soil Composition, Terrain, and Hydrology — directly as JSON
(no background jobs / polling), wrapped in the unified response envelope
from the GeoSense API Contract. Each module is fault isolated: a failure in
one is reported in ``data.notes`` rather than aborting the others.
"""

import logging
import os

from fastapi import APIRouter, HTTPException
from pydantic import BaseModel

from app.schemas.topography import TopographyOptions
from app.services.soilgrids_service import get_soil_composition
from app.services.terrain_service import analyze_terrain
from app.services.gee_service import terrain_from_gee
from app.services.report_builder import build_analysis_data

logger = logging.getLogger(__name__)

router = APIRouter(prefix="/api/v1", tags=["analyze"])

RASTER_DIR = os.getenv("RASTER_DIR", "/data/rasters/")


class AnalyzeRequest(BaseModel):
    # Polygon vertices as [lat, lon] pairs (the order a user reads off a map).
    points: list[list[float]]


@router.post("/analyze")
async def analyze(req: AnalyzeRequest) -> dict:
    pts = [list(p) for p in req.points]

    if len(pts) < 3:
        raise HTTPException(
            status_code=400,
            detail={
                "statusCode": 400,
                "message": "Request validation failed",
                "errors": [{
                    "field": "points",
                    "code": "INVALID_POLYGON",
                    "message": "Need at least 3 coordinate points to form a polygon.",
                }],
                "data": None,
            },
        )

    # Close the ring if the user didn't repeat the first point.
    if pts[0] != pts[-1]:
        pts.append(pts[0])

    notes: list[str] = []

    # ── Soil composition (with auto lat/lon-order detection) ──
    # The user may type points as (lat, lon) OR (lon, lat). We try the order
    # as given first; if SoilGrids returns no data, we retry with the axes
    # swapped and keep whichever order actually yields soil data.
    def _build(points, swap: bool):
        # swap=False -> points are (lat, lon); swap=True -> points are (lon, lat)
        ll = [(p[1], p[0]) if swap else (p[0], p[1]) for p in points]  # -> (lat, lon)
        ring = [[lon, lat] for lat, lon in ll]
        geo = {"type": "Polygon", "coordinates": [ring]}
        lons = [lon for lat, lon in ll]
        lats = [lat for lat, lon in ll]
        return geo, [min(lons), min(lats), max(lons), max(lats)]

    geo_json, bbox = _build(pts, swap=False)

    soil = None
    swapped = False
    try:
        soil = get_soil_composition(geo_json)
        if not soil or soil.get("clay_0_5") is None:
            # Retry with swapped axes — maybe coords were entered as (lon, lat).
            try:
                alt_geo, alt_bbox = _build(pts, swap=True)
                alt_soil = get_soil_composition(alt_geo)
                if alt_soil and alt_soil.get("clay_0_5") is not None:
                    soil, geo_json, bbox, swapped = alt_soil, alt_geo, alt_bbox, True
                    notes.append(
                        "Coordinates were read as (lon, lat) and auto-corrected — "
                        "your points appear to be in longitude, latitude order."
                    )
            except Exception:
                pass  # keep original result/notes below

        if not swapped and (not soil or soil.get("clay_0_5") is None):
            notes.append(
                "SoilGrids returned no data at this location "
                "(likely water / coast / no-data zone). Try an inland parcel."
            )
    except Exception as e:  # fault isolation
        logger.error("Soil analysis failed: %s", e, exc_info=True)
        notes.append(f"Soil analysis failed: {e}")

    # ── Terrain analysis ──────────────────────────────────────
    # Prefer the precomputed local raster; if it's missing, fall back to a
    # live Copernicus GLO-30 computation via Google Earth Engine.
    terrain = None
    try:
        terrain = analyze_terrain(bbox, TopographyOptions(), RASTER_DIR)
    except FileNotFoundError:
        try:
            terrain = terrain_from_gee(geo_json)
            notes.append(
                "Terrain computed live from Google Earth Engine "
                "(local DEM raster not found)."
            )
        except Exception as e:
            logger.error("GEE terrain failed: %s", e, exc_info=True)
            notes.append(f"Terrain analysis failed (GEE fallback): {e}")
    except Exception as e:
        logger.error("Terrain analysis failed: %s", e, exc_info=True)
        notes.append(f"Terrain analysis failed: {e}")

    data = build_analysis_data(soil, terrain, bbox, notes)

    return {
        "statusCode": 200,
        "message": "Analysis completed successfully",
        "errors": None,
        "data": data,
    }
