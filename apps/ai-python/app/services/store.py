"""In-memory store for the client-facing API (§2).

A lightweight stand-in for the .NET backend's PostGIS + Redis layer, used so the
GeoSense AI engine can expose a coherent, browsable §2 surface end-to-end
(register parcel → submit job → poll status → fetch results) without external
infrastructure.

NOT for production: process-local, non-persistent, cleared on restart.
"""

from __future__ import annotations

import threading
from typing import Optional

_lock = threading.RLock()

# parcelId -> parcel dict
PARCELS: dict[str, dict] = {}

# jobId -> job dict {jobId, parcelId, module, status, progressPercentage,
#                    startedAt, completedAt, nextModule, message}
JOBS: dict[str, dict] = {}

# (parcelId, module) -> result dict (the §2 GET-results payload)
RESULTS: dict[tuple[str, str], dict] = {}

# Order of modules used to compute `nextModule` hints (§2.8).
MODULE_ORDER = ["topography", "soil", "bearing", "risk", "borehole", "report"]


# ── Parcels ───────────────────────────────────────────────────
def save_parcel(parcel: dict) -> None:
    with _lock:
        PARCELS[parcel["parcelId"]] = parcel


def get_parcel(parcel_id: str) -> Optional[dict]:
    with _lock:
        return PARCELS.get(parcel_id)


# ── Jobs ──────────────────────────────────────────────────────
def save_job(job: dict) -> None:
    with _lock:
        JOBS[job["jobId"]] = job


def get_job(job_id: str) -> Optional[dict]:
    with _lock:
        return JOBS.get(job_id)


def update_job(job_id: str, **fields) -> None:
    with _lock:
        if job_id in JOBS:
            JOBS[job_id].update(fields)


def next_module(module: str) -> Optional[str]:
    """Return the module expected to run after `module` (§2.8 nextModule)."""
    try:
        idx = MODULE_ORDER.index(module)
    except ValueError:
        return None
    return MODULE_ORDER[idx + 1] if idx + 1 < len(MODULE_ORDER) else None


# ── Results ───────────────────────────────────────────────────
def save_result(parcel_id: str, module: str, result: dict) -> None:
    with _lock:
        RESULTS[(parcel_id, module)] = result
        parcel = PARCELS.get(parcel_id)
        if parcel is not None:
            completed = parcel.setdefault("modulesCompleted", [])
            if module not in completed:
                completed.append(module)


def get_result(parcel_id: str, module: str) -> Optional[dict]:
    with _lock:
        return RESULTS.get((parcel_id, module))
