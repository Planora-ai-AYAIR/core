# GeoSense `ai-python` — Full Description & Formula Reference

وصف كامل لمشروع `ai-python` من جذر المسار، مع شرح **كل معادلة** اتحسبت بالتفصيل.
This document walks the entire service top-to-bottom and documents **every computed
formula** with its full derivation, units, and rationale.

---

## 1. What this service is

`ai-python` is the **Topography & Soil intelligence backend** for GeoSense (Planora).
Given a land parcel (polygon of coordinates inside Egypt), it returns:

- 🌱 **Soil composition** (clay / sand / silt / bulk density across 6 depths) — from ISRIC SoilGrids.
- ⛰️ **Terrain** (elevation, slope, slope zones, contour zones) — from a Copernicus DEM.
- 💧 **Hydrology** (TWI + water-accumulation risk) — from MERIT Hydro + slope.
- 🏗️ **Bearing capacity** (Model B, optional) — an ML estimate of buildable load.
- ⚠️ **Risk summary** — combined flood / slope / bearing risk.

It is a **FastAPI** app, authenticated to **Google Earth Engine (GEE)** with a service
account, and ships a small **single-page UI** at `/ui`.

---

## 2. Directory tree (from `apps/ai-python/`)

```
ai-python/
├── .env                      # Real secrets/config (GEE project, key path, Egypt bbox)
├── .env.template             # Documented template for .env
├── .gitignore
├── Dockerfile                # Placeholder image (not production-ready yet)
├── pyproject.toml            # Project metadata / tooling
├── requirements.txt          # Python dependencies
├── CODEBASE_EXPLANATION.md   # Earlier prose explanation of the codebase
├── describe.md               # ← THIS FILE
├── run.py                    # Convenience entry point: `py -3.13 run.py` starts the API
├── query_soil.py             # Standalone helper: query SoilGrids for a polygon from CLI
├── secrets/
│   └── planora_gee.json       # GEE service-account private key (DO NOT COMMIT)
└── app/
    ├── __init__.py
    ├── main.py                # FastAPI app: lifespan, CORS, routers, /ui mount, health
    ├── config.py              # Pydantic Settings loaded from .env
    ├── routers/
    │   ├── __init__.py
    │   ├── analyze.py         # POST /api/v1/analyze — synchronous soil+terrain for the UI
    │   └── topography.py      # POST /api/v1/topography/jobs — async job pipeline (Model B)
    ├── schemas/
    │   ├── __init__.py
    │   └── topography.py      # Pydantic request/response models
    ├── services/
    │   ├── __init__.py
    │   ├── gee_service.py     # GEE auth + DEM export + LIVE terrain (slope/TWI/contours)
    │   ├── soilgrids_service.py  # ISRIC SoilGrids REST client + unit conversions (incl. soc, pH)
    │   ├── terrain_service.py    # Local-raster terrain analysis (rasterio)
    │   ├── report_builder.py     # Maps raw soil/terrain dicts → contract sections (soil/terrain/hydrology)
    │   ├── topography_service.py # Orchestrator + Model B bearing capacity + risk
    │   ├── redis_service.py   # STUB (empty) — planned job cache
    │   └── tiles_service.py   # STUB (empty) — planned map-tile export
    └── static/
        └── index.html         # The /ui single-page app (coords in → soil+terrain out)
```

**Stubs:** `redis_service.py` and `tiles_service.py` are intentionally empty placeholders.

---

## 3. Request flow

There are **two** entry paths:

### A. Simple synchronous path (the UI uses this)
```
Browser (/ui)
   │  POST /api/v1/analyze  { points: [[lat,lon], ...] }
   ▼
routers/analyze.py
   ├── get_soil_composition(geo_json)           → soilgrids_service.py  → ISRIC REST API
   └── analyze_terrain(bbox, ...)               → terrain_service.py    (local raster)
          └── on FileNotFoundError → terrain_from_gee(geo_json) → gee_service.py (LIVE GEE)
   ▼
report_builder.build_analysis_data(...)   → maps raw service dicts to contract shapes
   ▼
{ statusCode, message, errors, data: {        ← unified response envelope
    location, soilComposition, terrain, hydrology, notes
}}  → rendered as cards in index.html / consumed by Postman
```

