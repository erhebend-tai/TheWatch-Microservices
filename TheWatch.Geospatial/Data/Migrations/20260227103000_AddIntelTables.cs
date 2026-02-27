using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace TheWatch.Geospatial.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddIntelTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IntelEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Summary = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    SourceType = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    SourceUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    SourceName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Location = table.Column<Point>(type: "geometry (Point, 4326)", nullable: false),
                    RadiusMeters = table.Column<double>(type: "double precision", nullable: false),
                    Category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ThreatLevel = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ConfidenceScore = table.Column<double>(type: "double precision", nullable: false),
                    Tags = table.Column<Dictionary<string, string>>(type: "jsonb", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    PublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IngestedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntelEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IntelInferences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Location = table.Column<Point>(type: "geometry (Point, 4326)", nullable: false),
                    RadiusMeters = table.Column<double>(type: "double precision", nullable: false),
                    Category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ThreatLevel = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Summary = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ConfidenceScore = table.Column<double>(type: "double precision", nullable: false),
                    SupportingEntryCount = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    GeneratedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntelInferences", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IntelEntries_Category",
                table: "IntelEntries",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_IntelEntries_ExpiresAt",
                table: "IntelEntries",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_IntelEntries_IngestedAt",
                table: "IntelEntries",
                column: "IngestedAt");

            migrationBuilder.CreateIndex(
                name: "IX_IntelEntries_Location",
                table: "IntelEntries",
                column: "Location")
                .Annotation("Npgsql:IndexMethod", "gist");

            migrationBuilder.CreateIndex(
                name: "IX_IntelEntries_PublishedAt",
                table: "IntelEntries",
                column: "PublishedAt");

            migrationBuilder.CreateIndex(
                name: "IX_IntelEntries_ThreatLevel",
                table: "IntelEntries",
                column: "ThreatLevel");

            migrationBuilder.CreateIndex(
                name: "IX_IntelInferences_Category",
                table: "IntelInferences",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_IntelInferences_ExpiresAt",
                table: "IntelInferences",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_IntelInferences_GeneratedAt",
                table: "IntelInferences",
                column: "GeneratedAt");

            migrationBuilder.CreateIndex(
                name: "IX_IntelInferences_Location",
                table: "IntelInferences",
                column: "Location")
                .Annotation("Npgsql:IndexMethod", "gist");

            migrationBuilder.CreateIndex(
                name: "IX_IntelInferences_ThreatLevel",
                table: "IntelInferences",
                column: "ThreatLevel");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "IntelEntries");
            migrationBuilder.DropTable(name: "IntelInferences");
        }
    }
}
