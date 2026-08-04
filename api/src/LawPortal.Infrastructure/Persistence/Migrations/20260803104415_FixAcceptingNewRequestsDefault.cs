using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LawPortal.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixAcceptingNewRequestsDefault : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<bool>(
                name: "AcceptingNewRequests",
                table: "lawyer_profiles",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "tinyint(1)");

            // The prior migration's ADD COLUMN used the CLR bool default (false), not this
            // entity's intended default (true) — an ALTER COLUMN default only affects future
            // inserts that omit the column, so every row added by that migration needs an
            // explicit backfill here, the same way P4 backfilled a similarly-missed VAT number.
            migrationBuilder.Sql("UPDATE lawyer_profiles SET AcceptingNewRequests = 1;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<bool>(
                name: "AcceptingNewRequests",
                table: "lawyer_profiles",
                type: "tinyint(1)",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "tinyint(1)",
                oldDefaultValue: true);
        }
    }
}