### B. Async job path (full pipeline incl. Model B)
```
POST /api/v1/topography/jobs  → 202 + python_job_id
   └── background _run_pipeline():
         ├── export_dem_for_parcel()   (GEE → Google Drive GeoTIFF)
         └── run_analysis()            → topography_service.py
               ├── get_soil_composition()
               ├── analyze_terrain()
               └── _estimate_bearing_capacity()  (Model B)
GET  /api/v1/topography/jobs/{id}  → poll status/results
```

---

## 4. Configuration (`config.py` + `.env`)

`Settings` (pydantic-settings) loads from `.env`:

| Key | Default | Meaning |
|---|---|---|
| `GEE_PROJECT` | `planora-497015` | GEE / GCP project id |
| `GEE_SERVICE_ACCOUNT_EMAIL` | `planora@planora-497015.iam.gserviceaccount.com` | service account |
| `GEE_SERVICE_ACCOUNT_KEY` | `./secrets/planora_gee.json` | private key path |
| `REDIS_URL` | `redis://localhost:6379` | (planned) job cache |
| `LOCAL_OUT_DIR` | `/tmp/geosense` | scratch dir for exports |
| `EGYPT_BBOX` | `[24.0, 22.0, 37.0, 32.0]` | `[minLon, minLat, maxLon, maxLat]` |
| `API_TITLE` / `API_VERSION` | `GeoSense API` / `0.1.0` | metadata |

**Read from OS env (NOT `.env`)** by `routers/topography.py` via `os.getenv`:
`RASTER_DIR` (default `/data/rasters/`) and `MODEL_B_PATH` (default `/data/models/model_b_bundle.joblib`).

---

## 5. FORMULA REFERENCE — every computed value

> الجزء ده هو المطلوب: كل قيمة اتحسبت، المعادلة بتاعتها، الوحدات، وليه.
> Notation: `ln` = natural logarithm; angles in **degrees** unless a `_rad` suffix is shown.

---

### 5.1 SoilGrids — `soilgrids_service.py`

SoilGrids returns values in **mapped integer units**. We convert them to human units.

#### (a) Polygon centroid
```
centroid = shapely.geometry.shape(polygon).centroid
lon = centroid.x ,  lat = centroid.y
```
The query is sent at the centroid `(lon, lat)`. Soil is reported as a single
representative profile for the parcel.

#### (b) Median of a depth profile (heuristic guard)
For each property, over its non-null depth values:
```
median = statistics.median( values )
```
Used only to decide whether a unit conversion is needed (guards against double-converting
already-converted data).

#### (c) Clay / Sand / Silt: **g/kg → %**
```
percent = raw_g_per_kg / 10            (applied when median > 100)
```
**Why ÷10:** 1000 g/kg = 100 %, therefore `% = (g/kg) ÷ 10`.
Example: `380 g/kg → 38.0 %`.

#### (d) Bulk density (`bdod`): **cg/cm³ → g/cm³**
```
g_per_cm3 = raw_cg_per_cm3 / 100       (applied when median > 10)
```
**Why ÷100:** 1 g = 100 cg, so `g/cm³ = (cg/cm³) ÷ 100`.
Example: `131 cg/cm³ → 1.31 g/cm³`.

#### (e) Dominant soil-type classification (surface 0–5 cm)
A simple texture rule (first match wins):
```
if  sand > 70 %  → "Sandy"
elif clay > 35 % → "Clayey"
elif silt > 50 % → "Silty"
else             → "Loamy"
```
Thresholds approximate the USDA texture triangle's dominant regions.

#### (f) Egypt bounds guard
The centroid must satisfy:
```
24.0 ≤ lon ≤ 37.0   AND   22.0 ≤ lat ≤ 32.0
```
otherwise `ValueError("Centroid outside supported region")`.

---

### 5.2 Terrain from local raster — `terrain_service.py`

