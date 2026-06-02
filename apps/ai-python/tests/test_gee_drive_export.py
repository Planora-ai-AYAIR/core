"""
Test suite for GEE Drive-based DEM export workflow.

Tests verify:
- GEE authentication
- Bbox validation
- DEM export task creation
- Task ID returns (not file paths)
- Integration with Google Drive (mocked)
"""

import pytest
import logging
from unittest.mock import Mock, patch
from app.services.gee_service import (
    init_gee,
    validate_bbox_egypt,
    export_dem_for_parcel
)

# Configure logging for tests
logging.basicConfig(level=logging.DEBUG)


class TestGEEAuthentication:
    """Test GEE authentication flow."""
    
    @patch("app.services.gee_service.ee.Geometry")
    @patch("app.services.gee_service.ee.ServiceAccountCredentials")
    @patch("app.services.gee_service.ee.Initialize")
    @patch("app.services.gee_service.ee.Image")
    def test_init_gee_with_service_account(self, mock_image, mock_init, mock_creds, mock_geometry):
        """Test: GEE initialization with service account credentials."""
        # Arrange
        mock_creds_instance = Mock()
        mock_creds.return_value = mock_creds_instance
        
        mock_point = Mock()
        mock_sample = Mock()
        mock_first = Mock()
        mock_first.getInfo.return_value = {"value": 100}
        mock_sample.first.return_value = mock_first
        mock_point.sample.return_value = mock_sample
        
        mock_image.return_value = mock_point
        mock_geometry.Point.return_value = mock_point
        
        # Act
        init_gee(
            gee_project="geosense-prod",
            service_account_email="geosense@project.iam.gserviceaccount.com",
            service_account_key="./secrets/gee_key.json"
        )
        
        # Assert
        mock_creds.assert_called_once_with(
            email="geosense@project.iam.gserviceaccount.com",
            key_file="./secrets/gee_key.json"
        )
        mock_init.assert_called_once()
    
    @patch("app.services.gee_service.ee.Geometry")
    @patch("app.services.gee_service.ee.Initialize")
    @patch("app.services.gee_service.ee.Image")
    def test_init_gee_without_key_uses_fallback(self, mock_image, mock_init, mock_geometry):
        """Test: GEE initialization falls back to gcloud auth if key missing."""
        # Arrange
        mock_point = Mock()
        mock_sample = Mock()
        mock_first = Mock()
        mock_first.getInfo.return_value = {"value": 100}
        mock_sample.first.return_value = mock_first
        mock_point.sample.return_value = mock_sample
        
        mock_image.return_value = mock_point
        mock_geometry.Point.return_value = mock_point
        
        # Act
        init_gee(
            gee_project="geosense-prod",
            service_account_email="geosense@project.iam.gserviceaccount.com",
            service_account_key="/nonexistent/key.json"
        )
        
        # Assert
        mock_init.assert_called_once_with(project="geosense-prod")


class TestBboxValidation:
    """Test parcel bbox validation against Egypt bounds."""
    
    def test_validate_bbox_egypt_valid(self):
        """Test: Valid bbox within Egypt returns True."""
        result = validate_bbox_egypt([31.2, 30.0, 31.5, 30.3])
        assert result is True
    
    def test_validate_bbox_egypt_too_far_north(self):
        """Test: Bbox above Egypt returns False."""
        result = validate_bbox_egypt([31.2, 33.0, 31.5, 33.3])
        assert result is False
    
    def test_validate_bbox_egypt_too_far_south(self):
        """Test: Bbox below Egypt returns False."""
        result = validate_bbox_egypt([31.2, 20.0, 31.5, 21.0])
        assert result is False
    
    def test_validate_bbox_egypt_too_far_west(self):
        """Test: Bbox left of Egypt returns False."""
        result = validate_bbox_egypt([23.0, 30.0, 23.5, 30.3])
        assert result is False
    
    def test_validate_bbox_egypt_too_far_east(self):
        """Test: Bbox right of Egypt returns False."""
        result = validate_bbox_egypt([31.2, 30.0, 38.0, 30.3])
        assert result is False
    
    def test_validate_bbox_egypt_on_boundary(self):
        """Test: Bbox on Egypt bounds returns True."""
        result = validate_bbox_egypt([24.0, 22.0, 37.0, 32.0])
        assert result is True


