using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TheWatch.P11.Surveillance.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CameraRegistrations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Latitude = table.Column<double>(type: "float", nullable: false),
                    Longitude = table.Column<double>(type: "float", nullable: false),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CoverageRadiusMeters = table.Column<double>(type: "float", nullable: false),
                    Heading = table.Column<double>(type: "float", nullable: true),
                    CameraModel = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    StreamUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    IsPublic = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Source = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    VerifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CameraRegistrations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CrimeLocations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Latitude = table.Column<double>(type: "float", nullable: false),
                    Longitude = table.Column<double>(type: "float", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    CrimeType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ReporterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrimeLocations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FootageSubmissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CameraId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubmitterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GpsLatitude = table.Column<double>(type: "float", nullable: false),
                    GpsLongitude = table.Column<double>(type: "float", nullable: false),
                    GpsVerified = table.Column<bool>(type: "bit", nullable: false),
                    StartTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MediaUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    ThumbnailUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    DurationSeconds = table.Column<double>(type: "float", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Tags = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AnalysisCompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FootageSubmissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DetectionResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FootageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DetectionType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Label = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Confidence = table.Column<float>(type: "real", nullable: false),
                    BoundingBoxX = table.Column<float>(type: "real", nullable: false),
                    BoundingBoxY = table.Column<float>(type: "real", nullable: false),
                    BoundingBoxW = table.Column<float>(type: "real", nullable: false),
                    BoundingBoxH = table.Column<float>(type: "real", nullable: false),
                    FrameTimestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModelVersion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Metadata = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DetectionResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DetectionResults_FootageSubmissions_FootageId",
                        column: x => x.FootageId,
                        principalTable: "FootageSubmissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CameraRegistrations_CreatedAt",
                table: "CameraRegistrations",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_CameraRegistrations_OwnerId",
                table: "CameraRegistrations",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_CameraRegistrations_Status",
                table: "CameraRegistrations",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_CameraRegistrations_UpdatedAt",
                table: "CameraRegistrations",
                column: "UpdatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_CrimeLocations_CreatedAt",
                table: "CrimeLocations",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_CrimeLocations_IsActive",
                table: "CrimeLocations",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_CrimeLocations_ReporterId",
                table: "CrimeLocations",
                column: "ReporterId");

            migrationBuilder.CreateIndex(
                name: "IX_DetectionResults_CreatedAt",
                table: "DetectionResults",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_DetectionResults_DetectionType",
                table: "DetectionResults",
                column: "DetectionType");

            migrationBuilder.CreateIndex(
                name: "IX_DetectionResults_FootageId",
                table: "DetectionResults",
                column: "FootageId");

            migrationBuilder.CreateIndex(
                name: "IX_FootageSubmissions_CameraId",
                table: "FootageSubmissions",
                column: "CameraId");

            migrationBuilder.CreateIndex(
                name: "IX_FootageSubmissions_CreatedAt",
                table: "FootageSubmissions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_FootageSubmissions_Status",
                table: "FootageSubmissions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_FootageSubmissions_SubmitterId",
                table: "FootageSubmissions",
                column: "SubmitterId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "DetectionResults");
            migrationBuilder.DropTable(name: "FootageSubmissions");
            migrationBuilder.DropTable(name: "CrimeLocations");
            migrationBuilder.DropTable(name: "CameraRegistrations");
        }
    }
}
