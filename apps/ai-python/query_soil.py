"""
Query SoilGrids for a polygon and print the soil composition.

Usage:
    py -3.13 query_soil.py

Edit POINTS below with your own coordinates as (lat, lon) pairs.
The first and last point should be the same (closed ring).
"""
import json

from app.services.soilgrids_service import get_soil_composition

# ---- EDIT YOUR COORDINATES HERE (lat, lon) ----
POINTS = [
    (30.6000, 31.0000),
    (30.6030, 31.0040),
    (30.5990, 31.0070),
    (30.5960, 31.0030),
    (30.6000, 31.0000),
]
# ------------------------------------------------


def main() -> None:
    # GeoJSON needs [lon, lat] order
    ring = [[lon, lat] for (lat, lon) in POINTS]
    geo_json = {"type": "Polygon", "coordinates": [ring]}

    result = get_soil_composition(geo_json)

    if result["clay_0_5"] is None:
        print("\n⚠️  SoilGrids returned NO data at this location "
              "(likely water/coast/no-data zone). Try an inland parcel.\n")

    print(json.dumps(result, indent=2))
# py -3.13 -m uvicorn app.main:app --reload --port 8000

if __name__ == "__main__":
    main()
