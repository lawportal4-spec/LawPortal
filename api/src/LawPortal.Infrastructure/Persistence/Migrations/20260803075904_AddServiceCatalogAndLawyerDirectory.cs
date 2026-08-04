using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LawPortal.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceCatalogAndLawyerDirectory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AvgRating",
                table: "lawyer_profiles",
                type: "decimal(3,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CompletedRequestCount",
                table: "lawyer_profiles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "lawyer_profiles",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "lawyer_profiles",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                table: "lawyer_profiles",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "lawyer_profiles",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "ExperienceRangeId",
                table: "lawyer_profiles",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "lawyer_profiles",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "RatingCount",
                table: "lawyer_profiles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAtUtc",
                table: "lawyer_profiles",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "lawyer_profiles",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "lawyer_languages",
                columns: table => new
                {
                    LawyerProfileId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    LanguageId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lawyer_languages", x => new { x.LawyerProfileId, x.LanguageId });
                    table.ForeignKey(
                        name: "FK_lawyer_languages_languages_LanguageId",
                        column: x => x.LanguageId,
                        principalTable: "languages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_lawyer_languages_lawyer_profiles_LawyerProfileId",
                        column: x => x.LawyerProfileId,
                        principalTable: "lawyer_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "lawyer_pricing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    LawyerProfileId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    WrittenPrice = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    Price15 = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    Price30 = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    Price45 = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    PriceIsVatInclusive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsPublished = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lawyer_pricing", x => x.Id);
                    table.ForeignKey(
                        name: "FK_lawyer_pricing_lawyer_profiles_LawyerProfileId",
                        column: x => x.LawyerProfileId,
                        principalTable: "lawyer_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "lawyer_qualifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    LawyerProfileId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    TitleAr = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TitleEn = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Institution = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FromYear = table.Column<int>(type: "int", nullable: true),
                    ToYear = table.Column<int>(type: "int", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lawyer_qualifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_lawyer_qualifications_lawyer_profiles_LawyerProfileId",
                        column: x => x.LawyerProfileId,
                        principalTable: "lawyer_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "lawyer_specialties",
                columns: table => new
                {
                    LawyerProfileId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    SpecialtyId = table.Column<int>(type: "int", nullable: false),
                    RequestCount = table.Column<int>(type: "int", nullable: false),
                    IsPrimary = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lawyer_specialties", x => new { x.LawyerProfileId, x.SpecialtyId });
                    table.ForeignKey(
                        name: "FK_lawyer_specialties_lawyer_profiles_LawyerProfileId",
                        column: x => x.LawyerProfileId,
                        principalTable: "lawyer_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_lawyer_specialties_specialties_SpecialtyId",
                        column: x => x.SpecialtyId,
                        principalTable: "specialties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "service_categories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    NameAr = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NameEn = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Slug = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IconKey = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_service_categories", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "service_catalog_items",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CategoryId = table.Column<int>(type: "int", nullable: false),
                    NameAr = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NameEn = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Slug = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DescriptionAr = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DescriptionEn = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_service_catalog_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_service_catalog_items_service_categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "service_categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_lawyer_profiles_ExperienceRangeId",
                table: "lawyer_profiles",
                column: "ExperienceRangeId");

            migrationBuilder.CreateIndex(
                name: "IX_lawyer_profiles_IsVerified_AvgRating",
                table: "lawyer_profiles",
                columns: new[] { "IsVerified", "AvgRating" });

            migrationBuilder.CreateIndex(
                name: "IX_lawyer_profiles_IsVerified_CityId",
                table: "lawyer_profiles",
                columns: new[] { "IsVerified", "CityId" });

            migrationBuilder.CreateIndex(
                name: "IX_lawyer_profiles_IsVerified_Gender",
                table: "lawyer_profiles",
                columns: new[] { "IsVerified", "Gender" });

            migrationBuilder.CreateIndex(
                name: "IX_lawyer_languages_LanguageId",
                table: "lawyer_languages",
                column: "LanguageId");

            migrationBuilder.CreateIndex(
                name: "IX_lawyer_pricing_LawyerProfileId",
                table: "lawyer_pricing",
                column: "LawyerProfileId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_lawyer_qualifications_LawyerProfileId",
                table: "lawyer_qualifications",
                column: "LawyerProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_lawyer_specialties_SpecialtyId",
                table: "lawyer_specialties",
                column: "SpecialtyId");

            migrationBuilder.CreateIndex(
                name: "IX_service_catalog_items_CategoryId",
                table: "service_catalog_items",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_service_catalog_items_Slug",
                table: "service_catalog_items",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_service_categories_Slug",
                table: "service_categories",
                column: "Slug",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_lawyer_profiles_experience_ranges_ExperienceRangeId",
                table: "lawyer_profiles",
                column: "ExperienceRangeId",
                principalTable: "experience_ranges",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_lawyer_profiles_experience_ranges_ExperienceRangeId",
                table: "lawyer_profiles");

            migrationBuilder.DropTable(
                name: "lawyer_languages");

            migrationBuilder.DropTable(
                name: "lawyer_pricing");

            migrationBuilder.DropTable(
                name: "lawyer_qualifications");

            migrationBuilder.DropTable(
                name: "lawyer_specialties");

            migrationBuilder.DropTable(
                name: "service_catalog_items");

            migrationBuilder.DropTable(
                name: "service_categories");

            migrationBuilder.DropIndex(
                name: "IX_lawyer_profiles_ExperienceRangeId",
                table: "lawyer_profiles");

            migrationBuilder.DropIndex(
                name: "IX_lawyer_profiles_IsVerified_AvgRating",
                table: "lawyer_profiles");

            migrationBuilder.DropIndex(
                name: "IX_lawyer_profiles_IsVerified_CityId",
                table: "lawyer_profiles");

            migrationBuilder.DropIndex(
                name: "IX_lawyer_profiles_IsVerified_Gender",
                table: "lawyer_profiles");

            migrationBuilder.DropColumn(
                name: "AvgRating",
                table: "lawyer_profiles");

            migrationBuilder.DropColumn(
                name: "CompletedRequestCount",
                table: "lawyer_profiles");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "lawyer_profiles");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "lawyer_profiles");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                table: "lawyer_profiles");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "lawyer_profiles");

            migrationBuilder.DropColumn(
                name: "ExperienceRangeId",
                table: "lawyer_profiles");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "lawyer_profiles");

            migrationBuilder.DropColumn(
                name: "RatingCount",
                table: "lawyer_profiles");

            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                table: "lawyer_profiles");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "lawyer_profiles");
        }
    }
}
