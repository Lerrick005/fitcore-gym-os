using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FitCore.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class LoyaltyRewards : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LoyaltyRewards",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    PointsRequired = table.Column<int>(type: "INTEGER", nullable: false),
                    RewardType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ValueMinor = table.Column<long>(type: "INTEGER", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoyaltyRewards", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyRewards_IsActive_PointsRequired",
                table: "LoyaltyRewards",
                columns: new[] { "IsActive", "PointsRequired" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LoyaltyRewards");
        }
    }
}
