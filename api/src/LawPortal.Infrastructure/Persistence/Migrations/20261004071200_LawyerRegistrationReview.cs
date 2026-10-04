using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LawPortal.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class LawyerRegistrationReview : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CorrectionIssues",
                table: "lawyer_licenses",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "CorrectionNote",
                table: "lawyer_licenses",
                type: "varchar(1000)",
                maxLength: 1000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "CorrectionRequestedAtUtc",
                table: "lawyer_licenses",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ResubmittedAtUtc",
                table: "lawyer_licenses",
                type: "datetime(6)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CorrectionIssues",
                table: "lawyer_licenses");

            migrationBuilder.DropColumn(
                name: "CorrectionNote",
                table: "lawyer_licenses");

            migrationBuilder.DropColumn(
                name: "CorrectionRequestedAtUtc",
                table: "lawyer_licenses");

            migrationBuilder.DropColumn(
                name: "ResubmittedAtUtc",
                table: "lawyer_licenses");
        }
    }
}
