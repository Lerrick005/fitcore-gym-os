using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FitCore.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAttendeeStatusAndCapacity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Capacity",
                table: "Programmes",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "ProgrammeAssignments",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Capacity",
                table: "Programmes");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "ProgrammeAssignments");
        }
    }
}
