using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AggregatorPlatform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SubscriptionUniqueTriplet : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Subscriptions_Partner_BankAccount_Unique",
                table: "Subscriptions");

            migrationBuilder.DropIndex(
                name: "IX_Subscriptions_Partner_Phone_Unique",
                table: "Subscriptions");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_Partner_Bank_Phone_Unique",
                table: "Subscriptions",
                columns: new[] { "PartnerId", "BankAccountNumber", "PhoneNumber" },
                unique: true,
                filter: "[IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Subscriptions_Partner_Bank_Phone_Unique",
                table: "Subscriptions");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_Partner_BankAccount_Unique",
                table: "Subscriptions",
                columns: new[] { "PartnerId", "BankAccountNumber" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_Partner_Phone_Unique",
                table: "Subscriptions",
                columns: new[] { "PartnerId", "PhoneNumber" },
                unique: true,
                filter: "[IsDeleted] = 0");
        }
    }
}
