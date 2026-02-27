using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TheWatch.P2.VoiceEmergency.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Dispatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IncidentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RadiusKm = table.Column<double>(type: "float", nullable: false),
                    RespondersRequested = table.Column<int>(type: "int", nullable: false),
                    RespondersNotified = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RespondersAccepted = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AcknowledgedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EscalationCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Dispatches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Incidents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Location_Latitude = table.Column<double>(type: "float", nullable: false),
                    Location_Longitude = table.Column<double>(type: "float", nullable: false),
                    Location_Accuracy = table.Column<double>(type: "float", nullable: true),
                    Location_Timestamp = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ReporterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReporterName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ReporterPhone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Severity = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Tags = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MediaUrls = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Incidents", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Dispatches_AcknowledgedAt",
                table: "Dispatches",
                column: "AcknowledgedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Dispatches_CreatedAt",
                table: "Dispatches",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Dispatches_IncidentId",
                table: "Dispatches",
                column: "IncidentId");

            migrationBuilder.CreateIndex(
                name: "IX_Dispatches_Status",
                table: "Dispatches",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Dispatches_UpdatedAt",
                table: "Dispatches",
                column: "UpdatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Incidents_CreatedAt",
                table: "Incidents",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Incidents_ReporterId",
                table: "Incidents",
                column: "ReporterId");

            migrationBuilder.CreateIndex(
                name: "IX_Incidents_ResolvedAt",
                table: "Incidents",
                column: "ResolvedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Incidents_Status",
                table: "Incidents",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Incidents_Type",
                table: "Incidents",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_Incidents_UpdatedAt",
                table: "Incidents",
                column: "UpdatedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Dispatches");

            migrationBuilder.DropTable(
                name: "Incidents");
        }
    }
}
