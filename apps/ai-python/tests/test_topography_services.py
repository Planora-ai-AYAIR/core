"""
Day 2 tests — terrain derivatives + topography services.
All tests use synthetic rasters (no GEE calls).
"""

import json
import pytest
import numpy as np
import rasterio
from rasterio.transform import from_bounds
from unittest.mock import patch, MagicMock

from app.services.topography_service import (
    compute_elevation_stats,
    classify_slope,
    compute_cut_fill,
    detect_ponding_zones,
)
from app.services.tiles_service import export_png_tile, export_all_tiles


# ── Helper: write synthetic raster ───────────────────────────
def make_raster(path, data, nodata=None):
    h, w = data.shape
    with rasterio.open(
        path, "w", driver="GTiff",
        height=h, width=w, count=1,
        dtype="float32", crs="EPSG:32636",
        transform=from_bounds(0, 0, w * 30, h * 30, w, h),
        nodata=nodata,
    ) as dst:
        dst.write(data.astype(np.float32), 1)


# ══════════════════════════════════════════════════════════════
class TestElevationStats:

    def test_basic_stats(self, tmp_path):
        dem = str(tmp_path / "dem.tif")
        make_raster(dem, np.array([[10, 20, 30], [40, 50, 60]]))

        stats = compute_elevation_stats(dem)
        assert stats["min_m"]  == 10.0
        assert stats["max_m"]  == 60.0
        assert stats["mean_m"] == 35.0
        assert stats["std_m"]  > 0

    def test_nodata_excluded(self, tmp_path):
        dem  = str(tmp_path / "dem.tif")
        data = np.array([[10, -9999, 30], [40, 50, 60]], dtype=np.float32)
        make_raster(dem, data, nodata=-9999)

        stats = compute_elevation_stats(dem)
        assert stats["min_m"] == 10.0
        assert stats["max_m"] == 60.0

    def test_returns_all_keys(self, tmp_path):
        dem = str(tmp_path / "dem.tif")
        make_raster(dem, np.ones((3, 3)) * 100)

        stats = compute_elevation_stats(dem)
        for key in ["min_m", "max_m", "mean_m", "std_m"]:
            assert key in stats


# ══════════════════════════════════════════════════════════════
class TestSlopeClassification:

    def test_distribution_sums_to_100(self, tmp_path):
        slope = str(tmp_path / "slope.tif")
        data  = np.array([[1, 3, 8], [12, 18, 25]], dtype=np.float32)
        make_raster(slope, data)

        _, dist = classify_slope(slope)
        total = sum(d["pct_area"] for d in dist)
        assert abs(total - 100.0) < 1.0

    def test_four_categories_returned(self, tmp_path):
        slope = str(tmp_path / "slope.tif")
        make_raster(slope, np.array([[1, 3, 8, 20]], dtype=np.float32))

        _, dist = classify_slope(slope)
        assert len(dist) == 4
        categories = {d["category"] for d in dist}
        assert categories == {"flat", "gentle", "moderate", "steep"}

    def test_all_flat(self, tmp_path):
        slope = str(tmp_path / "slope.tif")
        make_raster(slope, np.ones((3, 3), dtype=np.float32) * 1.0)

        _, dist = classify_slope(slope)
        flat = next(d for d in dist if d["category"] == "flat")
        assert flat["pct_area"] == 100.0

    def test_has_color_field(self, tmp_path):
        slope = str(tmp_path / "slope.tif")
        make_raster(slope, np.array([[5, 10]], dtype=np.float32))

        _, dist = classify_slope(slope)
        for d in dist:
            assert "color" in d
            assert d["color"].startswith("#")


# ══════════════════════════════════════════════════════════════
class TestCutFill:

    def test_basic_cut_fill(self, tmp_path):
        dem = str(tmp_path / "dem.tif")
        make_raster(dem, np.array([[40, 50, 60], [70, 80, 90]], dtype=np.float32))

        result = compute_cut_fill(dem, reference_elevation=65.0)
        assert result["cut_m3"]  > 0
        assert result["fill_m3"] > 0
        assert result["reference_elev_m"] == 65.0
        assert result["method"] == "user_defined"

    def test_auto_reference_uses_mean(self, tmp_path):
        dem = str(tmp_path / "dem.tif")
        data = np.array([[10, 20], [30, 40]], dtype=np.float32)
        make_raster(dem, data)

        result = compute_cut_fill(dem)
        assert result["reference_elev_m"] == 25.0
        assert result["method"] == "mean_elevation"

    def test_flat_terrain_zero_volumes(self, tmp_path):
        dem = str(tmp_path / "dem.tif")
        make_raster(dem, np.ones((3, 3), dtype=np.float32) * 50.0)

        result = compute_cut_fill(dem, reference_elevation=50.0)
        assert result["cut_m3"]  == 0.0
        assert result["fill_m3"] == 0.0
        assert result["net_volume_m3"] == 0.0

    def test_returns_all_keys(self, tmp_path):
        dem = str(tmp_path / "dem.tif")
        make_raster(dem, np.array([[10, 20], [30, 40]], dtype=np.float32))

        result = compute_cut_fill(dem)
        for key in ["cut_m3","fill_m3","net_volume_m3",
                    "reference_elev_m","pixel_area_m2","method"]:
            assert key in result


