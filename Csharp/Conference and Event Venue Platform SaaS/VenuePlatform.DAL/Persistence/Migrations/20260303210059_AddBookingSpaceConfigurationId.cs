using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VenuePlatform.DAL.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingSpaceConfigurationId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SpaceConfigurationId",
                table: "Bookings",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_CompanyId_SpaceConfigurationId",
                table: "Bookings",
                columns: new[] { "CompanyId", "SpaceConfigurationId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Bookings_CompanyId_SpaceConfigurationId",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "SpaceConfigurationId",
                table: "Bookings");
        }
    }
}
