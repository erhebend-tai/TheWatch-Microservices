using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TheWatch.P6.FirstResponder.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CheckIns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ResponderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IncidentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Location_Latitude = table.Column<double>(type: "float", nullable: false),
                    Location_Longitude = table.Column<double>(type: "float", nullable: false),
                    Location_Accuracy = table.Column<double>(type: "float", nullable: true),
                    Location_Timestamp = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CheckIns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Responders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    BadgeNumber = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LastKnownLocation_Latitude = table.Column<double>(type: "float", nullable: true),
                    LastKnownLocation_Longitude = table.Column<double>(type: "float", nullable: true),
                    LastKnownLocation_Accuracy = table.Column<double>(type: "float", nullable: true),
                    LastKnownLocation_Timestamp = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LocationUpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Certifications = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MaxResponseRadiusKm = table.Column<double>(type: "float", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Responders", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CheckIns_IncidentId",
                table: "CheckIns",
                column: "IncidentId");

            migrationBuilder.CreateIndex(
                name: "IX_CheckIns_ResponderId",
                table: "CheckIns",
                column: "ResponderId");

            migrationBuilder.CreateIndex(
                name: "IX_CheckIns_Type",
                table: "CheckIns",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_Responders_CreatedAt",
                table: "Responders",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Responders_LocationUpdatedAt",
                table: "Responders",
                column: "LocationUpdatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Responders_Status",
                table: "Responders",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Responders_Type",
                table: "Responders",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_Responders_UpdatedAt",
                table: "Responders",
                column: "UpdatedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CheckIns");

            migrationBuilder.DropTable(
                name: "Responders");
        }
    }
}
