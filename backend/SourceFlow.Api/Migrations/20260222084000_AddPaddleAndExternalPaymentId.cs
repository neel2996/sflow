using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SourceFlow.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPaddleAndExternalPaymentId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PaddlePriceId",
                table: "Plans",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaddleCustomerId",
                table: "Users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalPaymentId",
                table: "Payments",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaddlePriceId",
                table: "Plans");

            migrationBuilder.DropColumn(
                name: "PaddleCustomerId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ExternalPaymentId",
                table: "Payments");
        }
    }
}
