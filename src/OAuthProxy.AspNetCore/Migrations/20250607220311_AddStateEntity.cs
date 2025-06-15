using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OAuthProxy.AspNetCore.Migrations
{
    /// <inheritdoc />
    public partial class AddStateEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OAuthStates",
                columns: table => new
                {
                    StateId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    ThirdPartyServiceProvider = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    StateSecret = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OAuthStates", x => x.StateId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OAuthStates_StateId_ThirdPartyServiceProvider",
                table: "OAuthStates",
                columns: new[] { "StateId", "ThirdPartyServiceProvider" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OAuthStates");
        }
    }
}
