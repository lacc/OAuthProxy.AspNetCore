using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OAuthProxy.AspNetCore.Migrations
{
    /// <inheritdoc />
    public partial class AddLocalRedirectUriSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LocalRedirectUris",
                columns: table => new
                {
                    AuthState = table.Column<string>(type: "TEXT", nullable: false),
                    LocalRedirectUrl = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocalRedirectUris", x => x.AuthState);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LocalRedirectUris");
        }
    }
}
