"""Contract-shaped mock results for the client-facing API (§2).

Each builder returns the exact result payload shape from the GeoSense AI API
Contract §2.2–2.6, parameterised by ``parcelId``. These stand in for the
.NET backend's stored module results so the §2 surface is fully browsable
without live Google Earth Engine / SoilGrids calls.

The real AI-engine services (``topography_service``, ``soilgrids_service``,
``risk_service``, ``borehole_service``) can replace these later — the JSON
shape returned here is the contract the frontend depends on.
"""

from __future__ import annotations

from app.schemas.common import utc_now_iso


def _s3(parcel_id: str, *parts: str) -> str:
    tail = "/".join(parts)
    return f"https://s3.amazonaws.com/geosense/{parcel_id}/{tail}?signature=..."


# ── §2.2 Topography ──────────────────────────────────────────
def topography_result(parcel_id: str, include_tiles: bool = True, fmt: str = "json") -> dict:
    result = {
        "parcelId": parcel_id,
        "elevation": {"min": 12.5, "max": 45.2, "mean": 28.7, "unit": "m"},
        "slopeAnalysis": {
            "distribution": [
                {"category": "flat", "range": "<2%", "percentage": 45, "color": "#4CAF50"},
                {"category": "gentle", "range": "2-5%", "percentage": 30, "color": "#8BC34A"},
                {"category": "moderate", "range": "5-15%", "percentage": 20, "color": "#FFC107"},
                {"category": "steep", "range": ">15%", "percentage": 5, "color": "#F44336"},
            ]
        },
        "cutFill": {
            "cutVolume": 4500,
            "fillVolume": 3200,
            "netVolume": -1300,
            "unit": "m³",
        },
        "contourLines": {
            "geoJsonUrl": _s3(parcel_id, "contours.geojson"),
            "interval": 0.5,
        },
        "pondingRisk": {
            "zonesCount": 3,
            "totalArea": 12500,
            "unit": "m²",
            "geoJsonUrl": _s3(parcel_id, "ponding.geojson"),
        },
        "generatedAt": utc_now_iso(),
    }
    if include_tiles:
        result["rasterTiles"] = {
            "elevation": _s3(parcel_id, "elevation", "{z}", "{x}", "{y}.png"),
            "slope": _s3(parcel_id, "slope", "{z}", "{x}", "{y}.png"),
        }
    return result


# ── §2.3 Soil ────────────────────────────────────────────────
_SOIL_DEPTHS = {
    "0-20cm": {"sand": 60, "silt": 25, "clay": 15, "type": "Sandy Loam"},
    "20-50cm": {"sand": 50, "silt": 30, "clay": 20, "type": "Loam"},
    "50-100cm": {"sand": 40, "silt": 35, "clay": 25, "type": "Clay Loam"},
    "100-200cm": {"sand": 30, "silt": 35, "clay": 35, "type": "Clay"},
}


def soil_result(parcel_id: str, depth: str = "0-20cm") -> dict:
    layer = _SOIL_DEPTHS.get(depth, _SOIL_DEPTHS["0-20cm"])
    return {
        "parcelId": parcel_id,
        "depth": depth,
        "composition": {
            "sand": layer["sand"],
            "silt": layer["silt"],
            "clay": layer["clay"],
            "unit": "%",
        },
        "properties": {
            "bulkDensity": 1.4,
            "bulkDensityUnit": "g/cm³",
            "organicCarbon": 1.2,
            "organicCarbonUnit": "%",
            "ph": 8.1,
        },
        "classification": {
            "primaryType": layer["type"],
            "usdaClass": "Loamy",
            "aiConfidence": 0.85,
        },
        "multiDepthProfile": [
            {"depth": d, "sand": v["sand"], "clay": v["clay"], "type": v["type"]}
            for d, v in _SOIL_DEPTHS.items()
        ],
        "heatmapTileUrl": _s3(parcel_id, "soil_heatmap", "{z}", "{x}", "{y}.png"),
        "generatedAt": utc_now_iso(),
    }


