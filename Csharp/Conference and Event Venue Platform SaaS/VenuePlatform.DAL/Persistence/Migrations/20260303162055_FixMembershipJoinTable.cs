using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VenuePlatform.DAL.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixMembershipJoinTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_UserCompanyMemberships",
                table: "UserCompanyMemberships");

            migrationBuilder.DropIndex(
                name: "IX_UserCompanyMemberships_UserId_CompanyId",
                table: "UserCompanyMemberships");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "UserCompanyMemberships");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserCompanyMemberships",
                table: "UserCompanyMemberships",
                columns: new[] { "UserId", "CompanyId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_UserCompanyMemberships",
                table: "UserCompanyMemberships");

            migrationBuilder.AddColumn<Guid>(
                name: "Id",
                table: "UserCompanyMemberships",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserCompanyMemberships",
                table: "UserCompanyMemberships",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_UserCompanyMemberships_UserId_CompanyId",
                table: "UserCompanyMemberships",
                columns: new[] { "UserId", "CompanyId" },
                unique: true);
        }
    }
}
