"""Job Status & Polling (client-facing) — API Contract §2.8.

GET /api/jobs/{jobId}/status — Check the status of any async job.
"""

from fastapi import APIRouter, HTTPException

from app.schemas.client import JobStatusData
from app.schemas.common import Envelope, ErrorCode, error_response, success_response
from app.services import store

router = APIRouter(prefix="/api/jobs", tags=["client: jobs"])


@router.get("/{jobId}/status", response_model=Envelope[JobStatusData])
async def get_job_status(jobId: str):
    """Check the processing status of any asynchronous job (§2.8.1)."""
    job = store.get_job(jobId)
    if job is None:
        raise HTTPException(
            status_code=404,
            detail=error_response(
                status_code=404,
                message="Job not found",
                errors=[{
                    "field": "jobId",
                    "code": ErrorCode.JOB_NOT_FOUND.value,
                    "message": f"No job found with id {jobId}",
                }],
            ),
        )

    return success_response(
        data={
            "jobId": job["jobId"],
            "parcelId": job["parcelId"],
            "module": job["module"],
            "status": job["status"],
            "progressPercentage": job.get("progressPercentage", 0),
            "startedAt": job.get("startedAt"),
            "completedAt": job.get("completedAt"),
            "nextModule": job.get("nextModule"),
            "message": job.get("message"),
        },
        message="Success",
    )
