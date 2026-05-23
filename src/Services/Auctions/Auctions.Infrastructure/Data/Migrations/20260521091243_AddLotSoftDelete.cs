using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Auctions.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLotSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Lots",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedBy",
                table: "Lots",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Lots",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Lots");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "Lots");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Lots");
        }
    }
}
