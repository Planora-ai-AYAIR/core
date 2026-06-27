# AI Python Team — Required Changes

> This file lists all changes the AI Python service needs to make so the .NET backend can integrate correctly.
> Send this file to the AI Python team.
> Date: 2026-06-27

---

## 1. 🔴 CRITICAL — Add Bearing Module to §3 Internal API

**Current state:** The §3 internal API (`/api/v1/`) has per-module endpoints for topography, soil, risk, borehole, and reports — but **no bearing endpoint**. Bearing only exists in the §2 client-facing API (`/api/bearing/jobs`).

**What we need:**
```
POST /api/v1/bearings/jobs
```

This endpoint must follow the exact same pattern as the other §3 module endpoints:

**Request body** (extends `BaseJobRequest`):
```json
{
  "jobId": "our-dotnet-analysis-job-id",
  "parcelId": "parcel-guid",
  "geoJson": { "type": "Polygon", "coordinates": [[[lon, lat], ...]] },
  "bbox": { "minX": ..., "minY": ..., "maxX": ..., "maxY": ... },
  "soilData": {
    "clayContent": 20.0,
    "sandContent": 55.0,
    "siltContent": 25.0,
    "bulkDensity": 1.45,
    "waterTableDepth": 5.0
  }
}
```

**Response** (202 Accepted, same envelope as other modules):
```json
{
  "statusCode": 202,
  "message": "Python bearing job queued",
  "errors": null,
  "data": {
    "pythonJobId": "pyjob_bearing_a1b2c3d4e5f6",
    "status": "queued",
    "acceptedAt": "2026-06-27T12:00:00.000000+00:00"
  }
}
```

**Webhook on completion:**
```json
{
  "eventType": "bearing.completed",
  "jobId": "pyjob_bearing_a1b2c3d4e5f6",
  "data": {
    "bearingCapacityKpa": 150.0,
    "classification": "Medium",
    "confidence": 0.85,
    "range": "100-200 kPa",
    "trafficLight": "green",
    "recommendedFoundation": "Strip footing",
    "maxFloorsWithoutDeepFoundation": 5,
    "floorCountCategory": "low-rise",
    "uncertaintyRange": {
      "minimumKpa": 120.0,
      "maximumKpa": 180.0
    },
    "featureImportance": [...],
    "soilFactors": { ... },
    "disclaimer": "This is an AI-derived estimate...",
    "modelMetadata": {
      "modelName": "XGBoost-Bearing-v2",
      "framework": "xgboost",
      "trainingR2": 0.89,
      "shapEnabled": true
    }
  },
  "timestamp": "2026-06-27T12:34:56.789000Z"
}
```

**Files to create/modify:**
- `app/routers/bearing.py` — new router under `prefix="/api/v1/bearings"`
- `app/schemas/bearing.py` — `BearingJobRequest` extending `BaseJobRequest` with optional `soilData`
- Register router in `app/main.py` (or wherever §3 routers are mounted)

---

## 2. 🔴 CRITICAL — Add Per-Module Failure Webhooks

**Current state:** When a module job fails, the error is stored in-memory but **no webhook is fired**. The .NET backend never learns about failures — jobs sit in `Running` forever.

**What we need:** Fire a webhook for each failure event:

| Event Type | When |
|-----------|------|
| `topography.failed` | Topography job throws an exception |
| `soil.failed` | Soil job throws an exception |
| `bearing.failed` | Bearing job throws an exception |
| `risk.failed` | Risk job throws an exception |
| `borehole.failed` | Borehole job throws an exception |
| `pdf.failed` | PDF job throws an exception |

**Webhook envelope shape:**
```json
{
  "eventType": "topography.failed",
  "jobId": "pyjob_topo_a1b2c3d4e5f6",
  "data": {
    "code": "INTERNAL_ERROR",
    "message": "GEE export timed out after 300s"
  },
  "timestamp": "2026-06-27T12:34:56.789000Z"
}
```

**Implementation:** In each §3 module router's exception handler, call `send_analysis_webhook()` with the `*.failed` event type, same as the `*.completed` calls. Example in `topography.py`:

```python
except Exception as e:
    _jobs[python_job_id].update({
        "status": "failed",
        "error": {"code": ErrorCode.INTERNAL_ERROR.value, "message": str(e)},
    })
    # ADD THIS:
    await send_analysis_webhook(
        event_type="topography.failed",
        job_id=python_job_id,
        data={"code": ErrorCode.INTERNAL_ERROR.value, "message": str(e)},
    )
```

---

## 3. 🟠 HIGH — Add Job Status Polling Endpoint (GET per module)

**Current state:** The aggregated analysis has a GET endpoint, but **per-module GET endpoints are missing** from §3. We need these as a fallback when webhooks fail.

**What we need:**
```
GET /api/v1/topography/jobs/{pythonJobId}
GET /api/v1/soil/jobs/{pythonJobId}
GET /api/v1/risks/jobs/{pythonJobId}
GET /api/v1/boreholes/jobs/{pythonJobId}
GET /api/v1/bearings/jobs/{pythonJobId}
GET /api/v1/reports/jobs/{pythonJobId}
```

**Response:**
```json
{
  "statusCode": 200,
  "message": "Job status",
  "errors": null,
  "data": {
    "pythonJobId": "pyjob_topo_a1b2c3d4e5f6",
    "status": "completed",
    "results": { ... },
    "error": null
  }
}
```

