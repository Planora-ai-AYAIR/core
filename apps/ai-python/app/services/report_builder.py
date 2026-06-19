"""
Contract response builder for GeoSense AI.

Maps the raw service outputs (``soilgrids_service.get_soil_composition`` and the
terrain dict from ``terrain_service`` / ``gee_service``) into the three
client-facing sections defined by the GeoSense API Contract:

    • soilComposition  — Module 2 (AI Soil Composition Estimate)
    • terrain          — Module 1 (AI Topographic Profile)
    • hydrology         — water-accumulation / TWI / drainage (ponding risk)

These functions are pure dict-shaping helpers (no I/O), so they are cheap to
unit-test and safe to call regardless of which terrain backend produced the
input (local raster or live Google Earth Engine).
"""

from typing import Optional

# SoilGrids standard depth intervals, surface → deep.
SOIL_DEPTHS = ["0-5cm", "5-15cm", "15-30cm", "30-60cm", "60-100cm", "100-200cm"]

# Slope bands + the contract's traffic-light colours. Our terrain service
# classifies slope in DEGREES (not percent), so the ranges are labelled in
# degrees to stay honest about what is actually computed.
SLOPE_BANDS = [
    ("flat",     "<2°",    "flat_pct",     "#4CAF50"),
    ("gentle",   "2–5°",   "gentle_pct",   "#8BC34A"),
    ("moderate", "5–15°",  "moderate_pct", "#FFC107"),
    ("steep",    ">15°",   "steep_pct",    "#F44336"),
]


def _r(value, ndigits: int = 2):
    """Round numerics for presentation; pass through None/non-numbers."""
    if isinstance(value, bool) or value is None:
        return value
    if isinstance(value, (int, float)):
        return round(value, ndigits)
    return value


def _soil_type(clay, sand, silt) -> str:
    """Dominant texture class from fractions (mirrors soilgrids_service)."""
    sand = sand or 0.0
    clay = clay or 0.0
    silt = silt or 0.0
    if sand > 70:
        return "Sandy"
    if clay > 35:
        return "Clayey"
    if silt > 50:
        return "Silty"
    return "Loamy"


def build_soil_composition(soil: Optional[dict]) -> Optional[dict]:
    """Shape the SoilGrids result into the contract's soil-composition block."""
    if not soil:
        return None

    profiles = soil.get("profiles", {})
    clay_p = profiles.get("clay", {})
    sand_p = profiles.get("sand", {})
    silt_p = profiles.get("silt", {})
    bdod_p = profiles.get("bdod", {})
    soc_p = profiles.get("soc", {})
    ph_p = profiles.get("phh2o", {})

    multi_depth = []
    for depth in SOIL_DEPTHS:
        clay = clay_p.get(depth)
        sand = sand_p.get(depth)
        silt = silt_p.get(depth)
        multi_depth.append({
            "depth": depth,
            "sand": _r(sand),
            "silt": _r(silt),
            "clay": _r(clay),
            "unit": "%",
            "bulkDensity": _r(bdod_p.get(depth)),
            "organicCarbon": _r(soc_p.get(depth)),
            "ph": _r(ph_p.get(depth)),
            "type": _soil_type(clay, sand, silt),
        })

    return {
        "surfaceDepth": "0-5cm",
        "composition": {
            "sand": _r(soil.get("sand_0_5")),
            "silt": _r(soil.get("silt_0_5")),
            "clay": _r(soil.get("clay_0_5")),
            "unit": "%",
        },
        "properties": {
            "bulkDensity": _r(soil.get("bdod_0_5")),
            "bulkDensityUnit": "g/cm³",
            "organicCarbon": _r(soil.get("soc_0_5")),
            "organicCarbonUnit": "%",
            "ph": _r(soil.get("ph_0_5")),
        },
        "classification": {
            "primaryType": soil.get("dominant_soil_type"),
        },
        "multiDepthProfile": multi_depth,
        "source": soil.get("source", "ISRIC SoilGrids v2.0"),
    }


def build_terrain(terrain: Optional[dict]) -> Optional[dict]:
    """Shape the terrain result into the contract's topographic-profile block."""
    if not terrain:
        return None

    zones = terrain.get("slope_zones", {})
    distribution = [
        {
            "category": name,
            "range": rng,
            "percentage": _r(zones.get(key, 0.0)),
            "color": color,
        }
        for name, rng, key, color in SLOPE_BANDS
    ]

    summary = terrain.get("site_summary", {})
    return {
        "elevation": {
            "min": _r(terrain.get("elevation_min_m")),
            "max": _r(terrain.get("elevation_max_m")),
            "mean": _r(terrain.get("elevation_mean_m")),
            "stdDev": _r(terrain.get("elevation_std_m")),
            "unit": "m",
        },
        "slope": {
            "meanDeg": _r(terrain.get("slope_mean_deg")),
            "maxDeg": _r(terrain.get("slope_max_deg")),
            "unit": "degrees",
        },
        "slopeAnalysis": {"distribution": distribution},
        "contourZones": terrain.get("contour_zones", []),
        "summary": {
            "terrainClass": summary.get("terrain_class"),
            "buildableAreaPct": _r(summary.get("buildable_area_pct")),
            "drainageRisk": summary.get("drainage_risk"),
        },
        "rasterResolutionM": terrain.get("raster_resolution_m"),
        "crs": terrain.get("crs"),
        "source": terrain.get("source", "Copernicus GLO-30 DEM"),
    }


def build_hydrology(terrain: Optional[dict]) -> Optional[dict]:
    """Derive the hydrology block (TWI / water accumulation / ponding) from terrain."""
    if not terrain:
        return None

    water = terrain.get("water_accumulation", {})
    high_pct = water.get("high_risk_pct", 0.0) or 0.0
    ponding_level = "High" if high_pct > 20 else "Medium" if high_pct > 5 else "Low"

    return {
        "twiMean": _r(terrain.get("twi_mean")),
        "twiThreshold": water.get("twi_threshold_used"),
        "waterAccumulation": {
            "highRiskPct": _r(water.get("high_risk_pct")),
            "mediumRiskPct": _r(water.get("medium_risk_pct")),
            "lowRiskPct": _r(water.get("low_risk_pct")),
            "unit": "%",
        },
        "drainageRisk": terrain.get("site_summary", {}).get("drainage_risk"),
        "pondingRisk": {
            "level": ponding_level,
            "highAccumulationAreaPct": _r(high_pct),
        },
        "floodRiskFlag": high_pct > 20,
        "source": terrain.get("source", "DEM-derived TWI (MERIT Hydro + slope)"),
    }


def build_analysis_data(
    soil: Optional[dict],
    terrain: Optional[dict],
    bbox: list,
    notes: list,
) -> dict:
    """Assemble the full ``data`` payload for the unified response envelope."""
    return {
        "location": {
            "centroid": soil.get("centroid") if soil else None,
            "boundingBox": {
                "minX": bbox[0],
                "minY": bbox[1],
                "maxX": bbox[2],
                "maxY": bbox[3],
            },
        },
        "soilComposition": build_soil_composition(soil),
        "terrain": build_terrain(terrain),
        "hydrology": build_hydrology(terrain),
        "notes": notes,
    }