# ── §2.4 Bearing Capacity ────────────────────────────────────
def bearing_result(parcel_id: str, foundation_type: str = "shallow") -> dict:
    return {
        "parcelId": parcel_id,
        "bearingCapacity": {
            "value": 150,
            "unit": "kPa",
            "category": "Medium",
            "range": "75-200 kPa",
            "trafficLight": "yellow",
        },
        "foundationRecommendation": {
            "type": "Shallow Foundations",
            "suitable": foundation_type in ("shallow", "mat"),
            "maxFloorsWithoutDeepFoundation": 5,
            "floorCountCategory": "3-5 floors",
        },
        "soilFactors": {
            "clayContent": 15,
            "sandContent": 60,
            "moistureIndex": 0.32,
            "depthToWaterTable": 8.5,
        },
        "disclaimer": (
            "This is an AI estimate for preliminary planning only. Physical "
            "borehole verification is mandatory for structural certification."
        ),
        "generatedAt": utc_now_iso(),
    }


# ── §2.5 Construction Risk ───────────────────────────────────
def risk_result(parcel_id: str) -> dict:
    return {
        "parcelId": parcel_id,
        "overallScore": 42,
        "overallRiskLevel": "Moderate",
        "maxScore": 100,
        "riskBreakdown": {
            "flood": {
                "score": 65,
                "level": "High",
                "factors": ["Low slope <2%", "High TWI index", "Proximity to drainage basin"],
                "geoJsonUrl": _s3(parcel_id, "flood_zones.geojson"),
            },
            "seismic": {
                "score": 20,
                "level": "Low",
                "factors": ["NCSR zone classification: Low", "Far from active fault lines"],
                "source": "USGS/Egypt NCSR",
            },
            "expansiveSoil": {
                "score": 45,
                "level": "Moderate",
                "factors": ["Clay content: 15%", "Shrink-swell potential: Medium"],
                "replacementDepth": 1.5,
            },
            "liquefaction": {
                "score": 30,
                "level": "Low-Moderate",
                "factors": ["Sandy soil present", "Water table depth: 8.5m"],
                "susceptibility": "Low",
            },
        },
        "generatedAt": utc_now_iso(),
    }


# ── §2.6 Optimized Borehole Campaign ─────────────────────────
def borehole_result(parcel_id: str) -> dict:
    return {
        "parcelId": parcel_id,
        "recommendation": {
            "minimumRequired": 12,
            "optimalCount": 18,
            "coveragePercentage": 85,
            "gridSize": "30m spacing",
        },
        "placement": {
            "strategy": "Adaptive grid with hotspots",
            "points": [
                {
                    "id": "BH-001",
                    "latitude": 31.05,
                    "longitude": 30.02,
                    "priority": "High",
                    "reason": "Soil variability hotspot",
                    "estimatedDepth": 20,
                }
            ],
            "geoJsonUrl": _s3(parcel_id, "boreholes.geojson"),
        },
        "costAnalysis": {
            "traditionalApproach": {
                "boreholes": 30,
                "estimatedCost": 420000,
                "currency": "EGP",
            },
            "optimizedApproach": {
                "boreholes": 12,
                "estimatedCost": 180000,
                "currency": "EGP",
            },
            "savings": {
                "amount": 240000,
                "currency": "EGP",
                "percentage": 57,
            },
        },
        "generatedAt": utc_now_iso(),
    }


# ── Dispatch by module ───────────────────────────────────────
BUILDERS = {
    "topography": topography_result,
    "soil": soil_result,
    "bearing": bearing_result,
    "risk": risk_result,
    "borehole": borehole_result,
}


def build_result(module: str, parcel_id: str) -> dict:
    """Build a default contract-shaped result for a module (used by pipelines)."""
    builder = BUILDERS.get(module)
    return builder(parcel_id) if builder else {"parcelId": parcel_id}
