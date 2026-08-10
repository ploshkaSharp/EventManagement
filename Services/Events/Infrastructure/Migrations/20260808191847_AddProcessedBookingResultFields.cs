using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventManagement.Events.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProcessedBookingResultFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AvailableSeats",
                table: "ProcessedBookings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "FailureReason",
                table: "ProcessedBookings",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Success",
                table: "ProcessedBookings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_ProcessedBookings_EventId",
                table: "ProcessedBookings",
                column: "EventId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProcessedBookings_EventId",
                table: "ProcessedBookings");

            migrationBuilder.DropColumn(
                name: "AvailableSeats",
                table: "ProcessedBookings");

            migrationBuilder.DropColumn(
                name: "FailureReason",
                table: "ProcessedBookings");

            migrationBuilder.DropColumn(
                name: "Success",
                table: "ProcessedBookings");
        }
    }
}
