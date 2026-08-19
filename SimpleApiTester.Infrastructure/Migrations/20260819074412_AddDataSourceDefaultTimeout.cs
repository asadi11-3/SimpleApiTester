using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimpleApiTester.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDataSourceDefaultTimeout : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DefaultTimeoutSeconds",
                table: "DataSources",
                type: "int",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_DataSources_DefaultTimeoutSeconds_Range",
                table: "DataSources",
                sql: "[DefaultTimeoutSeconds] IS NULL OR ([DefaultTimeoutSeconds] >= 1 AND [DefaultTimeoutSeconds] <= 300)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_DataSources_DefaultTimeoutSeconds_Range",
                table: "DataSources");

            migrationBuilder.DropColumn(
                name: "DefaultTimeoutSeconds",
                table: "DataSources");
        }
    }
}
