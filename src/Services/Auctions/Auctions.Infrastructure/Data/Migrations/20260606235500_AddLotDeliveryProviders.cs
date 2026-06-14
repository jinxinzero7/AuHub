using Auctions.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Auctions.Infrastructure.Data.Migrations
{
    [DbContext(typeof(AuctionsDbContext))]
    [Migration("20260606235500_AddLotDeliveryProviders")]
    public partial class AddLotDeliveryProviders : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SupportedDeliveryProviders",
                table: "Lots",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "RussianPost");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SupportedDeliveryProviders",
                table: "Lots");
        }
    }
}
