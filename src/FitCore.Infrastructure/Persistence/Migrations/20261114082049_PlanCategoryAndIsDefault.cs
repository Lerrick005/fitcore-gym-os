using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FitCore.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PlanCategoryAndIsDefault : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "MembershipPlans",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                defaultValue: "Membership");

            migrationBuilder.AddColumn<bool>(
                name: "IsDefault",
                table: "MembershipPlans",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Category",
                table: "MembershipPlans");

            migrationBuilder.DropColumn(
                name: "IsDefault",
                table: "MembershipPlans");
        }
    }
}
