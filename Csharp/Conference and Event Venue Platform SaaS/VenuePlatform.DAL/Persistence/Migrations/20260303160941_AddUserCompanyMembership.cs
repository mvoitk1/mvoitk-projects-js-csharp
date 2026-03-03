using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VenuePlatform.DAL.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserCompanyMembership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserCompanyMemberships",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CompanyId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Role = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserCompanyMemberships", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserCompanyMemberships_CompanyId",
                table: "UserCompanyMemberships",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_UserCompanyMemberships_UserId_CompanyId",
                table: "UserCompanyMemberships",
                columns: new[] { "UserId", "CompanyId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserCompanyMemberships");
        }
    }
}
