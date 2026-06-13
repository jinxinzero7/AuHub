using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Identity.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentVerificationRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DocumentVerificationStatus",
                table: "Users",
                type: "text",
                nullable: false,
                defaultValue: "Unverified");

            migrationBuilder.AddColumn<DateTime>(
                name: "DocumentVerifiedAt",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DocumentVerificationRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PassportImagePath = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    SelfieImagePath = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    ReviewedByAdminId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RejectionReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentVerificationRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentVerificationRequests_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentVerificationRequests_CreatedAt",
                table: "DocumentVerificationRequests",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentVerificationRequests_Status",
                table: "DocumentVerificationRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentVerificationRequests_UserId",
                table: "DocumentVerificationRequests",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DocumentVerificationRequests");

            migrationBuilder.DropColumn(
                name: "DocumentVerificationStatus",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DocumentVerifiedAt",
                table: "Users");
        }
    }
}
