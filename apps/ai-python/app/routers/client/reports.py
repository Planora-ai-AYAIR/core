"""Module 6 — Downloadable PDF Report (client-facing) — API Contract §2.7.

POST /api/reports/jobs           — Submit PDF generation job (202)
GET  /api/reports/{parcelId}/pdf — Download the generated PDF (binary)
"""

from datetime import datetime

from fastapi import APIRouter, BackgroundTasks, HTTPException, Query, Response

from app.schemas.client import ReportJobSubmit, JobQueued
from app.schemas.common import (
    Envelope, ErrorCode, accepted_response, error_response,
)
from app.services import store
from app.routers.client._helpers import (
    make_job, run_job, require_parcel, ESTIMATED_DURATION, build_minimal_pdf,
)

router = APIRouter(prefix="/api/reports", tags=["client: reports"])
MODULE = "report"


@router.post("/jobs", status_code=202, response_model=Envelope[JobQueued])
async def submit_report_job(req: ReportJobSubmit, bg: BackgroundTasks):
    """Submit a PDF report generation job (§2.7.1)."""
    require_parcel(req.parcelId)
    job = make_job(MODULE, req.parcelId)
    bg.add_task(run_job, job["jobId"], MODULE, req.parcelId)
    return accepted_response(
        data={
            "jobId": job["jobId"],
            "parcelId": req.parcelId,
            "status": "queued",
            "estimatedDuration": ESTIMATED_DURATION,
        },
        message="PDF report job queued",
    )


@router.get(
    "/{parcelId}/pdf",
    responses={
        200: {"content": {"application/pdf": {}}, "description": "Binary PDF stream"},
        404: {"description": "Report not ready / parcel not found"},
    },
)
async def download_report(
    parcelId: str,
    download: bool = Query(True, description="Force download vs inline view"),
    version: str = Query("latest", description="Report version timestamp"),
):
    """Download the generated PDF report (§2.7.2)."""
    require_parcel(parcelId)
    if store.get_result(parcelId, MODULE) is None:
        raise HTTPException(
            status_code=404,
            detail=error_response(
                status_code=404,
                message="Report not found or still generating",
                errors=[{
                    "field": None,
                    "code": ErrorCode.REPORT_NOT_READY.value,
                    "message": "PDF generation is still in progress. Please check job status.",
                }],
            ),
        )

    pdf = build_minimal_pdf([
        "GeoSense AI - Site Intelligence Report",
        f"Parcel: {parcelId}",
        f"Version: {version}",
        "AI estimate for preliminary planning only.",
    ])
    date_str = datetime.now().strftime("%Y%m%d")
    disposition = "attachment" if download else "inline"
    filename = f"GeoSense_Report_{parcelId}_{date_str}.pdf"
    return Response(
        content=pdf,
        media_type="application/pdf",
        headers={"Content-Disposition": f'{disposition}; filename="{filename}"'},
    )
