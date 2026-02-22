using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SourceFlow.Api.Migrations
{
    /// <inheritdoc />
    public partial class RazorpayOnlyRemoveStripe : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StripeCustomerId",
                table: "Users");

            migrationBuilder.AddColumn<string>(
                name: "RazorpayOrderId",
                table: "Payments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RazorpayPaymentId",
                table: "Payments",
                type: "text",
                nullable: true);

            migrationBuilder.Sql("UPDATE \"Plans\" SET \"Provider\" = 'razorpay' WHERE \"Provider\" = 'Stripe'");
            migrationBuilder.Sql("UPDATE \"Plans\" SET \"BillingType\" = 'one_time' WHERE \"BillingType\" = 'OneTime'");
            migrationBuilder.Sql("UPDATE \"Plans\" SET \"BillingType\" = 'subscription' WHERE \"BillingType\" = 'Monthly'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RazorpayOrderId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "RazorpayPaymentId",
                table: "Payments");

            migrationBuilder.AddColumn<string>(
                name: "StripeCustomerId",
                table: "Users",
                type: "text",
                nullable: true);
        }
    }
}
