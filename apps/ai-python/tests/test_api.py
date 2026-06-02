"""
Test suite for GeoSense FastAPI application and endpoints.
Updated to match API Contract specification.
"""

import pytest
from fastapi.testclient import TestClient
from unittest.mock import patch, Mock
from app.main import app
from app.schemas.topography import (
    TopographyRequest,
    JobAccepted,
    JobProcessing,
)

# ── Shared test payload ───────────────────────────────────────
VALID_PAYLOAD = {
    "parcel_id": "test-parcel-001",
    "bbox":      [31.2, 30.0, 31.5, 30.3],
    "geo_json":  {
        "type": "Polygon",
        "coordinates": [[[31.2,30.0],[31.5,30.0],
                          [31.5,30.3],[31.2,30.3],
                          [31.2,30.0]]]
    }
}

INVALID_PAYLOAD = {
    "parcel_id": "test-parcel-outside",
    "bbox":      [10.0, 10.0, 15.0, 15.0],
    "geo_json":  {
        "type": "Polygon",
        "coordinates": [[[10.0,10.0],[15.0,10.0],
                          [15.0,15.0],[10.0,15.0],
                          [10.0,10.0]]]
    }
}

MOCK_TASK_ID = "projects/ee-geosense-prod/operations/ABC123"


@pytest.fixture
def client():
    """Pytest fixture for FastAPI test client."""
    return TestClient(app)


# ══════════════════════════════════════════════════════════════
class TestApplicationStartup:
    """Test FastAPI application initialization."""

    def test_app_created(self):
        """Test: FastAPI app is created with correct config."""
        assert app.title   == "GeoSense API"
        assert app.version == "0.1.0"

    def test_app_has_routes(self):
        """Test: All expected routes are registered."""
        route_paths = [route.path for route in app.routes]
        assert "/"                          in route_paths
        assert "/api/v1/health"             in route_paths
        assert "/api/v1/topography/jobs"    in route_paths


# ══════════════════════════════════════════════════════════════
class TestHealthCheckEndpoint:
    """Test /api/v1/health endpoint."""

    @patch("app.main.gee_initialized", True)
    @patch("app.main.redis_connected", False)
    def test_health_check_gee_ready(self, client):
        """Test: Health check returns correct GEE + Redis status."""
        response = client.get("/api/v1/health")

        assert response.status_code == 200
        data = response.json()
        assert data["status"]          == "healthy"
        assert data["gee_initialized"] == True
        assert data["redis_connected"] == False
        assert data["version"]         == "0.1.0"

    def test_health_check_structure(self, client):
        """Test: Health check response has all required fields."""
        response = client.get("/api/v1/health")

        assert response.status_code == 200
        data = response.json()
        assert "status"          in data
        assert "gee_initialized" in data
        assert "redis_connected" in data
        assert "version"         in data


# ══════════════════════════════════════════════════════════════
class TestRootEndpoint:
    """Test / endpoint."""

    def test_root_returns_info(self, client):
        """Test: Root endpoint returns API metadata."""
        response = client.get("/")

        assert response.status_code == 200
        data = response.json()
        assert data["title"]  == "GeoSense API"
        assert "version"      in data
        assert "docs"         in data
        assert "health"       in data


