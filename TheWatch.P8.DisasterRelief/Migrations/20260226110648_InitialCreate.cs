using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TheWatch.P8.DisasterRelief.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DisasterEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Location_Latitude = table.Column<double>(type: "float", nullable: false),
                    Location_Longitude = table.Column<double>(type: "float", nullable: false),
                    RadiusKm = table.Column<double>(type: "float", nullable: false),
                    Severity = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DisasterEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EvacuationRoutes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DisasterEventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Origin_Latitude = table.Column<double>(type: "float", nullable: false),
                    Origin_Longitude = table.Column<double>(type: "float", nullable: false),
                    Destination_Latitude = table.Column<double>(type: "float", nullable: false),
                    Destination_Longitude = table.Column<double>(type: "float", nullable: false),
                    DistanceKm = table.Column<double>(type: "float", nullable: false),
                    EstimatedTimeMinutes = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvacuationRoutes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ResourceItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Location_Latitude = table.Column<double>(type: "float", nullable: false),
                    Location_Longitude = table.Column<double>(type: "float", nullable: false),
                    DonorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DisasterEventId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResourceItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ResourceRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequesterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    Priority = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Location_Latitude = table.Column<double>(type: "float", nullable: false),
                    Location_Longitude = table.Column<double>(type: "float", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MatchedResourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DisasterEventId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResourceRequests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Shelters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Location_Latitude = table.Column<double>(type: "float", nullable: false),
                    Location_Longitude = table.Column<double>(type: "float", nullable: false),
                    Capacity = table.Column<int>(type: "int", nullable: false),
                    CurrentOccupancy = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Amenities = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContactPhone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DisasterEventId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Shelters", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DisasterEvents_CreatedAt",
                table: "DisasterEvents",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_DisasterEvents_Status",
                table: "DisasterEvents",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_DisasterEvents_Type",
                table: "DisasterEvents",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_DisasterEvents_UpdatedAt",
                table: "DisasterEvents",
                column: "UpdatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_EvacuationRoutes_CreatedAt",
                table: "EvacuationRoutes",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_EvacuationRoutes_DisasterEventId",
                table: "EvacuationRoutes",
                column: "DisasterEventId");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceItems_Category",
                table: "ResourceItems",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceItems_CreatedAt",
                table: "ResourceItems",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceItems_DisasterEventId",
                table: "ResourceItems",
                column: "DisasterEventId");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceItems_DonorId",
                table: "ResourceItems",
                column: "DonorId");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceItems_Status",
                table: "ResourceItems",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceRequests_Category",
                table: "ResourceRequests",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceRequests_CreatedAt",
                table: "ResourceRequests",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceRequests_DisasterEventId",
                table: "ResourceRequests",
                column: "DisasterEventId");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceRequests_MatchedResourceId",
                table: "ResourceRequests",
                column: "MatchedResourceId");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceRequests_Priority",
                table: "ResourceRequests",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceRequests_RequesterId",
                table: "ResourceRequests",
                column: "RequesterId");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceRequests_Status",
                table: "ResourceRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Shelters_CreatedAt",
                table: "Shelters",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Shelters_DisasterEventId",
                table: "Shelters",
                column: "DisasterEventId");

            migrationBuilder.CreateIndex(
                name: "IX_Shelters_Status",
                table: "Shelters",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Shelters_UpdatedAt",
                table: "Shelters",
                column: "UpdatedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DisasterEvents");

            migrationBuilder.DropTable(
                name: "EvacuationRoutes");

            migrationBuilder.DropTable(
                name: "ResourceItems");

            migrationBuilder.DropTable(
                name: "ResourceRequests");

            migrationBuilder.DropTable(
                name: "Shelters");
        }
    }
}
