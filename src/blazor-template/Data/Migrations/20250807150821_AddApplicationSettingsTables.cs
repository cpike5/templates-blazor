using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlazorTemplate.Migrations
{
    /// <inheritdoc />
    public partial class AddApplicationSettingsTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApplicationSettings",
                columns: table => new
                {
                    Key = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsEncrypted = table.Column<bool>(type: "bit", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationSettings", x => x.Key);
                    table.ForeignKey(
                        name: "FK_ApplicationSettings_AspNetUsers_ModifiedBy",
                        column: x => x.ModifiedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "SettingsAuditLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SettingsKey = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    OldValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SettingsAuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SettingsAuditLogs_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "SystemHealthMetrics",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MetricType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MetricValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemHealthMetrics", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationSettings_Category",
                table: "ApplicationSettings",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationSettings_Category_Key",
                table: "ApplicationSettings",
                columns: new[] { "Category", "Key" });

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationSettings_LastModified",
                table: "ApplicationSettings",
                column: "LastModified");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationSettings_ModifiedBy",
                table: "ApplicationSettings",
                column: "ModifiedBy");

            migrationBuilder.CreateIndex(
                name: "IX_SettingsAuditLogs_SettingsKey",
                table: "SettingsAuditLogs",
                column: "SettingsKey");

            migrationBuilder.CreateIndex(
                name: "IX_SettingsAuditLogs_SettingsKey_Timestamp",
                table: "SettingsAuditLogs",
                columns: new[] { "SettingsKey", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_SettingsAuditLogs_Timestamp",
                table: "SettingsAuditLogs",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_SettingsAuditLogs_UserId",
                table: "SettingsAuditLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SystemHealthMetrics_MetricType",
                table: "SystemHealthMetrics",
                column: "MetricType");

            migrationBuilder.CreateIndex(
                name: "IX_SystemHealthMetrics_MetricType_Timestamp",
                table: "SystemHealthMetrics",
                columns: new[] { "MetricType", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_SystemHealthMetrics_Timestamp",
                table: "SystemHealthMetrics",
                column: "Timestamp");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApplicationSettings");

            migrationBuilder.DropTable(
                name: "SettingsAuditLogs");

            migrationBuilder.DropTable(
                name: "SystemHealthMetrics");
        }
    }
}
