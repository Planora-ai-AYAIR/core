"""Report router — API Contract §3.5.

POST /api/v1/reports/jobs              — Submit PDF generation job
GET  /api/v1/reports/jobs/{pythonJobId} — Poll job status / results
"""

import logging
import uuid

from fastapi import APIRouter, HTTPException, BackgroundTasks
from fastapi.responses import JSONResponse

from app.schemas.reports import ReportJobRequest
from app.schemas.common import (
    accepted_response,
    error_response,
    success_response,
    utc_now_iso,
)

logger = logging.getLogger(__name__)

router = APIRouter(prefix="/api/v1/reports", tags=["reports"])

_jobs: dict = {}


# ── POST /api/v1/reports/jobs ─────────────────────────────────
@router.post("/jobs", status_code=202)
async def submit_report_job(req: ReportJobRequest, bg: BackgroundTasks):
    """Submit a PDF report generation job (§3.5.1)."""
    python_job_id = f"pyjob_pdf_{uuid.uuid4().hex[:12]}"
    accepted_at = utc_now_iso()

    _jobs[python_job_id] = {
        "pythonJobId": python_job_id,
        "status": "queued",
        "results": None,
        "error": None,
    }

    bg.add_task(_run_report_pipeline, python_job_id, req)

    return JSONResponse(
        status_code=202,
        content=accepted_response(
            data={
                "pythonJobId": python_job_id,
                "status": "queued",
                "acceptedAt": accepted_at,
            },
            message="Python report job queued",
        ),
    )


# ── GET /api/v1/reports/jobs/{pythonJobId} ────────────────────
@router.get("/jobs/{pythonJobId}")
async def get_report_status(pythonJobId: str):
    """Poll report job status (§3.5.2)."""
    job = _jobs.get(pythonJobId)

    if not job:
        raise HTTPException(
            status_code=404,
            detail=error_response(
                status_code=404,
                message="Report not found or still generating",
                errors=[{
                    "field": "pythonJobId",
                    "code": "REPORT_NOT_READY",
                    "message": f"No report job found with id {pythonJobId}",
                }],
            ),
        )

    msg = {
        "queued": "Job queued",
        "processing": "Job in progress",
        "completed": "Success",
        "failed": "Job failed",
    }.get(job["status"], "Unknown")

    return JSONResponse(
        status_code=200,
        content=success_response(
            data={
                "pythonJobId": job["pythonJobId"],
                "status": job["status"],
                "results": job.get("results"),
            },
            message=msg,
        ),
    )


# ── Background pipeline ──────────────────────────────────────
async def _run_report_pipeline(python_job_id: str, req: ReportJobRequest):
    _jobs[python_job_id]["status"] = "processing"

    try:
        # PDF assembly — placeholder that generates S3 URL.
        # In production, this would use a PDF library (e.g., WeasyPrint, ReportLab)
        # to assemble the report from module results.
        parcel_id = req.parcelId
        from datetime import datetime
        date_str = datetime.now().strftime("%Y%m%d")

        results = {
            "pdfUrl": f"s3://bucket/reports/report_{parcel_id}_{date_str}.pdf",
            "pageCount": 6,
            "sizeBytes": 2450000,
        }

        _jobs[python_job_id].update({
            "status": "completed",
            "results": results,
        })

    except Exception as e:
        logger.error(f"Report pipeline failed for {python_job_id}: {e}", exc_info=True)
        _jobs[python_job_id].update({
            "status": "failed",
            "error": {"code": "PROCESSING_ERROR", "message": str(e)},
        })