# ══════════════════════════════════════════════════════════════
class TestPondingZones:

    def _make_twi_slope(self, tmp_path, twi_data, slope_data):
        twi_path   = str(tmp_path / "twi.tif")
        slope_path = str(tmp_path / "slope.tif")
        make_raster(twi_path,   twi_data)
        make_raster(slope_path, slope_data)
        return twi_path, slope_path

    def test_high_twi_low_slope_is_ponding(self, tmp_path):
        # All pixels: TWI=10 (>8), slope=1 (<2) → all ponding
        twi_p, slp_p = self._make_twi_slope(
            tmp_path,
            np.ones((3,3), dtype=np.float32) * 10.0,
            np.ones((3,3), dtype=np.float32) * 1.0,
        )
        result = detect_ponding_zones(twi_p, slp_p, str(tmp_path))
        assert result["zones_count"]   >= 1
        assert result["total_area_m2"] >  0

    def test_low_twi_no_ponding(self, tmp_path):
        # All pixels: TWI=2 (<8) → no ponding
        twi_p, slp_p = self._make_twi_slope(
            tmp_path,
            np.ones((3,3), dtype=np.float32) * 2.0,
            np.ones((3,3), dtype=np.float32) * 1.0,
        )
        result = detect_ponding_zones(twi_p, slp_p, str(tmp_path))
        assert result["zones_count"]   == 0
        assert result["total_area_m2"] == 0.0

    def test_geojson_file_created(self, tmp_path):
        twi_p, slp_p = self._make_twi_slope(
            tmp_path,
            np.ones((3,3), dtype=np.float32) * 10.0,
            np.ones((3,3), dtype=np.float32) * 1.0,
        )
        result = detect_ponding_zones(twi_p, slp_p, str(tmp_path))
        import os
        assert os.path.exists(result["geo_json_path"])

    def test_returns_all_keys(self, tmp_path):
        twi_p, slp_p = self._make_twi_slope(
            tmp_path,
            np.ones((2,2), dtype=np.float32) * 5.0,
            np.ones((2,2), dtype=np.float32) * 1.0,
        )
        result = detect_ponding_zones(twi_p, slp_p, str(tmp_path))
        for key in ["zones_count","total_area_m2",
                    "geo_json_path","twi_threshold"]:
            assert key in result


# ══════════════════════════════════════════════════════════════
class TestTilesService:

    def test_png_tile_created(self, tmp_path):
        dem_path = str(tmp_path / "dem.tif")
        out_path = str(tmp_path / "tiles" / "elevation" / "overview.png")
        make_raster(dem_path, np.random.rand(10, 10).astype(np.float32) * 100)

        result = export_png_tile(dem_path, out_path, "viridis")
        import os
        assert os.path.exists(result)

    def test_png_is_valid_image(self, tmp_path):
        from PIL import Image as PILImage
        dem_path = str(tmp_path / "dem.tif")
        out_path = str(tmp_path / "overview.png")
        make_raster(dem_path, np.random.rand(8, 8).astype(np.float32) * 100)

        export_png_tile(dem_path, out_path, "viridis")
        img = PILImage.open(out_path)
        assert img.size == (8, 8)
        assert img.mode == "RGB"

    def test_export_all_tiles_returns_two_keys(self, tmp_path):
        dem_path   = str(tmp_path / "dem.tif")
        slope_path = str(tmp_path / "slope.tif")
        make_raster(dem_path,   np.random.rand(6,6).astype(np.float32)*200)
        make_raster(slope_path, np.random.rand(6,6).astype(np.float32)*30)

        urls = export_all_tiles(dem_path, slope_path,
                                str(tmp_path), "parcel-001")
        assert "elevation" in urls
        assert "slope"     in urls


# ══════════════════════════════════════════════════════════════
class TestTerrainService:
    """Test terrain derivatives — WhiteboxTools mocked."""

    @patch("app.services.terrain_service._get_wbt")
    def test_returns_all_expected_keys(self, mock_get_wbt, tmp_path):
        mock_wbt = MagicMock()
        mock_get_wbt.return_value = mock_wbt

        # Make fake output files so the function doesn't fail on missing files
        import os
        dem = str(tmp_path / "dem.tif")
        make_raster(dem, np.ones((4,4), dtype=np.float32) * 100)

        keys = ["dem_filled","slope","aspect","curvature",
                "flow_accum","TWI","TRI"]
        for k in keys:
            open(str(tmp_path / f"{k}.tif"), "w").close()

        from app.services.terrain_service import compute_terrain_derivatives
        result = compute_terrain_derivatives(dem, str(tmp_path))

        assert set(result.keys()) == set(keys)

    @patch("app.services.terrain_service._get_wbt")
    def test_sink_fill_called_first(self, mock_get_wbt, tmp_path):
        """breach_depressions must be the first WhiteboxTools call."""
        mock_wbt = MagicMock()
        mock_get_wbt.return_value = mock_wbt

        dem = str(tmp_path / "dem.tif")
        make_raster(dem, np.ones((4,4), dtype=np.float32) * 100)

        for fname in ["dem_filled","slope","aspect","curvature",
                      "flow_accum","TWI","TRI"]:
            open(str(tmp_path / f"{fname}.tif"), "w").close()

        from app.services.terrain_service import compute_terrain_derivatives
        compute_terrain_derivatives(dem, str(tmp_path))

        first_call = mock_wbt.method_calls[0]
        assert first_call[0] == "breach_depressions"


if __name__ == "__main__":
    import pytest
    pytest.main([__file__, "-v", "--tb=short"])