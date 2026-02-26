using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SourceFlow.Api.Migrations
{
    /// <inheritdoc />
    public partial class RemoveTestPlan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DELETE FROM ""Plans"" WHERE ""Price"" = 2 AND ""Currency"" = 'INR' AND ""Credits"" = 2 AND ""Name"" = 'Test (₹2)';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                INSERT INTO ""Plans"" (""Name"", ""Price"", ""Currency"", ""Credits"", ""BillingType"", ""Provider"", ""PlanType"")
                SELECT 'Test (₹2)', 2, 'INR', 2, 'one_time', 'razorpay', 'credit_pack'
                WHERE NOT EXISTS (SELECT 1 FROM ""Plans"" WHERE ""Price"" = 2 AND ""Currency"" = 'INR' AND ""Credits"" = 2);
            ");
        }
    }
}