Reads a pre-computed 7-band Egypt-wide GeoTIFF
(`copernicus_dem_features_egypt_250m.tif`, EPSG:32636, 250 m).
Bands: `elevation, slope, aspect, curvature, TWI, TRI, SRR`.

#### (a) Bounding-box reprojection (WGS84 → UTM 36N)
```
(minx, miny) = Transformer(EPSG:4326→EPSG:32636).transform(minLon, minLat)
(maxx, maxy) = Transformer(EPSG:4326→EPSG:32636).transform(maxLon, maxLat)
```
Needed because the raster is in metres (UTM), but the input bbox is in degrees.

#### (b) Elevation statistics (over valid, non-NaN pixels)
```
elev_mean = nanmean(elevation)
elev_min  = nanmin(elevation)
elev_max  = nanmax(elevation)
elev_std  = nanstd(elevation)          # population standard deviation
```

#### (c) Slope statistics
```
slope_mean = nanmean(slope)            # degrees
slope_max  = nanmax(slope)
```

#### (d) Generic pixel-percentage helper
For any boolean mask over `total` valid pixels:
```
pct(condition) = count_nonzero(condition) / total × 100
```
This single definition underlies every `*_pct` below.

#### (e) Slope zones (degrees)
```
flat_pct     = pct( slope < 2 )
gentle_pct   = pct( 2 ≤ slope < 5 )
moderate_pct = pct( 5 ≤ slope < 15 )
steep_pct    = pct( slope ≥ 15 )
```

#### (f) Water-accumulation zones (from the raster's TWI band)
With `threshold = twi_threshold` (default **8.0**):
```
high_risk_pct   = pct( TWI > threshold )
low_risk_pct    = pct( TWI < 5 )
medium_risk_pct = max( 0, 100 − high_risk_pct − low_risk_pct )
```

#### (g) Contour zones (equal elevation bands)
```
span    = elev_max − elev_min
n_zones = 4                                  if span ≤ 0
        = max( 4, min(6, ⌊span / contour_interval⌋ ) )   otherwise
edges   = linspace(elev_min, elev_max, n_zones + 1)
```
For band `i` (last band is inclusive on the upper edge):
```
in_band   = (elevation ≥ edges[i]) AND (elevation < edges[i+1])
area_pct  = pct(in_band)
```
`contour_interval` default = **0.5 m**.

#### (h) Qualitative classes
```
terrain_class:  <2 Flat | <5 Gently Sloped | <15 Moderately Sloped | else Steep   (on slope_mean)
drainage_risk:  >20 High | >5 Medium | else Low                                   (on high_risk_pct)
buildable_area_pct = flat_pct + gentle_pct
```

---

### 5.3 Terrain LIVE from GEE — `gee_service.py :: terrain_from_gee()`

Used automatically when the local raster is absent. No local file needed.
**Elevation/slope** ← Copernicus **GLO-30** (30 m). **Hydrology** ← **MERIT Hydro** (~90 m).

#### (a) Projection fix before slope
```
native_proj = collection.first().projection()         # 30 m projection
dem         = collection.mosaic().setDefaultProjection(native_proj).clip(geom)
slope       = ee.Terrain.slope(dem)                    # Horn (1981) 3×3 method, degrees
```
**Why:** `mosaic()` strips projection metadata, which makes `ee.Terrain.slope`
degenerate (returns ~0 / null). Restoring the native 30 m projection fixes it.

#### (b) Elevation & slope statistics
Computed with `reduceRegion` over the polygon at `scale = 30 m`:
```
DEM_mean, DEM_min, DEM_max, DEM_stdDev   (ee.Reducer.mean+minMax+stdDev)
slope_mean, slope_max                    (ee.Reducer.mean+max)
```

#### (c) Slope-zone fractions (mask-mean trick)
For a 0/1 mask image, **the regional mean equals the fraction of `1` pixels**:
```
flat_pct = mean( slope < 2 ) × 100
```
and likewise for gentle (2–5), moderate (5–15), steep (≥15). Same thresholds as §5.2(e).

