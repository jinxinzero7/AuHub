using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Auctions.Infrastructure.Data.Migrations
{
    public partial class AddLotDeliveryRequestDetails : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeliveryRequestDeadlineAt",
                table: "Lots",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeliveryRequestedAt",
                table: "Lots",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryRecipientName",
                table: "Lots",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryRecipientPhone",
                table: "Lots",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SelectedDeliveryProvider",
                table: "Lots",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeliveryRequestDeadlineAt",
                table: "Lots");

            migrationBuilder.DropColumn(
                name: "DeliveryRequestedAt",
                table: "Lots");

            migrationBuilder.DropColumn(
                name: "DeliveryRecipientName",
                table: "Lots");

            migrationBuilder.DropColumn(
                name: "DeliveryRecipientPhone",
                table: "Lots");

            migrationBuilder.DropColumn(
                name: "SelectedDeliveryProvider",
                table: "Lots");
        }
    }
}
