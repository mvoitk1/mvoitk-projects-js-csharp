using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VenuePlatform.DAL.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSpaceConfigurations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Spaces",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "SpaceConfigurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CompanyId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    HourlyRateOverride = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    MinBookingMinutesOverride = table.Column<int>(type: "INTEGER", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpaceConfigurations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SpaceConfigurations_CompanyId_Name",
                table: "SpaceConfigurations",
                columns: new[] { "CompanyId", "Name" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SpaceConfigurations");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Spaces");
        }
    }
}
