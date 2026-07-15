using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AggregatorPlatform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTransactionOperationType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OperationType",
                table: "Transactions",
                type: "nvarchar(3)",
                maxLength: 3,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OperationType",
                table: "Transactions");
        }
    }
}
