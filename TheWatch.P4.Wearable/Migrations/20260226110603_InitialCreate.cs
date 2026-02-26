using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TheWatch.P4.Wearable.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HeartbeatReadings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeviceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Bpm = table.Column<int>(type: "int", nullable: false),
                    StepCount = table.Column<int>(type: "int", nullable: true),
                    CaloriesBurned = table.Column<int>(type: "int", nullable: true),
                    RecordedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HeartbeatReadings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SyncJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeviceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Direction = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RecordsProcessed = table.Column<int>(type: "int", nullable: false),
                    Success = table.Column<bool>(type: "bit", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncJobs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WearableDevices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Platform = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Model = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    FirmwareVersion = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    OwnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    BatteryPercent = table.Column<int>(type: "int", nullable: true),
                    LastSyncAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WearableDevices", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HeartbeatReadings_DeviceId",
                table: "HeartbeatReadings",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_HeartbeatReadings_RecordedAt",
                table: "HeartbeatReadings",
                column: "RecordedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SyncJobs_CompletedAt",
                table: "SyncJobs",
                column: "CompletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SyncJobs_DeviceId",
                table: "SyncJobs",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_SyncJobs_Direction",
                table: "SyncJobs",
                column: "Direction");

            migrationBuilder.CreateIndex(
                name: "IX_SyncJobs_StartedAt",
                table: "SyncJobs",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_WearableDevices_CreatedAt",
                table: "WearableDevices",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_WearableDevices_LastSyncAt",
                table: "WearableDevices",
                column: "LastSyncAt");

            migrationBuilder.CreateIndex(
                name: "IX_WearableDevices_OwnerId",
                table: "WearableDevices",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_WearableDevices_Platform",
                table: "WearableDevices",
                column: "Platform");

            migrationBuilder.CreateIndex(
                name: "IX_WearableDevices_Status",
                table: "WearableDevices",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HeartbeatReadings");

            migrationBuilder.DropTable(
                name: "SyncJobs");

            migrationBuilder.DropTable(
                name: "WearableDevices");
        }
    }
}
