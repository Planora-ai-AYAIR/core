"""
GeoSense FastAPI application.
"""

import logging
import os
from contextlib import asynccontextmanager

from fastapi import FastAPI, Request, HTTPException
from fastapi.middleware.cors import CORSMiddleware
from fastapi.responses import JSONResponse
from fastapi.staticfiles import StaticFiles

from app.config import settings
from app.services.gee_service import init_gee
from app.routers import topography, analyze, soil, risks, boreholes, reports
from app.routers.analysis import router as analysis_router
from app.routers.client import (
    parcels as client_parcels,
    topography as client_topography,
    soil as client_soil,
    bearing as client_bearing,
    risks as client_risks,
    boreholes as client_boreholes,
    reports as client_reports,
    jobs as client_jobs,
)

logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s - %(name)s - %(levelname)s - %(message)s"
)
logger = logging.getLogger(__name__)

gee_initialized = False
redis_connected = False
s3_connected    = False          # ← NEW


# ── Lifespan ──────────────────────────────────────────────────
@asynccontextmanager
async def lifespan(app: FastAPI):
    logger.info("🚀 Starting GeoSense API...")
    global gee_initialized, redis_connected, s3_connected

    # ── GEE ───────────────────────────────────────────────────
    try:
        init_gee(
            gee_project           = settings.gee_project,
            service_account_email = settings.gee_service_account_email,
            service_account_key   = settings.gee_service_account_key,
        )
        gee_initialized = True
        logger.info("✅ GEE initialized")
    except Exception as e:
        gee_initialized = False
        logger.error(f"❌ GEE init failed: {e}")

    # ── Redis ──────────────────────────────────────────────────
    redis_connected = False
    logger.info("ℹ️  Redis skipped — will be added Day 2")

    # ── S3 ────────────────────────────────────────────────────  ← NEW
    try:
        s3_connected = settings.validate_s3()
        if s3_connected:
            logger.info("✅ S3 ready — bucket=%s", settings.aws_s3_bucket)
        else:
            logger.warning("⚠️  S3 not reachable at startup — check AWS credentials")
    except Exception as e:
        s3_connected = False
        logger.error("❌ S3 init failed: %s", e)

    yield

    logger.info("🛑 Shutting down...")


# ── App ───────────────────────────────────────────────────────
app = FastAPI(
    title       = settings.api_title,
    description = "GeoSense AI — Internal AI Engine API (Python FastAPI)",
    version     = settings.api_version,
    lifespan    = lifespan,
)

app.add_middleware(
    CORSMiddleware,
    allow_origins      = ["*"],
    allow_credentials  = True,
    allow_methods      = ["*"],
    allow_headers      = ["*"],
)

# ── Internal AI Engine routers (API Contract §3) ─────────────
app.include_router(topography.router)
app.include_router(soil.router)
app.include_router(risks.router)
app.include_router(boreholes.router)
app.include_router(reports.router)

# ── Client-Facing API routers (API Contract §2) ──────────────
app.include_router(client_parcels.router)
app.include_router(client_topography.router)
app.include_router(client_soil.router)
app.include_router(client_bearing.router)
app.include_router(client_risks.router)
app.include_router(client_boreholes.router)
app.include_router(client_reports.router)
app.include_router(client_jobs.router)

# ── Debug / demo router ──────────────────────────────────────
app.include_router(analyze.router)
app.include_router(analysis_router)

# ── Static UI ─────────────────────────────────────────────────
_STATIC_DIR = os.path.join(os.path.dirname(__file__), "static")
app.mount("/ui", StaticFiles(directory=_STATIC_DIR, html=True), name="ui")


# ── Health ────────────────────────────────────────────────────
@app.get("/api/v1/health")
async def health_check():
    return {
        "statusCode": 200,
        "message": "Success",
        "errors": None,
        "data": {
            "status":          "healthy" if (gee_initialized and s3_connected) else "degraded",
            "gee_initialized": gee_initialized,
            "redis_connected": redis_connected,
            "s3_connected":    s3_connected,    # ← NEW
            "s3_bucket":       settings.aws_s3_bucket,  # ← NEW
            "version":         settings.api_version,
        },
    }


# ── Root ──────────────────────────────────────────────────────
@app.get("/")
async def root():
    return {
        "statusCode": 200,
        "message": "GeoSense AI Engine API",
        "errors": None,
        "data": {
            "title":   settings.api_title,
            "version": settings.api_version,
            "ui":      "/ui",
            "docs":    "/docs",
            "health":  "/api/v1/health",
        },
    }


# ── Error Handlers ────────────────────────────────────────────
@app.exception_handler(HTTPException)
async def http_exception_handler(request: Request, exc: HTTPException):
    return JSONResponse(
        status_code = exc.status_code,
        content     = exc.detail,
    )


@app.exception_handler(Exception)
async def global_exception_handler(request: Request, exc: Exception):
    logger.error(f"Unhandled error: {exc}", exc_info=True)
    return JSONResponse(
        status_code = 500,
        content = {
            "statusCode": 500,
            "message":    "An unexpected error occurred",
            "errors": [{
                "field":   None,
                "code":    "INTERNAL_ERROR",
                "message": str(exc),
            }],
            "data": None,
        }
    )

# feature/module1-terrain-soil-contourZones
# py -3.13 -m uvicorn app.main:app --reload --port 8000
if __name__ == "__main__":
    import uvicorn
    uvicorn.run(
        "app.main:app",
        host      = "0.0.0.0",
        port      = 8000,
        reload    = True,
        log_level = "info",
    )