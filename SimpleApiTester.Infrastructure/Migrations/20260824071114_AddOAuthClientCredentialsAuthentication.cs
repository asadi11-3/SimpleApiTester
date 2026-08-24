using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimpleApiTester.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOAuthClientCredentialsAuthentication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_DataSourceAuthentications_AuthShape",
                table: "DataSourceAuthentications");

            migrationBuilder.DropCheckConstraint(
                name: "CK_DataSourceAuthentications_SourceTypes",
                table: "DataSourceAuthentications");

            migrationBuilder.AddColumn<string>(
                name: "OAuthClientIdSourceKey",
                table: "DataSourceAuthentications",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OAuthClientIdSourceType",
                table: "DataSourceAuthentications",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OAuthClientSecretSourceKey",
                table: "DataSourceAuthentications",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OAuthClientSecretSourceType",
                table: "DataSourceAuthentications",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OAuthScope",
                table: "DataSourceAuthentications",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OAuthTokenEndpoint",
                table: "DataSourceAuthentications",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_DataSourceAuthentications_AuthShape",
                table: "DataSourceAuthentications",
                sql: "([AuthenticationType] = 'Bearer' AND [ValueSourceType] IS NOT NULL AND [SourceKey] IS NOT NULL AND [ApiKeyHeaderName] IS NULL AND [ApiKeyLocation] IS NULL AND [UsernameSourceType] IS NULL AND [UsernameSourceKey] IS NULL AND [PasswordSourceType] IS NULL AND [PasswordSourceKey] IS NULL AND [OAuthTokenEndpoint] IS NULL AND [OAuthClientIdSourceType] IS NULL AND [OAuthClientIdSourceKey] IS NULL AND [OAuthClientSecretSourceType] IS NULL AND [OAuthClientSecretSourceKey] IS NULL AND [OAuthScope] IS NULL) OR ([AuthenticationType] = 'ApiKey' AND [ValueSourceType] IS NOT NULL AND [SourceKey] IS NOT NULL AND [ApiKeyHeaderName] IS NOT NULL AND [ApiKeyLocation] IS NOT NULL AND [UsernameSourceType] IS NULL AND [UsernameSourceKey] IS NULL AND [PasswordSourceType] IS NULL AND [PasswordSourceKey] IS NULL AND [OAuthTokenEndpoint] IS NULL AND [OAuthClientIdSourceType] IS NULL AND [OAuthClientIdSourceKey] IS NULL AND [OAuthClientSecretSourceType] IS NULL AND [OAuthClientSecretSourceKey] IS NULL AND [OAuthScope] IS NULL) OR ([AuthenticationType] = 'Basic' AND [ValueSourceType] IS NULL AND [SourceKey] IS NULL AND [ApiKeyHeaderName] IS NULL AND [ApiKeyLocation] IS NULL AND [UsernameSourceType] IS NOT NULL AND [UsernameSourceKey] IS NOT NULL AND [PasswordSourceType] IS NOT NULL AND [PasswordSourceKey] IS NOT NULL AND [OAuthTokenEndpoint] IS NULL AND [OAuthClientIdSourceType] IS NULL AND [OAuthClientIdSourceKey] IS NULL AND [OAuthClientSecretSourceType] IS NULL AND [OAuthClientSecretSourceKey] IS NULL AND [OAuthScope] IS NULL) OR ([AuthenticationType] = 'OAuthClientCredentials' AND [ValueSourceType] IS NULL AND [SourceKey] IS NULL AND [ApiKeyHeaderName] IS NULL AND [ApiKeyLocation] IS NULL AND [UsernameSourceType] IS NULL AND [UsernameSourceKey] IS NULL AND [PasswordSourceType] IS NULL AND [PasswordSourceKey] IS NULL AND [OAuthTokenEndpoint] IS NOT NULL AND [OAuthClientIdSourceType] IS NOT NULL AND [OAuthClientIdSourceKey] IS NOT NULL AND [OAuthClientSecretSourceType] IS NOT NULL AND [OAuthClientSecretSourceKey] IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_DataSourceAuthentications_SourceTypes",
                table: "DataSourceAuthentications",
                sql: "([ValueSourceType] IS NULL OR [ValueSourceType] IN ('Variable', 'UserSecret', 'EnvironmentVariable')) AND ([UsernameSourceType] IS NULL OR [UsernameSourceType] IN ('Variable', 'UserSecret', 'EnvironmentVariable')) AND ([PasswordSourceType] IS NULL OR [PasswordSourceType] IN ('Variable', 'UserSecret', 'EnvironmentVariable')) AND ([OAuthClientIdSourceType] IS NULL OR [OAuthClientIdSourceType] IN ('Variable', 'UserSecret', 'EnvironmentVariable')) AND ([OAuthClientSecretSourceType] IS NULL OR [OAuthClientSecretSourceType] IN ('Variable', 'UserSecret', 'EnvironmentVariable'))");
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
                name: "OAuthClientIdSourceKey",
                table: "DataSourceAuthentications");

            migrationBuilder.DropColumn(
                name: "OAuthClientIdSourceType",
                table: "DataSourceAuthentications");

            migrationBuilder.DropColumn(
                name: "OAuthClientSecretSourceKey",
                table: "DataSourceAuthentications");

            migrationBuilder.DropColumn(
                name: "OAuthClientSecretSourceType",
                table: "DataSourceAuthentications");

            migrationBuilder.DropColumn(
                name: "OAuthScope",
                table: "DataSourceAuthentications");

            migrationBuilder.DropColumn(
                name: "OAuthTokenEndpoint",
                table: "DataSourceAuthentications");

            migrationBuilder.AddCheckConstraint(
                name: "CK_DataSourceAuthentications_AuthShape",
                table: "DataSourceAuthentications",
                sql: "([AuthenticationType] = 'Bearer' AND [ValueSourceType] IS NOT NULL AND [SourceKey] IS NOT NULL AND [ApiKeyHeaderName] IS NULL AND [ApiKeyLocation] IS NULL AND [UsernameSourceType] IS NULL AND [UsernameSourceKey] IS NULL AND [PasswordSourceType] IS NULL AND [PasswordSourceKey] IS NULL) OR ([AuthenticationType] = 'ApiKey' AND [ValueSourceType] IS NOT NULL AND [SourceKey] IS NOT NULL AND [ApiKeyHeaderName] IS NOT NULL AND [ApiKeyLocation] IS NOT NULL AND [UsernameSourceType] IS NULL AND [UsernameSourceKey] IS NULL AND [PasswordSourceType] IS NULL AND [PasswordSourceKey] IS NULL) OR ([AuthenticationType] = 'Basic' AND [ValueSourceType] IS NULL AND [SourceKey] IS NULL AND [ApiKeyHeaderName] IS NULL AND [ApiKeyLocation] IS NULL AND [UsernameSourceType] IS NOT NULL AND [UsernameSourceKey] IS NOT NULL AND [PasswordSourceType] IS NOT NULL AND [PasswordSourceKey] IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_DataSourceAuthentications_SourceTypes",
                table: "DataSourceAuthentications",
                sql: "([ValueSourceType] IS NULL OR [ValueSourceType] IN ('Variable', 'UserSecret', 'EnvironmentVariable')) AND ([UsernameSourceType] IS NULL OR [UsernameSourceType] IN ('Variable', 'UserSecret', 'EnvironmentVariable')) AND ([PasswordSourceType] IS NULL OR [PasswordSourceType] IN ('Variable', 'UserSecret', 'EnvironmentVariable'))");
        }
    }
}
