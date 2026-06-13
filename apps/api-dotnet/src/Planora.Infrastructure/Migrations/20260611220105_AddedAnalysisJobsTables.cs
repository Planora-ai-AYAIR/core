using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Planora.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddedAnalysisJobsTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AnalysisJobs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    parcel_id = table.Column<Guid>(type: "uuid", nullable: false),
                    python_job_id = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    error_message = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_analysis_jobs", x => x.id);
                    table.ForeignKey(
                        name: "fk_analysis_jobs_parcels_parcel_id",
                        column: x => x.parcel_id,
                        principalTable: "Parcels",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_analysis_jobs_parcel_id",
                table: "AnalysisJobs",
                column: "parcel_id");

            migrationBuilder.CreateIndex(
                name: "ix_analysis_jobs_python_job_id",
                table: "AnalysisJobs",
                column: "python_job_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_analysis_jobs_status",
                table: "AnalysisJobs",
                column: "status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AnalysisJobs");
        }
    }
}
