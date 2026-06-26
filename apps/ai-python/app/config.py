"""
GeoSense configuration management.

Loads settings from .env file using Pydantic Settings.
Validates all required credentials on application startup.
"""

import logging
from functools import lru_cache
from pathlib import Path
from pydantic_settings import BaseSettings
from pydantic import Field, ConfigDict

logger = logging.getLogger(__name__)


class Settings(BaseSettings):
    """Application settings loaded from .env file."""

    model_config = ConfigDict(
        env_file=".env",
        env_file_encoding="utf-8",
        case_sensitive=False,
        extra="ignore"  
    )

    # Google Earth Engine Configuration
    gee_project: str = Field(
        default="",
        description="GEE project ID (e.g., 'geosense-prod')"
    )
    gee_service_account_email: str = Field(
        default="",
        description="Service account email from GEE console"
    )
    gee_service_account_key: str = Field(
        default="./secrets/gee_key.json",
        description="Path to service account JSON key file"
    )

    # Redis Configuration
    redis_url: str = Field(
        default="redis://localhost:6379",
        description="Redis connection URL for job caching"
    )

    # Output Configuration
    local_out_dir: str = Field(
        default="/tmp/geosense",
        description="Local directory for temporary files"
    )

    # Egypt Boundary Configuration
    egypt_bbox: list[float] = Field(
        default=[24.0, 22.0, 37.0, 32.0],
        description="Egypt bounds [minLon, minLat, maxLon, maxLat]"
    )

    # FastAPI Configuration
    api_title: str = Field(
        default="GeoSense API",
        description="API title for Swagger docs"
    )
    api_version: str = Field(
        default="0.1.0",
        description="API version"
    )

    # ── AWS / S3 Configuration ────────────────────────────────────────────
    # Credentials (AWS_ACCESS_KEY_ID + AWS_SECRET_ACCESS_KEY) are NOT stored
    # here. boto3 reads them automatically from environment variables injected
    # by GitHub Actions / server secrets. Never commit credentials.
    aws_region: str = Field(
        default="us-east-1",
        description="AWS region for S3 bucket"
    )
    aws_s3_bucket: str = Field(
        default="planora-dev-bucket",
        description="S3 bucket name (provisioned by DevOps)"
    )

    # ── Webhook Configuration ────────────────────────────────────────────
    webhook_url: str = Field(
        default="",
        description="URL to POST analysis-completed webhooks to"
    )
    shared_secret: str = Field(
        default="",
        description="HMAC-SHA256 shared secret for signing webhook payloads"
    )

    def validate_gee_credentials(self) -> bool:
        """
        Validate that GEE credentials are configured.

        Returns:
            True if credentials are present, False otherwise
        """
        if not self.gee_project:
            logger.warning("⚠️  GEE_PROJECT not configured in .env")
            return False

        if not self.gee_service_account_email:
            logger.warning("⚠️  GEE_SERVICE_ACCOUNT_EMAIL not configured in .env")
            return False

        key_path = Path(self.gee_service_account_key)
        if not key_path.exists():
            logger.warning(
                f"⚠️  GEE_SERVICE_ACCOUNT_KEY file not found: {self.gee_service_account_key}"
            )
            return False

        logger.info("✅ GEE credentials validated successfully")
        return True

    def validate_s3(self) -> bool:
        """
        Validate S3 connectivity at startup.

        Returns:
            True if bucket is reachable, False otherwise.
        """
        try:
            from app.services.s3_service import S3Service
            ok = S3Service(bucket=self.aws_s3_bucket, region=self.aws_region).ping()
            if ok:
                logger.info("✅ S3 bucket reachable: %s", self.aws_s3_bucket)
            else:
                logger.warning("⚠️  S3 ping failed — bucket=%s", self.aws_s3_bucket)
            return ok
        except Exception as exc:
            logger.warning("⚠️  S3 validation error: %s", exc)
            return False


# ── Singleton instances ───────────────────────────────────────────────────

# Global settings instance (used everywhere via `from app.config import settings`)
settings = Settings()


@lru_cache(maxsize=1)
def get_s3_service():
    """
    Return a cached S3Service instance.

    Usage in routers / services:
        from app.config import get_s3_service
        s3 = get_s3_service()
        url = s3.upload_geojson(data, parcel_id, "contours.geojson")
    """
    from app.services.s3_service import S3Service
    return S3Service(bucket=settings.aws_s3_bucket, region=settings.aws_region)
