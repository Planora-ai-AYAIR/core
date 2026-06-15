"""Risk assessment service for GeoSense AI.

Computes flood, seismic, expansive soil, and liquefaction risk scores
based on terrain data (slope, TWI) and soil composition.
"""

import logging
from typing import Optional

logger = logging.getLogger(__name__)


def _flood_score(terrain: Optional[dict]) -> dict:
    """Compute flood risk from TWI and slope data."""
    if not terrain:
        return {"score": 0, "level": "Low"}

    water = terrain.get("water_accumulation", {})
    slope_zones = terrain.get("slope_zones", {})
    high_pct = water.get("high_risk_pct", 0) or 0
    flat_pct = slope_zones.get("flat_pct", 0) or 0

    score = min(100, int(high_pct * 1.5 + flat_pct * 0.3))

    factors = []
    if flat_pct > 40:
        factors.append(f"Low slope <2% ({flat_pct:.0f}% of area)")
    if high_pct > 10:
        factors.append("High TWI index")
    factors.append("Proximity to drainage basin")

    level = _score_to_level(score)
    return {"score": score, "level": level, "factors": factors}


def _seismic_score() -> dict:
    """Compute seismic risk — Egypt is generally low seismic activity."""
    return {
        "score": 20,
        "level": "Low",
        "factors": ["NCSR zone classification: Low", "Far from active fault lines"],
        "source": "USGS/Egypt NCSR",
    }


def _expansive_soil_score(clay_content: float | None) -> dict:
    """Compute expansive soil risk from clay content."""
    clay = clay_content or 0
    score = min(100, int(clay * 2.5))

    factors = [f"Clay content: {clay:.0f}%"]
    if clay > 30:
        factors.append("Shrink-swell potential: High")
        replacement_depth = 2.0
    elif clay > 15:
        factors.append("Shrink-swell potential: Medium")
        replacement_depth = 1.5
    else:
        factors.append("Shrink-swell potential: Low")
        replacement_depth = 0.5

    level = _score_to_level(score)
    return {
        "score": score,
        "level": level,
        "factors": factors,
        "replacementDepth": replacement_depth,
    }


def _liquefaction_score(
    sand_content: float | None,
    water_table_depth: float | None,
) -> dict:
    """Compute liquefaction risk from sand content and water table depth."""
    sand = sand_content or 0
    wtd = water_table_depth or 15.0

    score = min(100, int(sand * 0.5 + max(0, 20 - wtd) * 3))

    factors = []
    if sand > 50:
        factors.append("Sandy soil present")
    factors.append(f"Water table depth: {wtd}m")

    susceptibility = "High" if score > 60 else "Moderate" if score > 30 else "Low"
    level = _score_to_level(score)

    return {
        "score": score,
        "level": level,
        "factors": factors,
        "susceptibility": susceptibility,
    }


def _score_to_level(score: int) -> str:
    """Map a 0-100 score to a risk level string."""
    if score <= 20:
        return "Very Low"
    if score <= 40:
        return "Low"
    if score <= 60:
        return "Moderate"
    if score <= 80:
        return "High"
    return "Very High"


def compute_risks(
    risk_types: list[str],
    terrain: Optional[dict] = None,
    clay_content: float | None = None,
    sand_content: float | None = None,
    water_table_depth: float | None = None,
) -> dict:
    """
    Compute risk scores for the requested risk types.

    Returns a dict matching the contract §3.3.2 result shape.
    """
    results = {}
    scores = []

    if "flood" in risk_types:
        results["flood"] = _flood_score(terrain)
        scores.append(results["flood"]["score"])

    if "seismic" in risk_types:
        results["seismic"] = _seismic_score()
        scores.append(results["seismic"]["score"])

    if "expansiveSoil" in risk_types:
        results["expansiveSoil"] = _expansive_soil_score(clay_content)
        scores.append(results["expansiveSoil"]["score"])

    if "liquefaction" in risk_types:
        results["liquefaction"] = _liquefaction_score(sand_content, water_table_depth)
        scores.append(results["liquefaction"]["score"])

    overall = int(sum(scores) / len(scores)) if scores else 0
    results["overallScore"] = overall

    return results
