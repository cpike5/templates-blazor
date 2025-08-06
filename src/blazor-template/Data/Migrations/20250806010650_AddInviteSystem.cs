using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlazorTemplate.Migrations
{
    /// <inheritdoc />
    public partial class AddInviteSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmailInvites",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    InviteToken = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsUsed = table.Column<bool>(type: "bit", nullable: false),
                    UsedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UsedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    EmailSentAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailInvites", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmailInvites_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmailInvites_AspNetUsers_UsedByUserId",
                        column: x => x.UsedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "InviteCodes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsUsed = table.Column<bool>(type: "bit", nullable: false),
                    UsedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UsedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InviteCodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InviteCodes_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InviteCodes_AspNetUsers_UsedByUserId",
                        column: x => x.UsedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmailInvites_CreatedAt",
                table: "EmailInvites",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_EmailInvites_CreatedByUserId",
                table: "EmailInvites",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailInvites_Email",
                table: "EmailInvites",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_EmailInvites_ExpiresAt",
                table: "EmailInvites",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_EmailInvites_InviteToken",
                table: "EmailInvites",
                column: "InviteToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmailInvites_IsUsed_ExpiresAt",
                table: "EmailInvites",
                columns: new[] { "IsUsed", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_EmailInvites_UsedByUserId",
                table: "EmailInvites",
                column: "UsedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InviteCodes_Code",
                table: "InviteCodes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InviteCodes_CreatedAt",
                table: "InviteCodes",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_InviteCodes_CreatedByUserId",
                table: "InviteCodes",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InviteCodes_ExpiresAt",
                table: "InviteCodes",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_InviteCodes_IsUsed_ExpiresAt",
                table: "InviteCodes",
                columns: new[] { "IsUsed", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_InviteCodes_UsedByUserId",
                table: "InviteCodes",
                column: "UsedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmailInvites");

            migrationBuilder.DropTable(
                name: "InviteCodes");
        }
    }
}
