using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LawPortal.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RefundReasonCodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Keep the old free text as the note, then turn Reason into a code: the automatic refund
            // when a lawyer declines becomes LawyerDeclined (3), everything else Other (99).
            migrationBuilder.AddColumn<string>(
                name: "Details",
                table: "refunds",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.Sql("UPDATE refunds SET Details = Reason;");
            migrationBuilder.Sql("UPDATE refunds SET Reason = CASE WHEN Details LIKE 'Lawyer declined:%' THEN '3' ELSE '99' END;");

            migrationBuilder.AlterColumn<int>(
                name: "Reason",
                table: "refunds",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(500)",
                oldMaxLength: 500)
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Reason",
                table: "refunds",
                type: "varchar(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.Sql("UPDATE refunds SET Reason = COALESCE(Details, Reason);");

            migrationBuilder.DropColumn(
                name: "Details",
                table: "refunds");
        }
    }
}
