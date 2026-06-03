"""
Redis job status service.
Falls back to in-memory dict if Redis is unavailable (dev mode).
"""

import json
import logging
from typing import Optional

logger = logging.getLogger(__name__)

JOB_TTL = 86400  # 24 hours

# ── Try to connect to Redis ───────────────────────────────────
_redis_client = None

def _get_redis():
    global _redis_client
    if _redis_client is not None:
        return _redis_client
    try:
        import redis
        from app.config import settings
        r = redis.Redis.from_url(settings.redis_url, decode_responses=True)
        r.ping()
        _redis_client = r
        logger.info("✅ Redis connected")
        return _redis_client
    except Exception as e:
        logger.warning(f"⚠️  Redis unavailable ({e}) — using in-memory store")
        return None

# ── In-memory fallback ────────────────────────────────────────
_memory_store: dict = {}


def set_status(job_id:    str,
               status:    str,
               progress:  int,
               parcel_id: str,
               results    = None,
               error      = None) -> None:
    payload = json.dumps({
        "python_job_id": job_id,
        "parcel_id":     parcel_id,
        "status":        status,
        "progress":      progress,
        "results":       results,
        "error":         error,
    })

    r = _get_redis()
    if r:
        r.setex(f"python_job:{job_id}", JOB_TTL, payload)
    else:
        _memory_store[f"python_job:{job_id}"] = payload


def get_status(job_id: str) -> Optional[dict]:
    r = _get_redis()
    if r:
        raw = r.get(f"python_job:{job_id}")
    else:
        raw = _memory_store.get(f"python_job:{job_id}")

    return json.loads(raw) if raw else None