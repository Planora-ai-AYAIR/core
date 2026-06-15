"""Report module schemas — API Contract §3.5."""

from __future__ import annotations
from typing import Optional
from pydantic import BaseModel

from app.schemas.common import JobStatus


class ReportOptions(BaseModel):
    companyName: Optional[str] = None
    projectName: Optional[str] = None
    language: str = "en"


# ── POST /api/v1/reports/jobs — Request (§3.5.1) ─────────────
class ReportJobRequest(BaseModel):
    jobId: str
    parcelId: str
    moduleResults: dict = {}
    reportOptions: ReportOptions = ReportOptions()


# ── Result sub-models ────────────────────────────────────────
class ReportResults(BaseModel):
    pdfUrl: Optional[str] = None
    pageCount: int = 0
    sizeBytes: int = 0


# ── GET response data (§3.5.2) ───────────────────────────────
class ReportJobData(BaseModel):
    pythonJobId: str
    status: JobStatus
    results: Optional[ReportResults] = None
