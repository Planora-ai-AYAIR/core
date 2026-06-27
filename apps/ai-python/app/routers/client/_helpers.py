"""Shared helpers for the client-facing (§2) routers.

Job lifecycle (queued → processing → completed) over the in-memory store, the
parcel/result guards that emit the contracted §4 error codes, and a tiny
self-contained PDF builder for the §2.7 download endpoint.
"""

from __future__ import annotations

import uuid

from fastapi import HTTPException

from app.services import store
from app.services import client_mocks
from app.services.webhook_service import send_analysis_webhook
from app.schemas.common import ErrorCode, error_response, utc_now_iso

ESTIMATED_DURATION = "2-6 hours"


# ── Job lifecycle ─────────────────────────────────────────────
def make_job(module: str, parcel_id: str) -> dict:
    """Register a queued job for (module, parcelId) and return it."""
    job_id = f"job_{module}_{uuid.uuid4().hex[:8]}"
    job = {
        "jobId": job_id,
        "parcelId": parcel_id,
        "module": module,
        "status": "queued",
        "progressPercentage": 0,
        "startedAt": utc_now_iso(),
        "completedAt": None,
        "nextModule": store.next_module(module),
        "message": f"{module} job queued",
    }
    store.save_job(job)
    return job


async def run_job(job_id: str, module: str, parcel_id: str) -> None:
    """Background task: process the job and persist a contract-shaped result."""
    store.update_job(
        job_id, status="processing", progressPercentage=50,
        message=f"{module} analysis in progress",
    )
    result = client_mocks.build_result(module, parcel_id)
    store.save_result(parcel_id, module, result)
    store.update_job(
        job_id, status="completed", progressPercentage=100,
        completedAt=utc_now_iso(),
        message=f"{module} analysis completed successfully",
    )
    await send_analysis_webhook(job_id, result, "analysis.completed")


# ── Guards ────────────────────────────────────────────────────
def require_parcel(parcel_id: str) -> dict:
    """Return the parcel or raise 404 PARCEL_NOT_FOUND (§4)."""
    parcel = store.get_parcel(parcel_id)
    if parcel is None:
        raise HTTPException(
            status_code=404,
            detail=error_response(
                status_code=404,
                message="Parcel not found",
                errors=[{
                    "field": "parcelId",
                    "code": ErrorCode.PARCEL_NOT_FOUND.value,
                    "message": f"No parcel found with id {parcel_id}",
                }],
            ),
        )
    return parcel


def require_result(parcel_id: str, module: str) -> dict:
    """Return a completed module result or raise the contracted error.

    404 PARCEL_NOT_FOUND if the parcel is unknown, else 409 JOB_NOT_COMPLETED
    while results are not yet available (§4).
    """
    require_parcel(parcel_id)
    result = store.get_result(parcel_id, module)
    if result is None:
        raise HTTPException(
            status_code=409,
            detail=error_response(
                status_code=409,
                message="Results requested before job completion",
                errors=[{
                    "field": "parcelId",
                    "code": ErrorCode.JOB_NOT_COMPLETED.value,
                    "message": (
                        f"{module} results are not ready for {parcel_id}. "
                        "Poll the job status endpoint until status=completed."
                    ),
                }],
            ),
        )
    return result


# ── Minimal PDF (§2.7 download) ───────────────────────────────
def build_minimal_pdf(title_lines: list[str]) -> bytes:
    """Assemble a tiny but structurally valid single-page PDF.

    Offsets in the xref table are computed dynamically so the file is valid
    regardless of the text content. Good enough as a §2.7 download placeholder.
    """
    def esc(s: str) -> str:
        return s.replace("\\", r"\\").replace("(", r"\(").replace(")", r"\)")

    text_ops = ["BT", "/F1 16 Tf", "72 760 Td", "18 TL"]
    for line in title_lines:
        text_ops.append(f"({esc(line)}) Tj")
        text_ops.append("T*")
    text_ops.append("ET")
    # PDF base-14 fonts are Latin-1; drop any non-encodable chars defensively.
    stream = "\n".join(text_ops).encode("latin-1", "replace")

    objects = [
        b"<< /Type /Catalog /Pages 2 0 R >>",
        b"<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
        b"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] "
        b"/Resources << /Font << /F1 5 0 R >> >> /Contents 4 0 R >>",
        b"<< /Length " + str(len(stream)).encode() + b" >>\nstream\n" + stream + b"\nendstream",
        b"<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
    ]

    out = bytearray(b"%PDF-1.4\n")
    offsets = []
    for i, obj in enumerate(objects, start=1):
        offsets.append(len(out))
        out += f"{i} 0 obj\n".encode() + obj + b"\nendobj\n"

    xref_pos = len(out)
    n = len(objects) + 1
    out += f"xref\n0 {n}\n".encode()
    out += b"0000000000 65535 f \n"
    for off in offsets:
        out += f"{off:010d} 00000 n \n".encode()
    out += (
        f"trailer\n<< /Size {n} /Root 1 0 R >>\nstartxref\n{xref_pos}\n%%EOF".encode()
    )
    return bytes(out)
