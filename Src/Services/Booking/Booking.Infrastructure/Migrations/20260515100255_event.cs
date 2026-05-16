using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Booking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class @event : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Price",
                table: "Seats",
                newName: "PriceAmount");

            migrationBuilder.AddColumn<string>(
                name: "PriceCurrency",
                table: "Seats",
                type: "nvarchar(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PaymentStatus",
                table: "Reservations",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Pending");

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Reservations",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<string>(
                name: "StripePaymentIntentId",
                table: "Reservations",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_PaymentStatus",
                table: "Reservations",
                column: "PaymentStatus");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_StripePaymentIntentId",
                table: "Reservations",
                column: "StripePaymentIntentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Reservations_PaymentStatus",
                table: "Reservations");

            migrationBuilder.DropIndex(
                name: "IX_Reservations_StripePaymentIntentId",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "PriceCurrency",
                table: "Seats");

            migrationBuilder.DropColumn(
                name: "PaymentStatus",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "StripePaymentIntentId",
                table: "Reservations");

            migrationBuilder.RenameColumn(
                name: "PriceAmount",
                table: "Seats",
                newName: "Price");
        }
    }
}
