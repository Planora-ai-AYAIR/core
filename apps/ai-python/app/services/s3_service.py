"""
s3_service.py — GeoSense AI / Planora
Handles all S3 uploads for visualization assets and map tiles.

Bucket layout (single bucket for dev, configurable for prod):
  {BUCKET}/tiles/{parcel_id}/{layer}/{z}/{x}/{y}.png   ← XYZ map tiles
  {BUCKET}/assets/{parcel_id}/contours.geojson
  {BUCKET}/assets/{parcel_id}/ponding_zones.geojson
  {BUCKET}/assets/{parcel_id}/flood_zones.geojson
  {BUCKET}/assets/{parcel_id}/soil_types.geojson
  {BUCKET}/assets/{parcel_id}/boreholes.geojson
  {BUCKET}/assets/{parcel_id}/dem.tif
  {BUCKET}/assets/{parcel_id}/slope_raster.tif
  {BUCKET}/assets/{parcel_id}/soil_depth_profile.png
"""

from __future__ import annotations

import json
import logging
from pathlib import Path
from typing import Any

import boto3
from botocore.exceptions import BotoCoreError, ClientError

logger = logging.getLogger(__name__)


class S3ServiceError(Exception):
    """Raised when any S3 operation fails."""


class S3Service:
    """
    Thin wrapper around boto3 for all GeoSense upload operations.

    Credentials are picked up automatically from environment variables:
        AWS_ACCESS_KEY_ID
        AWS_SECRET_ACCESS_KEY
        AWS_DEFAULT_REGION   (or AWS_REGION)

    The DevOps team injects these via GitHub Actions / server env.
    DO NOT hard-code credentials here.
    """

    def __init__(self, bucket: str, region: str = "us-east-1") -> None:
        self.bucket = bucket
        self.region = region
        # boto3 reads credentials from env automatically
        self._client = boto3.client("s3", region_name=region)
        logger.info("S3Service initialised — bucket=%s region=%s", bucket, region)

    # ------------------------------------------------------------------ #
    #  Tile uploads                                                        #
    # ------------------------------------------------------------------ #

    def upload_tile(
        self,
        data: bytes,
        parcel_id: str,
        layer: str,
        z: int,
        x: int,
        y: int,
    ) -> str:
        """
        Upload a single 256×256 PNG tile.

        Returns the s3:// URI string (e.g. s3://bucket/tiles/parcel/elev/12/1234/5678.png).
        The NestJS backend converts this to a presigned URL for the frontend.
        """
        key = f"tiles/{parcel_id}/{layer}/{z}/{x}/{y}.png"
        self._put(key, data, content_type="image/png")
        return f"s3://{self.bucket}/{key}"

    def tile_url_template(self, parcel_id: str, layer: str) -> str:
        """
        Return the XYZ tile URL template for a given parcel/layer.
        Example:  s3://planora-dev-bucket/tiles/parcel_xxx/elevation/{z}/{x}/{y}.png
        Frontend replaces {z}/{x}/{y} at render time.
        """
        return f"s3://{self.bucket}/tiles/{parcel_id}/{layer}/{{z}}/{{x}}/{{y}}.png"

    # ------------------------------------------------------------------ #
    #  Asset uploads (GeoJSON, rasters, images)                           #
    # ------------------------------------------------------------------ #

    def upload_geojson(
        self,
        geojson: dict[str, Any],
        parcel_id: str,
        filename: str,
    ) -> str:
        """Upload an in-memory GeoJSON dict. Returns s3:// URI."""
        key = f"assets/{parcel_id}/{filename}"
        body = json.dumps(geojson, ensure_ascii=False).encode("utf-8")
        self._put(key, body, content_type="application/geo+json")
        return f"s3://{self.bucket}/{key}"

    def upload_file(
        self,
        local_path: Path,
        parcel_id: str,
        filename: str,
        content_type: str = "application/octet-stream",
    ) -> str:
        """Upload a local file (raster, image, etc.). Returns s3:// URI."""
        key = f"assets/{parcel_id}/{filename}"
        with open(local_path, "rb") as fh:
            self._put(key, fh.read(), content_type=content_type)
        return f"s3://{self.bucket}/{key}"

    def upload_bytes(
        self,
        data: bytes,
        parcel_id: str,
        filename: str,
        content_type: str = "application/octet-stream",
    ) -> str:
        """Upload raw bytes as an asset. Returns s3:// URI."""
        key = f"assets/{parcel_id}/{filename}"
        self._put(key, data, content_type=content_type)
        return f"s3://{self.bucket}/{key}"

    # ------------------------------------------------------------------ #
    #  Presigned URLs (used internally if needed, but contract says       #
    #  NestJS backend generates presigned URLs, not us)                   #
    # ------------------------------------------------------------------ #

    def presigned_url(self, s3_uri: str, expiry: int = 3600) -> str:
        """
        Convert an s3:// URI → presigned HTTPS URL (1-hour expiry by default).
        Use only for debugging / internal checks.
        The NestJS backend is responsible for generating presigned URLs for the frontend.
        """
        key = s3_uri.removeprefix(f"s3://{self.bucket}/")
        return self._client.generate_presigned_url(
            "get_object",
            Params={"Bucket": self.bucket, "Key": key},
            ExpiresIn=expiry,
        )

    # ------------------------------------------------------------------ #
    #  Health check                                                        #
    # ------------------------------------------------------------------ #

    def ping(self) -> bool:
        """Return True if S3 bucket is reachable and writable."""
        try:
            test_key = "health/ping.txt"
            self._client.put_object(
                Bucket=self.bucket,
                Key=test_key,
                Body=b"ok",
                ContentType="text/plain",
            )
            self._client.delete_object(Bucket=self.bucket, Key=test_key)
            logger.info("S3 ping OK — bucket=%s", self.bucket)
            return True
        except (BotoCoreError, ClientError) as exc:
            logger.error("S3 ping FAILED — %s", exc)
            return False

    # ------------------------------------------------------------------ #
    #  Internal                                                            #
    # ------------------------------------------------------------------ #

    def _put(self, key: str, body: bytes, content_type: str) -> None:
        try:
            self._client.put_object(
                Bucket=self.bucket,
                Key=key,
                Body=body,
                ContentType=content_type,
            )
            logger.debug("S3 upload OK — s3://%s/%s", self.bucket, key)
        except (BotoCoreError, ClientError) as exc:
            logger.error("S3 upload FAILED — key=%s error=%s", key, exc)
            raise S3ServiceError(f"S3 upload failed for key '{key}': {exc}") from exc
