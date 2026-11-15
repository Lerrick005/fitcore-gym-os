using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FitCore.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddClassSchedule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DurationMinutes",
                table: "Programmes",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartTime",
                table: "Programmes",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DurationMinutes",
                table: "Programmes");

            migrationBuilder.DropColumn(
                name: "StartTime",
                table: "Programmes");
        }
    }
}
