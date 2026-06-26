"""Webhook notification service.

Fires an HMAC-SHA256-signed POST to a configured endpoint whenever an
analysis job completes.  Silently logs and swallows errors so a webhook
failure never blocks the main analysis pipeline.
"""

from __future__ import annotations

import hashlib
import hmac
import json
import logging
from datetime import datetime, timezone
from typing import Any

import httpx

from app.config import settings

logger = logging.getLogger(__name__)


def _build_envelope(job_id: str, module: str, data: dict[str, Any]) -> dict[str, Any]:
    return {
        "eventType": "analysis.completed",
        "jobId": job_id,
        "module": module,
        "data": data,
        "timestamp": datetime.now(timezone.utc).isoformat().replace("+00:00", "Z"),
    }


def _compute_signature(body: bytes, secret: str) -> str:
    return hmac.new(
        secret.encode("utf-8"),
        body,
        hashlib.sha256,
    ).hexdigest()


async def send_analysis_webhook(
    job_id: str,
    module: str,
    result_data: dict[str, Any],
) -> None:
    """Send a signed webhook notification for a completed job.

    No-ops silently when webhook settings are not configured.
    """
    url = settings.webhook_url
    secret = settings.webhook_secret
    if not url or not secret:
        return

    envelope = _build_envelope(job_id, module, result_data)
    body = json.dumps(envelope, separators=(",", ":"), ensure_ascii=False).encode("utf-8")
    signature = _compute_signature(body, secret)

    try:
        async with httpx.AsyncClient() as client:
            response = await client.post(
                url,
                content=body,
                headers={
                    "Content-Type": "application/json",
                    "X-Webhook-Signature": signature,
                },
                timeout=10.0,
            )
            response.raise_for_status()
            logger.info("Webhook delivered for job %s → %s", job_id, url)
    except Exception as exc:
        logger.error("Webhook delivery failed for job %s: %s", job_id, exc)
