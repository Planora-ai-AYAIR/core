using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Planora.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMissingContractFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "copernicus_dem_version",
                table: "TopographyResults",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "crs",
                table: "TopographyResults",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "dem_raster_url",
                table: "TopographyResults",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "pixel_resolution_meters",
                table: "TopographyResults",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "processing_time_seconds",
                table: "TopographyResults",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "slope_raster_url",
                table: "TopographyResults",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "bearing_confidence",
                table: "SoilResults",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "bearing_framework",
                table: "SoilResults",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "bearing_max_kpa",
                table: "SoilResults",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "bearing_min_kpa",
                table: "SoilResults",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "bearing_model_name",
                table: "SoilResults",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "bearing_range",
                table: "SoilResults",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "bearing_shap_enabled",
                table: "SoilResults",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "bearing_traffic_light",
                table: "SoilResults",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "bearing_training_r2",
                table: "SoilResults",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "bsi_mean",
                table: "SoilResults",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "cec",
                table: "SoilResults",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "data_sources_json",
                table: "SoilResults",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "depth_profile_image_url",
                table: "SoilResults",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "feature_importance_json",
                table: "SoilResults",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "floor_count_category",
                table: "SoilResults",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "max_floors_without_deep_foundation",
                table: "SoilResults",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "ndmi_mean",
                table: "SoilResults",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "ndvi_mean",
                table: "SoilResults",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "recommended_foundation",
                table: "SoilResults",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "soil_factors_json",
                table: "SoilResults",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "soil_type_geo_json_url",
                table: "SoilResults",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "water_table_depth_meters",
                table: "SoilResults",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "liquefaction_methodology",
                table: "RiskResults",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "mitigation_suggestions_json",
                table: "RiskResults",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "risk_heatmap_tile_url",
                table: "RiskResults",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "seismic_zone",
                table: "RiskResults",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "copernicus_dem_version",
                table: "TopographyResults");

            migrationBuilder.DropColumn(
                name: "crs",
                table: "TopographyResults");

            migrationBuilder.DropColumn(
                name: "dem_raster_url",
                table: "TopographyResults");

            migrationBuilder.DropColumn(
                name: "pixel_resolution_meters",
                table: "TopographyResults");

            migrationBuilder.DropColumn(
                name: "processing_time_seconds",
                table: "TopographyResults");

            migrationBuilder.DropColumn(
                name: "slope_raster_url",
                table: "TopographyResults");

            migrationBuilder.DropColumn(
                name: "bearing_confidence",
                table: "SoilResults");

            migrationBuilder.DropColumn(
                name: "bearing_framework",
                table: "SoilResults");

            migrationBuilder.DropColumn(
                name: "bearing_max_kpa",
                table: "SoilResults");

            migrationBuilder.DropColumn(
                name: "bearing_min_kpa",
                table: "SoilResults");

            migrationBuilder.DropColumn(
                name: "bearing_model_name",
                table: "SoilResults");

            migrationBuilder.DropColumn(
                name: "bearing_range",
                table: "SoilResults");

            migrationBuilder.DropColumn(
                name: "bearing_shap_enabled",
                table: "SoilResults");

            migrationBuilder.DropColumn(
                name: "bearing_traffic_light",
                table: "SoilResults");

            migrationBuilder.DropColumn(
                name: "bearing_training_r2",
                table: "SoilResults");

            migrationBuilder.DropColumn(
                name: "bsi_mean",
                table: "SoilResults");

            migrationBuilder.DropColumn(
                name: "cec",
                table: "SoilResults");

            migrationBuilder.DropColumn(
                name: "data_sources_json",
                table: "SoilResults");

            migrationBuilder.DropColumn(
                name: "depth_profile_image_url",
                table: "SoilResults");

            migrationBuilder.DropColumn(
                name: "feature_importance_json",
                table: "SoilResults");

            migrationBuilder.DropColumn(
                name: "floor_count_category",
                table: "SoilResults");

            migrationBuilder.DropColumn(
                name: "max_floors_without_deep_foundation",
                table: "SoilResults");

            migrationBuilder.DropColumn(
                name: "ndmi_mean",
                table: "SoilResults");

            migrationBuilder.DropColumn(
                name: "ndvi_mean",
                table: "SoilResults");

            migrationBuilder.DropColumn(
                name: "recommended_foundation",
                table: "SoilResults");

            migrationBuilder.DropColumn(
                name: "soil_factors_json",
                table: "SoilResults");

            migrationBuilder.DropColumn(
                name: "soil_type_geo_json_url",
                table: "SoilResults");

            migrationBuilder.DropColumn(
                name: "water_table_depth_meters",
                table: "SoilResults");

            migrationBuilder.DropColumn(
                name: "liquefaction_methodology",
                table: "RiskResults");

            migrationBuilder.DropColumn(
                name: "mitigation_suggestions_json",
                table: "RiskResults");

            migrationBuilder.DropColumn(
                name: "risk_heatmap_tile_url",
                table: "RiskResults");

            migrationBuilder.DropColumn(
                name: "seismic_zone",
                table: "RiskResults");
        }
    }
}
