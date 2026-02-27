using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TheWatch.P3.MeshNetwork.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MeshMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SenderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecipientId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ChannelId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Content = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Priority = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    HopCount = table.Column<int>(type: "int", nullable: false),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeshMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MeshNodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DeviceId = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Latitude = table.Column<double>(type: "float", nullable: true),
                    Longitude = table.Column<double>(type: "float", nullable: true),
                    BatteryPercent = table.Column<int>(type: "int", nullable: true),
                    RelayCount = table.Column<int>(type: "int", nullable: false),
                    ConnectedPeers = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastSeenAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeshNodes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NotificationChannels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SubscriberIds = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationChannels", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MeshMessages_ChannelId",
                table: "MeshMessages",
                column: "ChannelId");

            migrationBuilder.CreateIndex(
                name: "IX_MeshMessages_Priority",
                table: "MeshMessages",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_MeshMessages_RecipientId",
                table: "MeshMessages",
                column: "RecipientId");

            migrationBuilder.CreateIndex(
                name: "IX_MeshMessages_SenderId",
                table: "MeshMessages",
                column: "SenderId");

            migrationBuilder.CreateIndex(
                name: "IX_MeshMessages_SentAt",
                table: "MeshMessages",
                column: "SentAt");

            migrationBuilder.CreateIndex(
                name: "IX_MeshNodes_CreatedAt",
                table: "MeshNodes",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_MeshNodes_LastSeenAt",
                table: "MeshNodes",
                column: "LastSeenAt");

            migrationBuilder.CreateIndex(
                name: "IX_MeshNodes_Status",
                table: "MeshNodes",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationChannels_CreatedAt",
                table: "NotificationChannels",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationChannels_Type",
                table: "NotificationChannels",
                column: "Type");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MeshMessages");

            migrationBuilder.DropTable(
                name: "MeshNodes");

            migrationBuilder.DropTable(
                name: "NotificationChannels");
        }
    }
}
