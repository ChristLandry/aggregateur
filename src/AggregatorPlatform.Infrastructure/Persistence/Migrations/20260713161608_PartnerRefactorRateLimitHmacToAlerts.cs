using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AggregatorPlatform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PartnerRefactorRateLimitHmacToAlerts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RateLimitPerMin",
                table: "Partners");

            migrationBuilder.DropColumn(
                name: "RequireHmac",
                table: "Partners");

            migrationBuilder.AddColumn<int>(
                name: "AlertChannels",
                table: "Partners",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "LowBalanceReferenceAmount",
                table: "Partners",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LowBalanceThresholdPercent",
                table: "Partners",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AlertChannels",
                table: "Partners");

            migrationBuilder.DropColumn(
                name: "LowBalanceReferenceAmount",
                table: "Partners");

            migrationBuilder.DropColumn(
                name: "LowBalanceThresholdPercent",
                table: "Partners");

            migrationBuilder.AddColumn<int>(
                name: "RateLimitPerMin",
                table: "Partners",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "RequireHmac",
                table: "Partners",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
