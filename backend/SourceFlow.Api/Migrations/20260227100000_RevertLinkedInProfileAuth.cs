using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SourceFlow.Api.Migrations
{
    /// <inheritdoc />
    public partial class RevertLinkedInProfileAuth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Idempotent: only run if LinkedIn schema exists
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Users' AND column_name = 'LinkedInProfileUrl') THEN
                        DROP INDEX IF EXISTS ""IX_Users_LinkedInProfileUrl"";
                        ALTER TABLE ""Users"" DROP COLUMN ""LinkedInProfileUrl"";
                        ALTER TABLE ""Users"" DROP COLUMN ""DailyScansUsed"";
                        ALTER TABLE ""Users"" DROP COLUMN ""LastScanDate"";
                        ALTER TABLE ""Users"" ADD COLUMN IF NOT EXISTS ""Email"" text NOT NULL DEFAULT '';
                        ALTER TABLE ""Users"" ADD COLUMN IF NOT EXISTS ""PasswordHash"" text NOT NULL DEFAULT '';
                        ALTER TABLE ""Users"" ADD COLUMN IF NOT EXISTS ""PasswordResetToken"" text;
                        ALTER TABLE ""Users"" ADD COLUMN IF NOT EXISTS ""PasswordResetExpiry"" timestamp with time zone;
                        UPDATE ""Users"" SET ""Email"" = 'legacy@sourceflow.local' WHERE ""Email"" = '';
                        ALTER TABLE ""Users"" ALTER COLUMN ""Email"" DROP DEFAULT;
                        CREATE UNIQUE INDEX IF NOT EXISTS ""IX_Users_Email"" ON ""Users"" (""Email"");
                    END IF;
                END $$;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_Users_Email", table: "Users");
            migrationBuilder.DropColumn(name: "Email", table: "Users");
            migrationBuilder.DropColumn(name: "PasswordHash", table: "Users");
            migrationBuilder.DropColumn(name: "PasswordResetToken", table: "Users");
            migrationBuilder.DropColumn(name: "PasswordResetExpiry", table: "Users");
            migrationBuilder.AddColumn<string>(name: "LinkedInProfileUrl", table: "Users", type: "text", nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<int>(name: "DailyScansUsed", table: "Users", type: "integer", nullable: false, defaultValue: 0);
            migrationBuilder.AddColumn<DateTime>(name: "LastScanDate", table: "Users", type: "timestamp with time zone", nullable: false, defaultValue: DateTime.UtcNow.Date);
            migrationBuilder.CreateIndex(name: "IX_Users_LinkedInProfileUrl", table: "Users", column: "LinkedInProfileUrl", unique: true);
        }
    }
}
