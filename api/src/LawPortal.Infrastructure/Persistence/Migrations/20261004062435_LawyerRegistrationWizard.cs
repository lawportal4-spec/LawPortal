using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LawPortal.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class LawyerRegistrationWizard : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CountryCode",
                table: "lawyer_profiles",
                type: "varchar(2)",
                maxLength: 2,
                nullable: false,
                defaultValue: "SA")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "TermsAcceptedAtUtc",
                table: "lawyer_profiles",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DocumentContentType",
                table: "lawyer_licenses",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "DocumentFileName",
                table: "lawyer_licenses",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "DocumentStorageKey",
                table: "lawyer_licenses",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "LicenseType",
                table: "lawyer_licenses",
                type: "int",
                nullable: false,
                defaultValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CountryCode",
                table: "lawyer_profiles");

            migrationBuilder.DropColumn(
                name: "TermsAcceptedAtUtc",
                table: "lawyer_profiles");

            migrationBuilder.DropColumn(
                name: "DocumentContentType",
                table: "lawyer_licenses");

            migrationBuilder.DropColumn(
                name: "DocumentFileName",
                table: "lawyer_licenses");

            migrationBuilder.DropColumn(
                name: "DocumentStorageKey",
                table: "lawyer_licenses");

            migrationBuilder.DropColumn(
                name: "LicenseType",
                table: "lawyer_licenses");
        }
    }
}
