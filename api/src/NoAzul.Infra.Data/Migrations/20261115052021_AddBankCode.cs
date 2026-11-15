using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NoAzul.Infra.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBankCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BankCode",
                table: "BankConnections",
                type: "character varying(3)",
                maxLength: 3,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BankCode",
                table: "BankConnections");
        }
    }
}
