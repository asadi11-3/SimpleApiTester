using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimpleApiTester.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddApiKeyQueryAuthentication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_DataSourceAuthentications_AuthShape",
                table: "DataSourceAuthentications");

            migrationBuilder.AddColumn<string>(
                name: "ApiKeyLocation",
                table: "DataSourceAuthentications",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.Sql(
                "UPDATE [DataSourceAuthentications] SET [ApiKeyLocation] = 'Header' WHERE [AuthenticationType] = 'ApiKey'");

            migrationBuilder.AddCheckConstraint(
                name: "CK_DataSourceAuthentications_ApiKeyLocation",
                table: "DataSourceAuthentications",
                sql: "([ApiKeyLocation] IS NULL OR [ApiKeyLocation] IN ('Header', 'Query'))");

            migrationBuilder.AddCheckConstraint(
                name: "CK_DataSourceAuthentications_AuthShape",
                table: "DataSourceAuthentications",
                sql: "([AuthenticationType] = 'Bearer' AND [ValueSourceType] IS NOT NULL AND [SourceKey] IS NOT NULL AND [ApiKeyHeaderName] IS NULL AND [ApiKeyLocation] IS NULL AND [UsernameSourceType] IS NULL AND [UsernameSourceKey] IS NULL AND [PasswordSourceType] IS NULL AND [PasswordSourceKey] IS NULL) OR ([AuthenticationType] = 'ApiKey' AND [ValueSourceType] IS NOT NULL AND [SourceKey] IS NOT NULL AND [ApiKeyHeaderName] IS NOT NULL AND [ApiKeyLocation] IS NOT NULL AND [UsernameSourceType] IS NULL AND [UsernameSourceKey] IS NULL AND [PasswordSourceType] IS NULL AND [PasswordSourceKey] IS NULL) OR ([AuthenticationType] = 'Basic' AND [ValueSourceType] IS NULL AND [SourceKey] IS NULL AND [ApiKeyHeaderName] IS NULL AND [ApiKeyLocation] IS NULL AND [UsernameSourceType] IS NOT NULL AND [UsernameSourceKey] IS NOT NULL AND [PasswordSourceType] IS NOT NULL AND [PasswordSourceKey] IS NOT NULL)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_DataSourceAuthentications_ApiKeyLocation",
                table: "DataSourceAuthentications");

            migrationBuilder.DropCheckConstraint(
                name: "CK_DataSourceAuthentications_AuthShape",
                table: "DataSourceAuthentications");

            migrationBuilder.DropColumn(
                name: "ApiKeyLocation",
                table: "DataSourceAuthentications");

            migrationBuilder.AddCheckConstraint(
                name: "CK_DataSourceAuthentications_AuthShape",
                table: "DataSourceAuthentications",
                sql: "([AuthenticationType] = 'Bearer' AND [ValueSourceType] IS NOT NULL AND [SourceKey] IS NOT NULL AND [ApiKeyHeaderName] IS NULL AND [UsernameSourceType] IS NULL AND [UsernameSourceKey] IS NULL AND [PasswordSourceType] IS NULL AND [PasswordSourceKey] IS NULL) OR ([AuthenticationType] = 'ApiKey' AND [ValueSourceType] IS NOT NULL AND [SourceKey] IS NOT NULL AND [ApiKeyHeaderName] IS NOT NULL AND [UsernameSourceType] IS NULL AND [UsernameSourceKey] IS NULL AND [PasswordSourceType] IS NULL AND [PasswordSourceKey] IS NULL) OR ([AuthenticationType] = 'Basic' AND [ValueSourceType] IS NULL AND [SourceKey] IS NULL AND [ApiKeyHeaderName] IS NULL AND [UsernameSourceType] IS NOT NULL AND [UsernameSourceKey] IS NOT NULL AND [PasswordSourceType] IS NOT NULL AND [PasswordSourceKey] IS NOT NULL)");
        }
    }
}
