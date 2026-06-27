using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Planora.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "notifications",
                newName: "Notifications");

            migrationBuilder.AlterColumn<string>(
                name: "type",
                table: "Notifications",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "title",
                table: "Notifications",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "message",
                table: "Notifications",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            //migrationBuilder.AlterColumn<string>(
            //    name: "data",
            //    table: "Notifications",
            //    type: "jsonb",
            //    nullable: true,
            //    oldClrType: typeof(string),
            //    oldType: "text",
            //    oldNullable: true);

            migrationBuilder.Sql(@"ALTER TABLE ""Notifications"" ALTER COLUMN data TYPE jsonb USING data::jsonb;");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId_IsRead_CreatedAt",
                table: "Notifications",
                columns: new[] { "user_id", "is_read", "created_at" },
                descending: new[] { false, false, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Notifications_UserId_IsRead_CreatedAt",
                table: "Notifications");

            migrationBuilder.RenameTable(
                name: "Notifications",
                newName: "notifications");

            migrationBuilder.AlterColumn<int>(
                name: "type",
                table: "notifications",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<string>(
                name: "title",
                table: "notifications",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256);

            migrationBuilder.AlterColumn<string>(
                name: "message",
                table: "notifications",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(2048)",
                oldMaxLength: 2048);

            //migrationBuilder.AlterColumn<string>(
            //    name: "data",
            //    table: "notifications",
            //    type: "text",
            //    nullable: true,
            //    oldClrType: typeof(string),
            //    oldType: "jsonb",
            //    oldNullable: true);

            migrationBuilder.Sql(@"ALTER TABLE ""notifications"" ALTER COLUMN data TYPE text;");
        }
    }
}
