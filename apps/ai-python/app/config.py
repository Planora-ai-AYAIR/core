"""
GeoSense configuration management.

Loads settings from .env file using Pydantic Settings.
Validates all required credentials on application startup.
"""

import logging
from pathlib import Path
from pydantic_settings import BaseSettings
from pydantic import Field, ConfigDict

logger = logging.getLogger(__name__)


class Settings(BaseSettings):
    """Application settings loaded from .env file."""
    
    model_config = ConfigDict(
        env_file=".env",
        env_file_encoding="utf-8",
        case_sensitive=False
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


# Create global settings instance
settings = Settings()
