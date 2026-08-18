using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimpleApiTester.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDataSourceAuthentication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AuthenticationMode",
                table: "Operations",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Inherit");

            migrationBuilder.CreateTable(
                name: "DataSourceAuthentications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DataSourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AuthenticationType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ValueSourceType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    SourceKey = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ApiKeyHeaderName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataSourceAuthentications", x => x.Id);
                    table.CheckConstraint("CK_DataSourceAuthentications_AuthShape", "([AuthenticationType] = 'Bearer' AND [ApiKeyHeaderName] IS NULL) OR ([AuthenticationType] = 'ApiKey' AND [ApiKeyHeaderName] IS NOT NULL)");
                    table.CheckConstraint("CK_DataSourceAuthentications_ValueSourceType", "[ValueSourceType] IN ('Variable', 'UserSecret', 'EnvironmentVariable')");
                    table.ForeignKey(
                        name: "FK_DataSourceAuthentications_DataSources_DataSourceId",
                        column: x => x.DataSourceId,
                        principalTable: "DataSources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DataSourceAuthentications_DataSourceId",
                table: "DataSourceAuthentications",
                column: "DataSourceId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DataSourceAuthentications");

            migrationBuilder.DropColumn(
                name: "AuthenticationMode",
                table: "Operations");
        }
    }
}
