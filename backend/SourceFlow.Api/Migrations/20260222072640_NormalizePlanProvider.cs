using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SourceFlow.Api.Migrations
{
    /// <inheritdoc />
    public partial class NormalizePlanProvider : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE \"Plans\" SET \"Provider\" = 'razorpay' WHERE \"Provider\" = 'Razorpay'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