#### (d) **TWI — Topographic Wetness Index** (the key hydrology formula)

Definition:
```
TWI = ln( a / tan β )
```
where
- **a** = *specific catchment area* = upslope contributing area per unit contour width [m].
- **β** = local slope angle.

Because GEE has **no native flow-accumulation**, we take the pre-computed
**upstream drainage area** from MERIT Hydro and combine it with the GLO-30 slope:

```
upa        = MERIT/Hydro/v1_0_1 .select("upa")     # upstream drainage area [km²]
A          = upa × 1e6                              # → m²
w          = 92.77 m                                # MERIT cell width (~3 arc-sec @ equator)
a          = A / w                                  # specific catchment area [m]

β_rad      = slope_deg × π / 180
tan β      = max( tan(β_rad), 0.001 )               # clamp: avoid ÷0 on flat ground

TWI        = ln( a / tan β )
```

Notes:
- The `0.001` floor caps TWI on perfectly flat cells (where `tan β → 0` would give `+∞`).
- Higher TWI ⇒ wetter / more water-accumulating ground.

Then, with `threshold = 8.0`:
```
high_risk_pct   = mean( TWI > 8 ) × 100
low_risk_pct    = mean( TWI < 5 ) × 100
medium_risk_pct = max( 0, 100 − high_risk_pct − low_risk_pct )
twi_mean        = mean( TWI )
drainage_risk   = High if high_risk_pct > 20 else Medium if > 5 else Low
```

#### (e) Contour zones (live, second pass)
Identical maths to §5.2(g), but the per-band area share is computed in GEE:
```
n_zones  = max( 4, min(6, ⌊(elev_max − elev_min) / 0.5⌋ ) )
edges    = linspace(elev_min, elev_max, n_zones + 1)
area_pct(band i) = mean( (DEM ≥ edges[i]) AND (DEM < edges[i+1]) ) × 100
```
A second `getInfo()` is used here because the band edges depend on the
`elev_min/elev_max` learned from the first pass.

#### (f) Summary fields
```
buildable_area_pct = flat_pct + gentle_pct
terrain_class      = (same thresholds as §5.2(h), on slope_mean)
```

---

### 5.4 Orchestrator + Model B — `topography_service.py`

#### (a) Model B feature vector (order matters — taken from the bundle)
```
features = [ clay_0_5, sand_0_5, silt_0_5, bdod_0_5,
             clay_30_60, sand_30_60, bdod_30_60,
             slope (=slope_mean_deg), TWI (=twi_mean) ]
```
If any feature is `None`, bearing capacity is skipped (recorded in `processing_notes`).

#### (b) Quantile predictions (XGBoost quantile models)
```
median = model_median.predict(X)        # kPa
p10    = model_p10.predict(X)
p90    = model_p90.predict(X)
```

#### (c) Uncertainty
```
uncertainty_pct = (p90 − p10) / median × 100        (0 if median == 0)
```
Width of the 10–90 % prediction interval, expressed as % of the median.

#### (d) Bearing class (binning)
```
idx   = digitize(median, CLASS_BINS) − 1            # numpy.digitize
idx   = clamp(idx, 0, len(CLASS_NAMES) − 1)
class = CLASS_NAMES[idx]                             # e.g. Low / Medium / High
```

#### (e) Class → guidance lookups
```
Low    → floors "1-2"  , "Deep foundation / piles likely required"
Medium → floors "3-10" , "Shallow possible; verify settlement"
High   → floors "10+"  , "Shallow foundation likely adequate"
```

#### (f) Overall-risk score (integer points → band)
```
score = 0
bearing class Low  → +3   ;  Medium → +1
flood high_risk_pct  > 30 → +2   ;  > 15 → +1
slope_mean           > 15 → +2   ;  >  5 → +1

overall_risk = High   if score ≥ 4
             = Medium if score ≥ 2
             = Low    otherwise
```

#### (g) Risk-summary booleans
```
flood_risk = (water_accumulation.high_risk_pct > 20)
slope_risk = terrain_class ∈ { "Moderately Sloped", "Steep" }
```