For failed jobs:
```json
{
  "statusCode": 200,
  "message": "Job failed",
  "errors": null,
  "data": {
    "pythonJobId": "pyjob_topo_a1b2c3d4e5f6",
    "status": "failed",
    "results": null,
    "error": {
      "code": "INTERNAL_ERROR",
      "message": "GEE export timed out"
    }
  }
}
```

---

## 4. 🟠 HIGH — Add Webhook Retry with Exponential Backoff

**Current state:** Webhooks are fire-and-forget. If the POST fails (timeout, connection error, non-2xx), the error is silently swallowed. No retry, no dead-letter queue.

**What we need:**
- Retry webhook delivery up to **3 times** with exponential backoff (1s, 5s, 25s).
- If all retries fail, log the failure prominently (not just `logger.error`).
- Consider adding failed webhooks to a small in-memory retry queue.

**Suggested implementation in `webhook_service.py`:**
```python
async def send_analysis_webhook_with_retry(event_type, job_id, data, max_retries=3):
    for attempt in range(max_retries + 1):
        try:
            await _send_webhook(event_type, job_id, data)
            return  # success
        except Exception as e:
            if attempt < max_retries:
                wait_time = 2 ** attempt  # 1s, 2s, 4s
                logger.warning(f"Webhook attempt {attempt+1} failed, retrying in {wait_time}s: {e}")
                await asyncio.sleep(wait_time)
            else:
                logger.critical(f"Webhook delivery failed after {max_retries} retries for {event_type}/{job_id}")
```

---

## 5. 🟡 MEDIUM — Accept `jobId` in `BaseJobRequest` (Already Implemented ✅)

Just confirming: the `BaseJobRequest` already has `jobId` as a required field. Our .NET backend will now send the `AnalysisJob.Id` as `jobId` in every per-module request. No action needed on your side, this is just an acknowledgment.

---

## 6. 🟡 MEDIUM — Return `pythonJobId` in 202 Response (Already Implemented ✅)

Confirming: the §3 response envelope already uses `pythonJobId` in the `data` field. Our .NET backend now correctly parses `data.pythonJobId`. No action needed.

---

## 7. 🟡 MEDIUM — Add Idempotency Key Support

**Current state:** Each POST creates a new job regardless of duplicate requests. Hangfire retries on the .NET side could create duplicate jobs.

**What we need (optional but recommended):**
- Accept an `Idempotency-Key` header on all §3 POST endpoints.
- If a request with the same key arrives within a configurable window (e.g., 1 hour), return the **original** 202 response instead of creating a new job.
- Store the mapping: `idempotency_key → pythonJobId` in the in-memory dict.

---

## 8. 🟢 LOW — Document Webhook Payload Shapes

Please add a `WEBHOOK_CONTRACT.md` file to the AI Python repo that documents the exact `data` payload shape for every `*.completed` and `*.failed` event type, so we can build our deserialization with confidence. Currently we have to read source code to figure out the exact shapes.

---

## Summary Table

| # | Requirement | Priority | Impact if Not Done |
|---|------------|----------|-------------------|
| 1 | Add `POST /api/v1/bearings/jobs` | 🔴 Critical | Bearing module cannot be used from §3 |
| 2 | Fire `*.failed` webhooks | 🔴 Critical | Failed jobs sit in `Running` forever |
| 3 | Add per-module GET status endpoints | 🟠 High | No fallback when webhooks fail |
| 4 | Webhook retry with backoff | 🟠 High | Transient failures cause permanent data loss |
| 5 | `jobId` in BaseJobRequest | ✅ Already done | — |
| 6 | `pythonJobId` in response | ✅ Already done | — |
| 7 | Idempotency key support | 🟡 Medium | Hangfire retries create duplicate jobs |
| 8 | Document webhook payloads | 🟢 Low | Integration requires reading source code |

---

## .NET Backend Changes Already Made (For Reference)

The .NET team has already completed these changes on our side:

1. ✅ **Response envelope** — All Refit clients now deserialize `AiResponseEnvelope<AiJobAccepted>` and extract `data.pythonJobId`
2. ✅ **Request schema** — All per-module requests now send `{ jobId, parcelId, geoJson, bbox, options? }` matching Python's `BaseJobRequest`
3. ✅ **GeoJSON shape** — Converted from `string boundaryGeoJson` to `AiGeoJsonPolygon` object with `type` + `coordinates`
4. ✅ **BoundingBox** — Auto-computed from polygon envelope and sent as `bbox`
5. ✅ **Bearing as separate module** — `AnalysisType.Bearing` added to enum, `BearingResult` domain entity, `ProcessBearingJob` background job, `SubmitBearingJob` command/handler, `IBearingResultRepository`, EF configuration, and DI registration all created
6. ✅ **BearingCompleted webhook handler** — Updated to save to `BearingResult` table instead of embedding bearing fields in `SoilResult`
7. ✅ **StartAnalysis** — `ResolveModules` now treats `Soil` and `Bearing` as independent modules, each enqueued separately
8. ✅ **Base URL** — Must be `http://{host}:8000/api/v1` (verification needed in appsettings)
9. ✅ **Build compiles** with 0 errors