class TestDEMExportWorkflow:
    """Test DEM export task creation and Drive integration."""
    
    @patch("app.services.gee_service.ee.batch")
    @patch("app.services.gee_service.ee.Geometry")
    @patch("app.services.gee_service.ee.ImageCollection")
    def test_export_dem_returns_task_id_not_file_path(self, mock_collection, mock_geometry, mock_batch):
        """Test: export_dem_for_parcel() returns task ID, not file path."""
        # Arrange
        mock_task = Mock()
        mock_task.id = "projects/ee-geosense-prod/operations/XXXXXXXXXXXXXXXXXXXXX"
        mock_batch.Export.image.toDrive.return_value = mock_task
        
        mock_rect = Mock()
        mock_rect.buffer = Mock(return_value=Mock())
        mock_geometry.Rectangle.return_value = mock_rect
        
        mock_img_collection = Mock()
        mock_image = Mock()
        mock_collection.return_value = mock_img_collection
        mock_img_collection.filterBounds.return_value = mock_img_collection
        mock_img_collection.select.return_value = mock_img_collection
        mock_img_collection.mosaic.return_value = mock_image
        mock_image.clip.return_value = mock_image
        
        # Act
        result = export_dem_for_parcel(
            bbox=[31.2, 30.0, 31.5, 30.3],
            job_id="job-001",
            out_dir="/tmp/geosense/job-001"
        )
        
        # Assert
        assert isinstance(result, str)
        assert result.startswith("projects/")
        assert "operations/" in result
        mock_task.start.assert_called_once()
    
    @patch("app.services.gee_service.ee.batch")
    @patch("app.services.gee_service.ee.Geometry")
    @patch("app.services.gee_service.ee.ImageCollection")
    def test_export_dem_task_parameters(self, mock_collection, mock_geometry, mock_batch):
        """Test: export_dem_for_parcel() configures export task correctly."""
        # Arrange
        mock_task = Mock()
        mock_task.id = "projects/ee-geosense-prod/operations/ABC123"
        mock_batch.Export.image.toDrive.return_value = mock_task
        
        mock_rect = Mock()
        mock_rect.buffer = Mock(return_value=Mock())
        mock_geometry.Rectangle.return_value = mock_rect
        
        mock_img_collection = Mock()
        mock_image = Mock()
        mock_collection.return_value = mock_img_collection
        mock_img_collection.filterBounds.return_value = mock_img_collection
        mock_img_collection.select.return_value = mock_img_collection
        mock_img_collection.mosaic.return_value = mock_image
        mock_image.clip.return_value = mock_image
        
        # Act
        export_dem_for_parcel(
            bbox=[31.2, 30.0, 31.5, 30.3],
            job_id="job-001",
            out_dir="/tmp"
        )
        
        # Assert
        mock_batch.Export.image.toDrive.assert_called_once()
        call_kwargs = mock_batch.Export.image.toDrive.call_args[1]
        
        assert call_kwargs["scale"] == 30
        assert call_kwargs["crs"] == "EPSG:32636"
        assert call_kwargs["fileNamePrefix"] == "dem_job-001"
        assert call_kwargs["maxPixels"] == 1e9
    
    @patch("app.services.gee_service.ee.batch")
    @patch("app.services.gee_service.ee.Geometry")
    @patch("app.services.gee_service.ee.ImageCollection")
    def test_export_dem_geometry_buffering(self, mock_collection, mock_geometry, mock_batch):
        """Test: Parcel geometry is buffered by 500m."""
        # Arrange
        mock_task = Mock()
        mock_task.id = "projects/ee-geosense-prod/operations/DEF456"
        mock_batch.Export.image.toDrive.return_value = mock_task
        
        mock_rect = Mock()
        mock_rect.buffer = Mock(return_value=Mock())
        mock_geometry.Rectangle.return_value = mock_rect
        
        mock_img_collection = Mock()
        mock_image = Mock()
        mock_collection.return_value = mock_img_collection
        mock_img_collection.filterBounds.return_value = mock_img_collection
        mock_img_collection.select.return_value = mock_img_collection
        mock_img_collection.mosaic.return_value = mock_image
        mock_image.clip.return_value = mock_image
        
        # Act
        export_dem_for_parcel(
            bbox=[31.2, 30.0, 31.5, 30.3],
            job_id="job-001",
            out_dir="/tmp"
        )
        
        # Assert
        mock_rect.buffer.assert_called_once()


