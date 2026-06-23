using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Planora.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAnalysisOptionsToAnalysisJob : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "options_contour_interval",
                table: "AnalysisJobs",
                type: "numeric(10,4)",
                precision: 10,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "options_include_bearing",
                table: "AnalysisJobs",
                type: "boolean",
                nullable: true,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "options_include_borehole",
                table: "AnalysisJobs",
                type: "boolean",
                nullable: true,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "options_include_risk",
                table: "AnalysisJobs",
                type: "boolean",
                nullable: true,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "options_include_soil",
                table: "AnalysisJobs",
                type: "boolean",
                nullable: true,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "options_include_topography",
                table: "AnalysisJobs",
                type: "boolean",
                nullable: true,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "options_reference_plane",
                table: "AnalysisJobs",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "options_slope_categories",
                table: "AnalysisJobs",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "options_soil_depths",
                table: "AnalysisJobs",
                type: "jsonb",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "options_contour_interval",
                table: "AnalysisJobs");

            migrationBuilder.DropColumn(
                name: "options_include_bearing",
                table: "AnalysisJobs");

            migrationBuilder.DropColumn(
                name: "options_include_borehole",
                table: "AnalysisJobs");

            migrationBuilder.DropColumn(
                name: "options_include_risk",
                table: "AnalysisJobs");

            migrationBuilder.DropColumn(
                name: "options_include_soil",
                table: "AnalysisJobs");

            migrationBuilder.DropColumn(
                name: "options_include_topography",
                table: "AnalysisJobs");

            migrationBuilder.DropColumn(
                name: "options_reference_plane",
                table: "AnalysisJobs");

            migrationBuilder.DropColumn(
                name: "options_slope_categories",
                table: "AnalysisJobs");

            migrationBuilder.DropColumn(
                name: "options_soil_depths",
                table: "AnalysisJobs");
        }
    }
}
