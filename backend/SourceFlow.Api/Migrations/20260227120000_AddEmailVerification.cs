using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using SourceFlow.Api.Data;

#nullable disable

namespace SourceFlow.Api.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AppDbContext))]
    [Migration("20260227120000_AddEmailVerification")]
    public partial class AddEmailVerification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EmailVerificationTokenHash",
                table: "Users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EmailVerificationExpiry",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EmailVerificationSentAt",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsEmailVerified",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmailVerificationTokenHash",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "EmailVerificationExpiry",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "EmailVerificationSentAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsEmailVerified",
                table: "Users");
        }
    }
}
