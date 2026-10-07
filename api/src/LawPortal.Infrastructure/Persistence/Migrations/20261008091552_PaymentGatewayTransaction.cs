using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LawPortal.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PaymentGatewayTransaction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GwAuthorizationCode",
                table: "payments",
                type: "varchar(30)",
                maxLength: 30,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "GwCardBrand",
                table: "payments",
                type: "varchar(30)",
                maxLength: 30,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "GwCardMasked",
                table: "payments",
                type: "varchar(30)",
                maxLength: 30,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<decimal>(
                name: "GwFee",
                table: "payments",
                type: "decimal(10,2)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "GwFetchedAtUtc",
                table: "payments",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GwMessage",
                table: "payments",
                type: "varchar(200)",
                maxLength: 200,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "GwReferenceNumber",
                table: "payments",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "GwResponseCode",
                table: "payments",
                type: "varchar(20)",
                maxLength: 20,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "GwSourceType",
                table: "payments",
                type: "varchar(30)",
                maxLength: 30,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "GwTransactionId",
                table: "payments",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GwAuthorizationCode",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "GwCardBrand",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "GwCardMasked",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "GwFee",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "GwFetchedAtUtc",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "GwMessage",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "GwReferenceNumber",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "GwResponseCode",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "GwSourceType",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "GwTransactionId",
                table: "payments");
        }
    }
}