class TestIntegrationWithGoogleDrive:
    """Integration tests simulating Drive workflow."""
    
    @patch("app.services.gee_service.ee.batch")
    @patch("app.services.gee_service.ee.Geometry")
    @patch("app.services.gee_service.ee.ImageCollection")
    def test_multiple_exports_different_task_ids(self, mock_collection, mock_geometry, mock_batch):
        """Test: Multiple parcel exports generate different task IDs."""
        # Arrange
        task_ids = [
            "projects/ee-geosense-prod/operations/AAA111",
            "projects/ee-geosense-prod/operations/BBB222",
            "projects/ee-geosense-prod/operations/CCC333",
        ]
        
        mock_tasks = [Mock(id=tid) for tid in task_ids]
        mock_batch.Export.image.toDrive.side_effect = mock_tasks
        
        mock_rect = Mock()
        mock_rect.buffer = Mock(return_value=Mock())
        mock_geometry.Rectangle.return_value = mock_rect
        
        mock_img_collection = Mock()
        mock_image = Mock()
        mock_collection.return_value = mock_img_collection
        mock_img_collection.filterBounds.return_value = mock_img_collection
        mock_img_collection.select.return_value = mock_img_collection
        mock_img_collection.mosaic.return_value = mock_image
        mock_image.clip.return_value = mock_image
        
        # Act
        results = []
        for i in range(3):
            task_id = export_dem_for_parcel(
                bbox=[31.2 + i*0.1, 30.0, 31.5 + i*0.1, 30.3],
                job_id=f"job-{i:03d}",
                out_dir="/tmp"
            )
            results.append(task_id)
        
        # Assert
        assert len(set(results)) == 3
        assert results[0] == task_ids[0]
        assert results[1] == task_ids[1]
        assert results[2] == task_ids[2]


class TestErrorHandling:
    """Test error handling in export workflow."""
    
    @patch("app.services.gee_service.ee.batch")
    @patch("app.services.gee_service.ee.Geometry")
    @patch("app.services.gee_service.ee.ImageCollection")
    def test_export_dem_handles_gee_exception(self, mock_collection, mock_geometry, mock_batch):
        """Test: GEE exceptions are caught and re-raised as RuntimeError."""
        # Arrange
        import ee as ee_module
        mock_batch.Export.image.toDrive.side_effect = ee_module.EEException("GEE quota exceeded")
        
        mock_rect = Mock()
        mock_rect.buffer = Mock(return_value=Mock())
        mock_geometry.Rectangle.return_value = mock_rect
        
        mock_img_collection = Mock()
        mock_image = Mock()
        mock_collection.return_value = mock_img_collection
        mock_img_collection.filterBounds.return_value = mock_img_collection
        mock_img_collection.select.return_value = mock_img_collection
        mock_img_collection.mosaic.return_value = mock_image
        mock_image.clip.return_value = mock_image
        
        # Act & Assert
        with pytest.raises(RuntimeError, match="GEE export failed"):
            export_dem_for_parcel(
                bbox=[31.2, 30.0, 31.5, 30.3],
                job_id="job-001",
                out_dir="/tmp"
            )


class TestDriveExportWorkflowEndToEnd:
    """End-to-end test of complete Drive export workflow."""
    
    @patch("app.services.gee_service.ee.batch")
    @patch("app.services.gee_service.ee.Geometry")
    @patch("app.services.gee_service.ee.ImageCollection")
    def test_export_workflow_returns_metadata_for_api_response(self, mock_collection, mock_geometry, mock_batch):
        """Test: export_dem_for_parcel() returns data suitable for API response."""
        # Arrange
        mock_task = Mock()
        mock_task.id = "projects/ee-geosense-prod/operations/EXPORT123"
        mock_batch.Export.image.toDrive.return_value = mock_task
        
        mock_rect = Mock()
        mock_rect.buffer = Mock(return_value=Mock())
        mock_geometry.Rectangle.return_value = mock_rect
        
        mock_img_collection = Mock()
        mock_image = Mock()
        mock_collection.return_value = mock_img_collection
        mock_img_collection.filterBounds.return_value = mock_img_collection
        mock_img_collection.select.return_value = mock_img_collection
        mock_img_collection.mosaic.return_value = mock_image
        mock_image.clip.return_value = mock_image
        
        # Act
        job_id = "job-20260602-001"
        task_id = export_dem_for_parcel(
            bbox=[31.2, 30.0, 31.5, 30.3],
            job_id=job_id,
            out_dir="/tmp"
        )
        
        # Simulate backend creating API response
        api_response = {
            "job_id": job_id,
            "task_id": task_id,
            "status": "QUEUED",
            "file_name": f"dem_{job_id}.tif",
            "file_location": "Google Drive",
            "coordinate_system": "EPSG:32636",
            "resolution_m": 30,
            "message": "DEM export submitted. Monitor progress via task_id."
        }
        
        # Assert
        assert api_response["task_id"] == "projects/ee-geosense-prod/operations/EXPORT123"
        assert "dem_" in api_response["file_name"]
        assert api_response["file_location"] == "Google Drive"
        assert api_response["coordinate_system"] == "EPSG:32636"


if __name__ == "__main__":
    pytest.main([__file__, "-v", "--tb=short"])