# ══════════════════════════════════════════════════════════════
class TestTopographyJobCreation:
    """Test POST /api/v1/topography/jobs."""

    @patch("app.routers.topography.validate_bbox_egypt")
    @patch("app.routers.topography.export_dem_for_parcel")
    def test_create_job_valid_bbox(self, mock_export, mock_validate, client):
        """Test: Valid request returns 202 with python_job_id."""
        mock_validate.return_value = True
        mock_export.return_value   = MOCK_TASK_ID

        response = client.post("/api/v1/topography/jobs", json=VALID_PAYLOAD)

        assert response.status_code == 202
        data = response.json()
        assert "python_job_id" in data
        assert data["parcel_id"] == "test-parcel-001"
        assert data["status"]    == "queued"
        assert "accepted_at"     in data

    @patch("app.routers.topography.validate_bbox_egypt")
    def test_create_job_invalid_bbox(self, mock_validate, client):
        """Test: bbox outside Egypt returns 400 with error contract."""
        mock_validate.return_value = False

        response = client.post("/api/v1/topography/jobs", json=INVALID_PAYLOAD)

        assert response.status_code == 400
        data = response.json()
        assert data["error_code"] == "INVALID_BBOX"
        assert data["retryable"]  == False
        assert "Egypt"            in data["message"]

    @patch("app.routers.topography.validate_bbox_egypt")
    @patch("app.routers.topography.export_dem_for_parcel")
    def test_create_job_response_schema(self, mock_export, mock_validate, client):
        """Test: Response is a valid JobAccepted schema."""
        mock_validate.return_value = True
        mock_export.return_value   = MOCK_TASK_ID

        response = client.post("/api/v1/topography/jobs", json=VALID_PAYLOAD)

        assert response.status_code == 202
        # Parse into Pydantic model — will raise if schema mismatch
        job = JobAccepted(**response.json())
        assert job.python_job_id is not None
        assert job.status        == "queued"
        assert job.parcel_id     == "test-parcel-001"

    @patch("app.routers.topography.validate_bbox_egypt")
    @patch("app.routers.topography.export_dem_for_parcel")
    def test_create_job_missing_parcel_id(self, mock_export, mock_validate, client):
        """Test: Missing parcel_id returns 422 validation error."""
        mock_validate.return_value = True
        mock_export.return_value   = MOCK_TASK_ID

        payload_missing_parcel = {
            "bbox":     [31.2, 30.0, 31.5, 30.3],
            "geo_json": VALID_PAYLOAD["geo_json"]
            # parcel_id missing intentionally
        }

        response = client.post("/api/v1/topography/jobs", json=payload_missing_parcel)
        assert response.status_code == 422


# ══════════════════════════════════════════════════════════════
class TestTopographyJobStatus:
    """Test GET /api/v1/topography/jobs/{python_job_id}."""

    @patch("app.routers.topography.validate_bbox_egypt")
    @patch("app.routers.topography.export_dem_for_parcel")
    def test_get_job_status_exists(self, mock_export, mock_validate, client):
        """Test: Polling existing job returns correct status."""
        mock_validate.return_value = True
        mock_export.return_value   = MOCK_TASK_ID

        # Create job first
        create_response = client.post(
            "/api/v1/topography/jobs", json=VALID_PAYLOAD
        )
        assert create_response.status_code == 202
        python_job_id = create_response.json()["python_job_id"]

        # Poll status
        status_response = client.get(
            f"/api/v1/topography/jobs/{python_job_id}"
        )

        assert status_response.status_code == 200
        data = status_response.json()
        assert data["python_job_id"] == python_job_id
        assert data["parcel_id"]     == "test-parcel-001"
        assert data["status"]        in ["queued", "processing", "completed", "failed"]
        assert "progress"            in data

    def test_get_job_status_not_found(self, client):
        """Test: Non-existent python_job_id returns 404 with error contract."""
        response = client.get(
            "/api/v1/topography/jobs/00000000-0000-0000-0000-000000000000"
        )

        assert response.status_code == 404
        data = response.json()
        assert data["error_code"] == "JOB_NOT_FOUND"
        assert data["retryable"]  == False

    @patch("app.routers.topography.validate_bbox_egypt")
    @patch("app.routers.topography.export_dem_for_parcel")
    def test_get_job_status_response_schema(self, mock_export, mock_validate, client):
        """Test: Polling response is a valid JobProcessing schema."""
        mock_validate.return_value = True
        mock_export.return_value   = MOCK_TASK_ID

        create_response = client.post(
            "/api/v1/topography/jobs", json=VALID_PAYLOAD
        )
        python_job_id = create_response.json()["python_job_id"]

        status_response = client.get(
            f"/api/v1/topography/jobs/{python_job_id}"
        )

        assert status_response.status_code == 200
        # Parse into Pydantic model
        job = JobProcessing(**status_response.json())
        assert job.python_job_id == python_job_id
        assert job.progress      >= 0


# ══════════════════════════════════════════════════════════════
class TestAPIDocumentation:
    """Test Swagger / OpenAPI endpoints."""

    def test_swagger_docs_available(self, client):
        """Test: Swagger UI is accessible."""
        response = client.get("/docs")
        assert response.status_code == 200

    def test_openapi_schema_available(self, client):
        """Test: OpenAPI JSON schema is accessible and valid."""
        response = client.get("/openapi.json")

        assert response.status_code == 200
        data = response.json()
        assert "openapi"                     in data
        assert "paths"                       in data
        assert "/api/v1/topography/jobs"     in data["paths"]
        assert "/api/v1/health"              in data["paths"]


if __name__ == "__main__":
    pytest.main([__file__, "-v", "--tb=short"])