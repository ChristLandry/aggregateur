using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AggregatorPlatform.Infrastructure.Persistence.Migrations
{
    public partial class RemoveBankCodeFromSubscriptions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop the BankCode column from Subscriptions
            migrationBuilder.DropColumn(
                name: "BankCode",
                table: "Subscriptions");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Recreate the BankCode column in case of rollback
            migrationBuilder.AddColumn<string>(
                name: "BankCode",
                table: "Subscriptions",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: string.Empty);
        }
    }
}
