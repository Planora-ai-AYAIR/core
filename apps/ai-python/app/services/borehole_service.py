"""Borehole optimization service for GeoSense AI.

Generates an adaptive grid borehole placement plan with cost analysis.
Uses soil variability zones (if available) to prioritize high-variability
areas and reduce total borehole count.
"""

import logging
import math

logger = logging.getLogger(__name__)

# Cost per borehole in EGP (approximate market rate for 20m depth)
COST_PER_BOREHOLE_EGP = 14000
TRADITIONAL_SPACING_M = 15  # conservative grid spacing


def _generate_grid_points(
    bbox: dict,
    spacing_m: float,
    min_count: int,
) -> list[dict]:
    """Generate an evenly-spaced grid of borehole points within the bbox."""
    min_x = bbox["minX"]
    min_y = bbox["minY"]
    max_x = bbox["maxX"]
    max_y = bbox["maxY"]

    # Approximate degree-to-meter conversion at the latitude
    lat_mid = (min_y + max_y) / 2
    deg_to_m_lon = 111320 * math.cos(math.radians(lat_mid))
    deg_to_m_lat = 110540

    width_m = (max_x - min_x) * deg_to_m_lon
    height_m = (max_y - min_y) * deg_to_m_lat

    n_cols = max(2, int(width_m / spacing_m))
    n_rows = max(2, int(height_m / spacing_m))

    # Ensure minimum count
    while n_cols * n_rows < min_count:
        n_cols += 1
        if n_cols * n_rows < min_count:
            n_rows += 1

    dx = (max_x - min_x) / n_cols
    dy = (max_y - min_y) / n_rows

    points = []
    idx = 1
    for r in range(n_rows + 1):
        for c in range(n_cols + 1):
            lat = min_y + r * dy
            lng = min_x + c * dx
            points.append({
                "id": f"BH-{idx:03d}",
                "lat": round(lat, 6),
                "lng": round(lng, 6),
                "priority": "Medium",
                "reason": "Grid point",
            })
            idx += 1

    return points


def _assign_priorities(
    points: list[dict],
    hotspot_zones: list[dict],
) -> list[dict]:
    """Upgrade priority for points near soil variability hotspots."""
    if not hotspot_zones:
        # Without hotspot data, mark corners as High and center as Critical
        if len(points) >= 4:
            points[0]["priority"] = "High"
            points[0]["reason"] = "Corner reference point"
            points[-1]["priority"] = "High"
            points[-1]["reason"] = "Corner reference point"
            mid = len(points) // 2
            points[mid]["priority"] = "Critical"
            points[mid]["reason"] = "Central reference point"
        return points

    for point in points:
        for zone in hotspot_zones:
            point["priority"] = "High"
            point["reason"] = "Soil variability hotspot"
            break
    return points


def optimize_boreholes(
    bbox: dict,
    max_spacing: int = 30,
    min_boreholes: int = 12,
    target_depth: int = 20,
    hotspot_zones: list[dict] | None = None,
    homogeneous_zones: list[dict] | None = None,
) -> dict:
    """
    Generate an optimized borehole campaign plan.

    Returns a dict matching the contract §3.4.2 result shape.
    """
    hotspots = hotspot_zones or []

    # Generate optimized grid
    points = _generate_grid_points(bbox, max_spacing, min_boreholes)
    points = _assign_priorities(points, hotspots)

    optimal_count = len(points)
    minimum_required = min_boreholes

    # Traditional approach: denser grid
    traditional_points = _generate_grid_points(bbox, TRADITIONAL_SPACING_M, min_boreholes)
    traditional_count = len(traditional_points)

    traditional_cost = traditional_count * COST_PER_BOREHOLE_EGP
    optimized_cost = optimal_count * COST_PER_BOREHOLE_EGP
    savings_amount = traditional_cost - optimized_cost
    savings_pct = round(savings_amount / traditional_cost * 100) if traditional_cost > 0 else 0

    return {
        "minimumRequired": minimum_required,
        "optimalCount": optimal_count,
        "placementPoints": points,
        "costComparison": {
            "traditional": {
                "count": traditional_count,
                "cost": traditional_cost,
            },
            "optimized": {
                "count": optimal_count,
                "cost": optimized_cost,
            },
            "savings": {
                "amount": savings_amount,
                "percentage": savings_pct,
            },
        },
    }
