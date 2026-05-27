using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AggregatorPlatform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPartnerIsWebFlag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsWebPartner",
                table: "Partners",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Partners_IsWebPartner",
                table: "Partners",
                column: "IsWebPartner");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Partners_IsWebPartner",
                table: "Partners");

            migrationBuilder.DropColumn(
                name: "IsWebPartner",
                table: "Partners");
        }
    }
}
