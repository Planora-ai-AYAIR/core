"""
ISRIC SoilGrids service for GeoSense AI.

Queries the SoilGrids REST API v2.0 at a polygon's centroid to retrieve
soil composition (clay/sand/silt/bulk density) across six standard depth
intervals, applies the required unit conversions, and returns a dict that
is ready to feed into Model B (bearing capacity estimator).
"""

import logging
import statistics

import requests
from shapely.geometry import shape

logger = logging.getLogger(__name__)

# SoilGrids REST API v2.0
SOILGRIDS_URL = "https://rest.isric.org/soilgrids/v2.0/properties/query"
REQUEST_TIMEOUT_S = 30

# Egypt bounds [minLon, minLat, maxLon, maxLat] (WGS84)
EGYPT_BBOX = [24.0, 22.0, 37.0, 32.0]

# Soil properties and depth layers requested from the API
#   clay/sand/silt → texture %   bdod → bulk density   soc → organic carbon   phh2o → pH
PROPERTIES = ["clay", "sand", "silt", "bdod", "soc", "phh2o"]
DEPTHS = ["0-5cm", "5-15cm", "15-30cm", "30-60cm", "60-100cm", "100-200cm"]


def _extract_centroid(geo_json: dict) -> tuple[float, float]:
    """Return (lon, lat) of the GeoJSON polygon centroid."""
    polygon = shape(geo_json["geometry"] if "geometry" in geo_json else geo_json)
    centroid = polygon.centroid
    return centroid.x, centroid.y


def _convert_layer(prop: str, raw_by_depth: dict[str, float | None]) -> dict[str, float | None]:
    """Apply SoilGrids unit conversions to one property's depth profile."""
    values = [v for v in raw_by_depth.values() if v is not None]
    median = statistics.median(values) if values else 0.0

    converted: dict[str, float | None] = {}
    for depth, raw in raw_by_depth.items():
        if raw is None:
            converted[depth] = None
        elif prop == "bdod":
            # cg/cm³ → g/cm³
            converted[depth] = raw / 100 if median > 10 else float(raw)
        elif prop == "phh2o":
            # SoilGrids reports pH × 10 (e.g. 81) → divide by 10 → 8.1
            converted[depth] = float(raw) / 10
        elif prop == "soc":
            # SoilGrids reports organic carbon in dg/kg → percent = raw / 100
            converted[depth] = float(raw) / 100
        else:
            # clay / sand / silt: g/kg → %
            converted[depth] = raw / 10 if median > 100 else float(raw)
    return converted


def _parse_response(payload: dict) -> dict[str, dict[str, float | None]]:
    """Parse SoilGrids JSON into {property: {depth: converted_value}}."""
    layers = payload.get("properties", {}).get("layers", [])
    raw: dict[str, dict[str, float | None]] = {p: {d: None for d in DEPTHS} for p in PROPERTIES}

    for layer in layers:
        prop = layer.get("name")
        if prop not in raw:
            continue
        for depth in layer.get("depths", []):
            label = depth.get("label")
            if label not in raw[prop]:
                continue
            raw[prop][label] = depth.get("values", {}).get("mean")

    return {prop: _convert_layer(prop, raw[prop]) for prop in PROPERTIES}


def _classify_soil(clay: float | None, sand: float | None, silt: float | None) -> str:
    """Classify dominant soil type from surface texture fractions."""
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


def get_soil_composition(geo_json: dict) -> dict:
    """
    Query SoilGrids REST API at polygon centroid and return composition dict.
    """
    lon, lat = _extract_centroid(geo_json)

    min_lon, min_lat, max_lon, max_lat = EGYPT_BBOX
    if not (min_lon <= lon <= max_lon and min_lat <= lat <= max_lat):
        raise ValueError("Centroid outside supported region")

    params = [
        ("lon", lon),
        ("lat", lat),
        *[("property", p) for p in PROPERTIES],
        *[("depth", d) for d in DEPTHS],
        ("value", "mean"),
    ]

    logger.info("🌱 Querying SoilGrids at centroid lon=%.5f lat=%.5f", lon, lat)
    try:
        resp = requests.get(SOILGRIDS_URL, params=params, timeout=REQUEST_TIMEOUT_S)
    except requests.exceptions.Timeout as e:
        logger.error("❌ SoilGrids request timed out after %ss", REQUEST_TIMEOUT_S)
        raise RuntimeError("SoilGrids API error: timeout") from e

    if resp.status_code != 200:
        logger.error("❌ SoilGrids returned status %s", resp.status_code)
        raise RuntimeError(f"SoilGrids API error: {resp.status_code}")

    profiles = _parse_response(resp.json())

    clay = profiles["clay"]
    sand = profiles["sand"]
    silt = profiles["silt"]
    bdod = profiles["bdod"]
    soc = profiles["soc"]
    phh2o = profiles["phh2o"]

    dominant = _classify_soil(clay["0-5cm"], sand["0-5cm"], silt["0-5cm"])

    return {
        # Surface (0–5cm) — fed directly to Model B
        "clay_0_5": clay["0-5cm"],
        "sand_0_5": sand["0-5cm"],
        "silt_0_5": silt["0-5cm"],
        "bdod_0_5": bdod["0-5cm"],
        # Surface chemistry (organic carbon %, pH) — for the soil-composition report
        "soc_0_5": soc["0-5cm"],
        "ph_0_5": phh2o["0-5cm"],
        # Subsurface (30–60cm) — fed directly to Model B
        "clay_30_60": clay["30-60cm"],
        "sand_30_60": sand["30-60cm"],
        "bdod_30_60": bdod["30-60cm"],
        # Full profiles for the report
        "profiles": {
            "clay": clay,
            "sand": sand,
            "silt": silt,
            "bdod": bdod,
            "soc": soc,
            "phh2o": phh2o,
        },
        "dominant_soil_type": dominant,
        "source": "ISRIC SoilGrids v2.0",
        "centroid": {"lon": lon, "lat": lat},
    }
