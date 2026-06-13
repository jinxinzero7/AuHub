using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Auctions.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTrustScoreEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TrustScoreEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Subject = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Reason = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Points = table.Column<int>(type: "integer", nullable: false),
                    ReferenceType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ReferenceId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrustScoreEvents", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TrustScoreEvents_CreatedAt",
                table: "TrustScoreEvents",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TrustScoreEvents_UserId_Subject",
                table: "TrustScoreEvents",
                columns: new[] { "UserId", "Subject" });

            migrationBuilder.CreateIndex(
                name: "IX_TrustScoreEvents_UserId_Subject_Reason_ReferenceId",
                table: "TrustScoreEvents",
                columns: new[] { "UserId", "Subject", "Reason", "ReferenceId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TrustScoreEvents");
        }
    }
}
