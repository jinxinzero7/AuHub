using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Auctions.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBidIdempotencyKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "IdempotencyKey",
                table: "Bids",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Bids_IdempotencyKey",
                table: "Bids",
                column: "IdempotencyKey",
                unique: true,
                filter: "\"IdempotencyKey\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Bids_IdempotencyKey",
                table: "Bids");

            migrationBuilder.DropColumn(
                name: "IdempotencyKey",
                table: "Bids");
        }
    }
}