---

### 5.5 Analyze router — `routers/analyze.py`

#### (a) Ring + bbox construction
User enters `(lat, lon)`; GeoJSON needs `[lon, lat]`:
```
ring = [ [lon, lat] for (lat, lon) in points ]      # closed automatically
bbox = [ min(lons), min(lats), max(lons), max(lats) ]
```

#### (b) Automatic lat/lon-order detection
Some users paste `(lon, lat)`. Logic:
```
1. Try order as given.
2. If SoilGrids returns no data (clay_0_5 is None):
      retry with axes swapped.
      if swapped yields data → keep swapped result + note "auto-corrected".
3. If still no data → note "water/coast/no-data zone".
```
This is why the original Alexandria-coast input (`31.49, 29.78` typed as lon,lat)
auto-corrects to the desert parcel `lat 29.78, lon 31.49` that has real soil data.

---

### 5.6 GEE service — auth, validation, DEM export

#### (a) Egypt bbox validation (`validate_bbox_egypt`)
```
24.0 ≤ minLon  AND  maxLon ≤ 37.0
22.0 ≤ minLat  AND  maxLat ≤ 32.0
```
Whole bbox must be inside Egypt.

#### (b) DEM export buffer (`export_dem_for_parcel`)
```
buffer_metres = 0.005° × 111_320 m/°  ≈ 556.6 m
```
A ~556 m buffer is added around the parcel so edge terrain that influences the
site is captured. The DEM is mosaicked, clipped, and exported to Google Drive as a
GeoTIFF in **EPSG:32636** at **30 m** resolution (`maxPixels = 1e9`).

---

## 6. Data sources & resolutions

| Layer | Dataset | Resolution | Used for |
|---|---|---|---|
| Soil | ISRIC SoilGrids v2.0 (REST) | 250 m | clay/sand/silt/bdod profiles |
| Elevation / Slope | Copernicus DEM **GLO-30** | 30 m | elevation, slope, zones, contours |
| Hydrology (`upa`) | **MERIT Hydro** v1.0.1 | ~90 m | TWI numerator (drainage area) |
| Local raster (optional) | Egypt DEM features GeoTIFF | 250 m | offline terrain (if present) |

---

## 7. Running it

```powershell
cd apps\ai-python
py -3.13 run.py                 # simplest — starts uvicorn on :8000 with reload
# …or equivalently:
py -3.13 -m uvicorn app.main:app --reload --port 8000
# UI:      http://localhost:8000/ui
# Docs:    http://localhost:8000/docs
# Health:  http://localhost:8000/api/v1/health
```

`POST /api/v1/analyze` body (send this from Postman, coordinates as `[lat, lon]`):
```json
{ "points": [[31.4976423, 29.781129], [31.5032575, 29.7779854],
             [31.4986716, 29.7745251], [31.4934212, 29.7780081]] }
```

Response — unified envelope with three sections under `data`:
```json
{
  "statusCode": 200,
  "message": "Analysis completed successfully",
  "errors": null,
  "data": {
    "location":        { "centroid": {...}, "boundingBox": {...} },
    "soilComposition": { "composition": {...}, "properties": {...},
                         "classification": {...}, "multiDepthProfile": [...] },
    "terrain":         { "elevation": {...}, "slope": {...},
                         "slopeAnalysis": {...}, "contourZones": [...], "summary": {...} },
    "hydrology":       { "twiMean": ..., "waterAccumulation": {...},
                         "drainageRisk": ..., "pondingRisk": {...} },
    "notes": [...]
  }
}
```

---

## 8. Known gaps / TODO

- `redis_service.py`, `tiles_service.py` are empty stubs.
- `Dockerfile` is a placeholder (no real runtime command).
- Model B bundle (`MODEL_B_PATH`) must exist for bearing capacity; otherwise it's skipped.
- `requirements.txt` lacks `joblib` and `xgboost` (needed by Model B).
- Live GEE TWI mixes 30 m slope with ~90 m drainage area — fine for pre-qualification,
  not survey-grade.
