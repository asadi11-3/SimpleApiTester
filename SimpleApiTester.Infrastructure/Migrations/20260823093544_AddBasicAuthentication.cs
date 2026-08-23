using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimpleApiTester.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBasicAuthentication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_DataSourceAuthentications_AuthShape",
                table: "DataSourceAuthentications");

            migrationBuilder.DropCheckConstraint(
                name: "CK_DataSourceAuthentications_ValueSourceType",
                table: "DataSourceAuthentications");

            migrationBuilder.AlterColumn<string>(
                name: "ValueSourceType",
                table: "DataSourceAuthentications",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<string>(
                name: "SourceKey",
                table: "DataSourceAuthentications",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);

            migrationBuilder.AddColumn<string>(
                name: "PasswordSourceKey",
                table: "DataSourceAuthentications",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PasswordSourceType",
                table: "DataSourceAuthentications",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UsernameSourceKey",
                table: "DataSourceAuthentications",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UsernameSourceType",
                table: "DataSourceAuthentications",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_DataSourceAuthentications_AuthShape",
                table: "DataSourceAuthentications",
                sql: "([AuthenticationType] = 'Bearer' AND [ValueSourceType] IS NOT NULL AND [SourceKey] IS NOT NULL AND [ApiKeyHeaderName] IS NULL AND [UsernameSourceType] IS NULL AND [UsernameSourceKey] IS NULL AND [PasswordSourceType] IS NULL AND [PasswordSourceKey] IS NULL) OR ([AuthenticationType] = 'ApiKey' AND [ValueSourceType] IS NOT NULL AND [SourceKey] IS NOT NULL AND [ApiKeyHeaderName] IS NOT NULL AND [UsernameSourceType] IS NULL AND [UsernameSourceKey] IS NULL AND [PasswordSourceType] IS NULL AND [PasswordSourceKey] IS NULL) OR ([AuthenticationType] = 'Basic' AND [ValueSourceType] IS NULL AND [SourceKey] IS NULL AND [ApiKeyHeaderName] IS NULL AND [UsernameSourceType] IS NOT NULL AND [UsernameSourceKey] IS NOT NULL AND [PasswordSourceType] IS NOT NULL AND [PasswordSourceKey] IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_DataSourceAuthentications_SourceTypes",
                table: "DataSourceAuthentications",
                sql: "([ValueSourceType] IS NULL OR [ValueSourceType] IN ('Variable', 'UserSecret', 'EnvironmentVariable')) AND ([UsernameSourceType] IS NULL OR [UsernameSourceType] IN ('Variable', 'UserSecret', 'EnvironmentVariable')) AND ([PasswordSourceType] IS NULL OR [PasswordSourceType] IN ('Variable', 'UserSecret', 'EnvironmentVariable'))");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_DataSourceAuthentications_AuthShape",
                table: "DataSourceAuthentications");

            migrationBuilder.DropCheckConstraint(
                name: "CK_DataSourceAuthentications_SourceTypes",
                table: "DataSourceAuthentications");

            migrationBuilder.DropColumn(
                name: "PasswordSourceKey",
                table: "DataSourceAuthentications");

            migrationBuilder.DropColumn(
                name: "PasswordSourceType",
                table: "DataSourceAuthentications");

            migrationBuilder.DropColumn(
                name: "UsernameSourceKey",
                table: "DataSourceAuthentications");

            migrationBuilder.DropColumn(
                name: "UsernameSourceType",
                table: "DataSourceAuthentications");

            migrationBuilder.AlterColumn<string>(
                name: "ValueSourceType",
                table: "DataSourceAuthentications",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SourceKey",
                table: "DataSourceAuthentications",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_DataSourceAuthentications_AuthShape",
                table: "DataSourceAuthentications",
                sql: "([AuthenticationType] = 'Bearer' AND [ApiKeyHeaderName] IS NULL) OR ([AuthenticationType] = 'ApiKey' AND [ApiKeyHeaderName] IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_DataSourceAuthentications_ValueSourceType",
                table: "DataSourceAuthentications",
                sql: "[ValueSourceType] IN ('Variable', 'UserSecret', 'EnvironmentVariable')");
        }
    }
}
