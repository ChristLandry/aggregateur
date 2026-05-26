using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AggregatorPlatform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPartnerEndpoints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PartnerEndpoints",
                columns: table => new
                {
                    PartnerEndpointId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PartnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EndpointKey = table.Column<int>(type: "int", nullable: false),
                    SchemaId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartnerEndpoints", x => x.PartnerEndpointId);
                    table.ForeignKey(
                        name: "FK_PartnerEndpoints_AccountingSchemas_SchemaId",
                        column: x => x.SchemaId,
                        principalTable: "AccountingSchemas",
                        principalColumn: "SchemaId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PartnerEndpoints_Partners_PartnerId",
                        column: x => x.PartnerId,
                        principalTable: "Partners",
                        principalColumn: "PartnerId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PartnerEndpoints_Partner_Key_Unique",
                table: "PartnerEndpoints",
                columns: new[] { "PartnerId", "EndpointKey" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_PartnerEndpoints_SchemaId",
                table: "PartnerEndpoints",
                column: "SchemaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PartnerEndpoints");
        }
    }
}
