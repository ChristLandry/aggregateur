using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AggregatorPlatform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SubscriptionUniquePerPartner : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Subscriptions_CustomerId_PartnerId_PhoneNumber",
                table: "Subscriptions");

            migrationBuilder.DropIndex(
                name: "IX_Subscriptions_PartnerId",
                table: "Subscriptions");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_CustomerId",
                table: "Subscriptions",
                column: "CustomerId");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Subscriptions_CustomerId",
                table: "Subscriptions");

            migrationBuilder.DropIndex(
                name: "IX_Subscriptions_Partner_BankAccount_Unique",
                table: "Subscriptions");

            migrationBuilder.DropIndex(
                name: "IX_Subscriptions_Partner_Phone_Unique",
                table: "Subscriptions");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_CustomerId_PartnerId_PhoneNumber",
                table: "Subscriptions",
                columns: new[] { "CustomerId", "PartnerId", "PhoneNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_PartnerId",
                table: "Subscriptions",
                column: "PartnerId");
        }
    }
}
