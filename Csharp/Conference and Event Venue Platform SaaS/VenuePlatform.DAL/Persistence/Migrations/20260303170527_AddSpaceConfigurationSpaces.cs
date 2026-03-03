using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VenuePlatform.DAL.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSpaceConfigurationSpaces : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SpaceConfigurationSpaces",
                columns: table => new
                {
                    SpaceConfigurationId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SpaceId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpaceConfigurationSpaces", x => new { x.SpaceConfigurationId, x.SpaceId });
                    table.ForeignKey(
                        name: "FK_SpaceConfigurationSpaces_SpaceConfigurations_SpaceConfigurationId",
                        column: x => x.SpaceConfigurationId,
                        principalTable: "SpaceConfigurations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SpaceConfigurationSpaces_Spaces_SpaceId",
                        column: x => x.SpaceId,
                        principalTable: "Spaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SpaceConfigurationSpaces_SpaceConfigurationId",
                table: "SpaceConfigurationSpaces",
                column: "SpaceConfigurationId");

            migrationBuilder.CreateIndex(
                name: "IX_SpaceConfigurationSpaces_SpaceId",
                table: "SpaceConfigurationSpaces",
                column: "SpaceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SpaceConfigurationSpaces");
        }
    }
}
