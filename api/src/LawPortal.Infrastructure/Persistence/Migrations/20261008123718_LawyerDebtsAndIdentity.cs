using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LawPortal.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class LawyerDebtsAndIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LawyerShareBearer",
                table: "refunds",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DebtOffset",
                table: "payouts",
                type: "decimal(10,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastDebtReminderAtUtc",
                table: "lawyer_profiles",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NationalIdNumber",
                table: "lawyer_profiles",
                type: "varchar(10)",
                maxLength: 10,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<Guid>(
                name: "PossibleFormerProfileId",
                table: "lawyer_profiles",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<string>(
                name: "LicenseNumberKey",
                table: "lawyer_licenses",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "deleted_account_fingerprints",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    FormerUserId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    LawyerProfileId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    PhoneHash = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EmailHash = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NationalIdHash = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ContactEmail = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ContactPhone = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_deleted_account_fingerprints", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "lawyer_debt_entries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    LawyerProfileId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    PaymentId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    RefundId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    PayoutId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    Reference = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Note = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedByUserId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lawyer_debt_entries", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "refund_policy_settings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    RefundWindowDays = table.Column<int>(type: "int", nullable: false),
                    DebtReminderIntervalDays = table.Column<int>(type: "int", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_refund_policy_settings", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_lawyer_profiles_NationalIdNumber",
                table: "lawyer_profiles",
                column: "NationalIdNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_lawyer_licenses_LicenseNumberKey",
                table: "lawyer_licenses",
                column: "LicenseNumberKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_deleted_account_fingerprints_EmailHash",
                table: "deleted_account_fingerprints",
                column: "EmailHash");

            migrationBuilder.CreateIndex(
                name: "IX_deleted_account_fingerprints_NationalIdHash",
                table: "deleted_account_fingerprints",
                column: "NationalIdHash");

            migrationBuilder.CreateIndex(
                name: "IX_deleted_account_fingerprints_PhoneHash",
                table: "deleted_account_fingerprints",
                column: "PhoneHash");

            migrationBuilder.CreateIndex(
                name: "IX_lawyer_debt_entries_LawyerProfileId",
                table: "lawyer_debt_entries",
                column: "LawyerProfileId");

            // Backfill the normalised key (same rules as LicenseNumbers.Normalize). IGNORE leaves the
            // key empty instead of failing when two old records turn out to be the same licence.
            migrationBuilder.Sql("UPDATE IGNORE lawyer_licenses SET LicenseNumberKey = UPPER(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(LicenseNumber, '٠', '0'), '١', '1'), '٢', '2'), '٣', '3'), '٤', '4'), '٥', '5'), '٦', '6'), '٧', '7'), '٨', '8'), '٩', '9'), '۰', '0'), '۱', '1'), '۲', '2'), '۳', '3'), '۴', '4'), '۵', '5'), '۶', '6'), '۷', '7'), '۸', '8'), '۹', '9'), ' ', ''), '/', ''), '-', ''), '.', ''), '_', ''));");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "deleted_account_fingerprints");

            migrationBuilder.DropTable(
                name: "lawyer_debt_entries");

            migrationBuilder.DropTable(
                name: "refund_policy_settings");

            migrationBuilder.DropIndex(
                name: "IX_lawyer_profiles_NationalIdNumber",
                table: "lawyer_profiles");

            migrationBuilder.DropIndex(
                name: "IX_lawyer_licenses_LicenseNumberKey",
                table: "lawyer_licenses");

            migrationBuilder.DropColumn(
                name: "LawyerShareBearer",
                table: "refunds");

            migrationBuilder.DropColumn(
                name: "DebtOffset",
                table: "payouts");

            migrationBuilder.DropColumn(
                name: "LastDebtReminderAtUtc",
                table: "lawyer_profiles");

            migrationBuilder.DropColumn(
                name: "NationalIdNumber",
                table: "lawyer_profiles");

            migrationBuilder.DropColumn(
                name: "PossibleFormerProfileId",
                table: "lawyer_profiles");

            migrationBuilder.DropColumn(
                name: "LicenseNumberKey",
                table: "lawyer_licenses");
        }
    }
}
