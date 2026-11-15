using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace NoAzul.Infra.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CustomCategoryId",
                table: "Transactions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CustomCategoryId",
                table: "CategoryRules",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CustomCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CreatedBy = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<int>(type: "integer", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<int>(type: "integer", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomCategories_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_CustomCategoryId",
                table: "Transactions",
                column: "CustomCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_CategoryRules_CustomCategoryId",
                table: "CategoryRules",
                column: "CustomCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomCategories_UserId_Name",
                table: "CustomCategories",
                columns: new[] { "UserId", "Name" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.AddForeignKey(
                name: "FK_CategoryRules_CustomCategories_CustomCategoryId",
                table: "CategoryRules",
                column: "CustomCategoryId",
                principalTable: "CustomCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_CustomCategories_CustomCategoryId",
                table: "Transactions",
                column: "CustomCategoryId",
                principalTable: "CustomCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CategoryRules_CustomCategories_CustomCategoryId",
                table: "CategoryRules");

            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_CustomCategories_CustomCategoryId",
                table: "Transactions");

            migrationBuilder.DropTable(
                name: "CustomCategories");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_CustomCategoryId",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_CategoryRules_CustomCategoryId",
                table: "CategoryRules");

            migrationBuilder.DropColumn(
                name: "CustomCategoryId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "CustomCategoryId",
                table: "CategoryRules");
        }
    }
}
