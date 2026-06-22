using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Planora.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ExpandAnalysisResultEntitiesAndAddBorehole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "output_s3key",
                table: "ReportModules",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<long>(
                name: "file_size_bytes",
                table: "ReportModules",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "page_count",
                table: "ReportModules",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BoreholeResults",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    analysis_job_id = table.Column<Guid>(type: "uuid", nullable: false),
                    minimum_required = table.Column<int>(type: "integer", nullable: false),
                    optimal_count = table.Column<int>(type: "integer", nullable: false),
                    coverage_percentage = table.Column<double>(type: "double precision", nullable: false),
                    grid_size = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    placement_strategy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    placement_points_json = table.Column<string>(type: "jsonb", nullable: true),
                    placement_geo_json_url = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    traditional_borehole_count = table.Column<int>(type: "integer", nullable: false),
                    traditional_estimated_cost = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    optimized_borehole_count = table.Column<int>(type: "integer", nullable: false),
                    optimized_estimated_cost = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    savings_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    savings_percentage = table.Column<double>(type: "double precision", nullable: false),
                    currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_borehole_results", x => x.id);
                    table.ForeignKey(
                        name: "fk_borehole_results_analysis_jobs_analysis_job_id",
                        column: x => x.analysis_job_id,
                        principalTable: "AnalysisJobs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RiskResults",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    analysis_job_id = table.Column<Guid>(type: "uuid", nullable: false),
                    flood_risk_score = table.Column<int>(type: "integer", nullable: false),
                    seismic_risk_score = table.Column<int>(type: "integer", nullable: false),
                    expansive_soil_risk = table.Column<int>(type: "integer", nullable: false),
                    liquefaction_risk = table.Column<int>(type: "integer", nullable: false),
                    overall_risk_score = table.Column<int>(type: "integer", nullable: false),
                    overall_risk_level = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    flood_level = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    flood_factors_json = table.Column<string>(type: "jsonb", nullable: true),
                    flood_geo_json_url = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    seismic_level = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    seismic_factors_json = table.Column<string>(type: "jsonb", nullable: true),
                    seismic_source = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    expansive_soil_level = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    expansive_soil_factors_json = table.Column<string>(type: "jsonb", nullable: true),
                    replacement_depth = table.Column<double>(type: "double precision", nullable: true),
                    liquefaction_level = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    liquefaction_factors_json = table.Column<string>(type: "jsonb", nullable: true),
                    liquefaction_susceptibility = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_risk_results", x => x.id);
                    table.ForeignKey(
                        name: "fk_risk_results_analysis_jobs_analysis_job_id",
                        column: x => x.analysis_job_id,
                        principalTable: "AnalysisJobs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SoilResults",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    analysis_job_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sand_percent = table.Column<double>(type: "double precision", nullable: false),
                    silt_percent = table.Column<double>(type: "double precision", nullable: false),
                    clay_percent = table.Column<double>(type: "double precision", nullable: false),
                    bulk_density = table.Column<double>(type: "double precision", nullable: false),
                    organic_carbon = table.Column<double>(type: "double precision", nullable: false),
                    ph = table.Column<double>(type: "double precision", nullable: false),
                    bearing_capacity_estimate = table.Column<double>(type: "double precision", nullable: false),
                    bearing_capacity_category = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    composition_unit = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    bulk_density_unit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    organic_carbon_unit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    primary_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    usda_class = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ai_confidence = table.Column<double>(type: "double precision", nullable: true),
                    multi_depth_profile_json = table.Column<string>(type: "jsonb", nullable: true),
                    heatmap_tile_url = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_soil_results", x => x.id);
                    table.ForeignKey(
                        name: "fk_soil_results_analysis_jobs_analysis_job_id",
                        column: x => x.analysis_job_id,
                        principalTable: "AnalysisJobs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TopographyResults",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    analysis_job_id = table.Column<Guid>(type: "uuid", nullable: false),
                    elevation_min = table.Column<double>(type: "double precision", nullable: false),
                    elevation_max = table.Column<double>(type: "double precision", nullable: false),
                    elevation_mean = table.Column<double>(type: "double precision", nullable: false),
                    slope_distribution_json = table.Column<string>(type: "jsonb", nullable: false),
                    cut_volume = table.Column<double>(type: "double precision", nullable: false),
                    fill_volume = table.Column<double>(type: "double precision", nullable: false),
                    net_volume = table.Column<double>(type: "double precision", nullable: false),
                    contour_interval = table.Column<double>(type: "double precision", nullable: false),
                    contour_geo_json_url = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ponding_geo_json_url = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ponding_zones_count = table.Column<int>(type: "integer", nullable: true),
                    ponding_total_area = table.Column<double>(type: "double precision", nullable: true),
                    elevation_tile_url = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    slope_tile_url = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_topography_results", x => x.id);
                    table.ForeignKey(
                        name: "fk_topography_results_analysis_jobs_analysis_job_id",
                        column: x => x.analysis_job_id,
                        principalTable: "AnalysisJobs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_borehole_results_analysis_job_id",
                table: "BoreholeResults",
                column: "analysis_job_id");

            migrationBuilder.CreateIndex(
                name: "ix_risk_results_analysis_job_id",
                table: "RiskResults",
                column: "analysis_job_id");

            migrationBuilder.CreateIndex(
                name: "ix_soil_results_analysis_job_id",
                table: "SoilResults",
                column: "analysis_job_id");

            migrationBuilder.CreateIndex(
                name: "ix_topography_results_analysis_job_id",
                table: "TopographyResults",
                column: "analysis_job_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BoreholeResults");

            migrationBuilder.DropTable(
                name: "RiskResults");

            migrationBuilder.DropTable(
                name: "SoilResults");

            migrationBuilder.DropTable(
                name: "TopographyResults");

            migrationBuilder.DropColumn(
                name: "file_size_bytes",
                table: "ReportModules");

            migrationBuilder.DropColumn(
                name: "page_count",
                table: "ReportModules");

            migrationBuilder.AlterColumn<string>(
                name: "output_s3key",
                table: "ReportModules",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000,
                oldNullable: true);
        }
    }
}
