using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimpleApiTester.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVariableIsSecret : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSecret",
                table: "Variables",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsSecret",
                table: "Variables");
        }
    }
}
