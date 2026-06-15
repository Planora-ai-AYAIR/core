"""
GeoSense FastAPI application.
"""

import logging
import os
from contextlib import asynccontextmanager

from fastapi import FastAPI, Request, HTTPException
from fastapi.middleware.cors import CORSMiddleware
from fastapi.responses import JSONResponse, FileResponse
from fastapi.staticfiles import StaticFiles

from app.config import settings
from app.services.gee_service import init_gee
from app.routers import topography, analyze, soil, risks, boreholes, reports

logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s - %(name)s - %(levelname)s - %(message)s"
)
logger = logging.getLogger(__name__)

gee_initialized  = False
redis_connected  = False


# ── Lifespan ──────────────────────────────────────────────────
@asynccontextmanager
async def lifespan(app: FastAPI):
    logger.info("🚀 Starting GeoSense API...")
    global gee_initialized, redis_connected

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

    redis_connected = False
    logger.info("ℹ️  Redis skipped — will be added Day 2")

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

# ── Module routers (API Contract §3) ─────────────────────────
app.include_router(topography.router)
app.include_router(soil.router)
app.include_router(risks.router)
app.include_router(boreholes.router)
app.include_router(reports.router)

# ── Debug / demo router ──────────────────────────────────────
app.include_router(analyze.router)

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
            "status":          "healthy" if gee_initialized else "degraded",
            "gee_initialized": gee_initialized,
            "redis_connected": redis_connected,
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
    """
    Handles HTTPException raised from routers (400, 404, etc.)
    Returns the detail dict directly — already formatted per API contract.
    """
    return JSONResponse(
        status_code = exc.status_code,
        content     = exc.detail,
    )


@app.exception_handler(Exception)
async def global_exception_handler(request: Request, exc: Exception):
    """
    Catches any unhandled exception — returns unified error format (§1.2).
    """
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
# ── Run ───────────────────────────────────────────────────────
if __name__ == "__main__":
    import uvicorn
    uvicorn.run(
        "app.main:app",
        host      = "0.0.0.0",
        port      = 8000,
        reload    = True,
        log_level = "info",
    )