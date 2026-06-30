using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Planora.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SplitBearingIntoOwnModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "bearing_capacity_category",
                table: "SoilResults");

            migrationBuilder.DropColumn(
                name: "bearing_capacity_estimate",
                table: "SoilResults");

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
                name: "feature_importance_json",
                table: "SoilResults");

            migrationBuilder.DropColumn(
                name: "floor_count_category",
                table: "SoilResults");

            migrationBuilder.DropColumn(
                name: "max_floors_without_deep_foundation",
                table: "SoilResults");

            migrationBuilder.DropColumn(
                name: "recommended_foundation",
                table: "SoilResults");

            migrationBuilder.DropColumn(
                name: "soil_factors_json",
                table: "SoilResults");

            migrationBuilder.CreateTable(
                name: "BearingResults",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    analysis_job_id = table.Column<Guid>(type: "uuid", nullable: false),
                    bearing_capacity_kpa = table.Column<double>(type: "double precision", nullable: false),
                    classification = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    confidence = table.Column<double>(type: "double precision", nullable: true),
                    range = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    traffic_light = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    recommended_foundation = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    max_floors_without_deep_foundation = table.Column<int>(type: "integer", nullable: true),
                    floor_count_category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    min_kpa = table.Column<double>(type: "double precision", nullable: true),
                    max_kpa = table.Column<double>(type: "double precision", nullable: true),
                    feature_importance_json = table.Column<string>(type: "jsonb", nullable: true),
                    soil_factors_json = table.Column<string>(type: "jsonb", nullable: true),
                    model_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    framework = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    training_r2 = table.Column<double>(type: "double precision", nullable: true),
                    shap_enabled = table.Column<bool>(type: "boolean", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bearing_results", x => x.id);
                    table.ForeignKey(
                        name: "fk_bearing_results_analysis_jobs_analysis_job_id",
                        column: x => x.analysis_job_id,
                        principalTable: "AnalysisJobs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_bearing_results_analysis_job_id",
                table: "BearingResults",
                column: "analysis_job_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BearingResults");

            migrationBuilder.AddColumn<string>(
                name: "bearing_capacity_category",
                table: "SoilResults",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<double>(
                name: "bearing_capacity_estimate",
                table: "SoilResults",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

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
        }
    }
}
