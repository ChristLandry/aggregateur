using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AggregatorPlatform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPartnerBankAccount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PartnerBankAccount",
                table: "PartnerAccounts",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PartnerBankAccount",
                table: "PartnerAccounts");
        }
    }
}
