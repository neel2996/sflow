using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SourceFlow.Api.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUnlimitedPlans : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DELETE FROM ""Plans"" WHERE ""PlanType"" = 'unlimited';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
        }
    }
}
