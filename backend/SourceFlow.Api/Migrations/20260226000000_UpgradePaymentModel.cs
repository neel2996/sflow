using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SourceFlow.Api.Migrations
{
    /// <inheritdoc />
    public partial class UpgradePaymentModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PlanType",
                table: "Plans",
                type: "text",
                nullable: false,
                defaultValue: "credit_pack");

            migrationBuilder.AddColumn<int>(
                name: "DurationHours",
                table: "Plans",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsCustom",
                table: "Plans",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "UnlimitedAccessTill",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            // Seed unlimited and custom plans (idempotent)
            migrationBuilder.Sql(@"
                INSERT INTO ""Plans"" (""Name"", ""Price"", ""Currency"", ""Credits"", ""BillingType"", ""Provider"", ""PlanType"", ""DurationHours"", ""IsCustom"")
                SELECT '24h Unlimited', 149, 'INR', 0, 'one_time', 'razorpay', 'unlimited', 24, false
                WHERE NOT EXISTS (SELECT 1 FROM ""Plans"" WHERE ""PlanType"" = 'unlimited' AND ""DurationHours"" = 24);
            ");
            migrationBuilder.Sql(@"
                INSERT INTO ""Plans"" (""Name"", ""Price"", ""Currency"", ""Credits"", ""BillingType"", ""Provider"", ""PlanType"", ""DurationHours"", ""IsCustom"")
                SELECT '72h Unlimited', 299, 'INR', 0, 'one_time', 'razorpay', 'unlimited', 72, false
                WHERE NOT EXISTS (SELECT 1 FROM ""Plans"" WHERE ""PlanType"" = 'unlimited' AND ""DurationHours"" = 72);
            ");
            migrationBuilder.Sql(@"
                INSERT INTO ""Plans"" (""Name"", ""Price"", ""Currency"", ""Credits"", ""BillingType"", ""Provider"", ""PlanType"", ""DurationHours"", ""IsCustom"")
                SELECT 'Custom Credits', 0, 'INR', 0, 'one_time', 'razorpay', 'custom', NULL, true
                WHERE NOT EXISTS (SELECT 1 FROM ""Plans"" WHERE ""PlanType"" = 'custom');
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PlanType",
                table: "Plans");

            migrationBuilder.DropColumn(
                name: "DurationHours",
                table: "Plans");

            migrationBuilder.DropColumn(
                name: "IsCustom",
                table: "Plans");

            migrationBuilder.DropColumn(
                name: "UnlimitedAccessTill",
                table: "Users");
        }
    }
}
