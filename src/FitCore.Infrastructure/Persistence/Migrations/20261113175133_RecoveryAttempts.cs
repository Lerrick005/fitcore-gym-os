using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FitCore.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RecoveryAttempts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RecoveryAttempts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ChallengeId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    InstallationId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UsedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RecoveryCodeHash = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecoveryAttempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecoveryAttempts_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RecoveryAttempts_ChallengeId",
                table: "RecoveryAttempts",
                column: "ChallengeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RecoveryAttempts_UserId",
                table: "RecoveryAttempts",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RecoveryAttempts");
        }
    }
}
