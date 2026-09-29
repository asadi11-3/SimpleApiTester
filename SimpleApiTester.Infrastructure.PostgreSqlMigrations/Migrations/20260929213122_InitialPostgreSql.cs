using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimpleApiTester.Infrastructure.PostgreSqlMigrations.Migrations
{
    /// <inheritdoc />
    public partial class InitialPostgreSql : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:citext", ",,");

            migrationBuilder.CreateTable(
                name: "DataSources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "citext", maxLength: 100, nullable: false),
                    DefaultTimeoutSeconds = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataSources", x => x.Id);
                    table.CheckConstraint("CK_DataSources_DefaultTimeoutSeconds_Range", "\"DefaultTimeoutSeconds\" IS NULL OR (\"DefaultTimeoutSeconds\" >= 1 AND \"DefaultTimeoutSeconds\" <= 300)");
                    table.CheckConstraint("CK_DataSources_Key_Length", "char_length(\"Key\") <= 100");
                });

            migrationBuilder.CreateTable(
                name: "DataSourceAuthentications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DataSourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthenticationType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ValueSourceType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    SourceKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ApiKeyHeaderName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ApiKeyLocation = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    UsernameSourceType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    UsernameSourceKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PasswordSourceType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    PasswordSourceKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    OAuthTokenEndpoint = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    OAuthClientIdSourceType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    OAuthClientIdSourceKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    OAuthClientSecretSourceType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    OAuthClientSecretSourceKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    OAuthScope = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataSourceAuthentications", x => x.Id);
                    table.CheckConstraint("CK_DataSourceAuthentications_ApiKeyLocation", "(\"ApiKeyLocation\" IS NULL OR \"ApiKeyLocation\" IN ('Header', 'Query'))");
                    table.CheckConstraint("CK_DataSourceAuthentications_AuthShape", "(\"AuthenticationType\" = 'Bearer' AND \"ValueSourceType\" IS NOT NULL AND \"SourceKey\" IS NOT NULL AND \"ApiKeyHeaderName\" IS NULL AND \"ApiKeyLocation\" IS NULL AND \"UsernameSourceType\" IS NULL AND \"UsernameSourceKey\" IS NULL AND \"PasswordSourceType\" IS NULL AND \"PasswordSourceKey\" IS NULL AND \"OAuthTokenEndpoint\" IS NULL AND \"OAuthClientIdSourceType\" IS NULL AND \"OAuthClientIdSourceKey\" IS NULL AND \"OAuthClientSecretSourceType\" IS NULL AND \"OAuthClientSecretSourceKey\" IS NULL AND \"OAuthScope\" IS NULL) OR (\"AuthenticationType\" = 'ApiKey' AND \"ValueSourceType\" IS NOT NULL AND \"SourceKey\" IS NOT NULL AND \"ApiKeyHeaderName\" IS NOT NULL AND \"ApiKeyLocation\" IS NOT NULL AND \"UsernameSourceType\" IS NULL AND \"UsernameSourceKey\" IS NULL AND \"PasswordSourceType\" IS NULL AND \"PasswordSourceKey\" IS NULL AND \"OAuthTokenEndpoint\" IS NULL AND \"OAuthClientIdSourceType\" IS NULL AND \"OAuthClientIdSourceKey\" IS NULL AND \"OAuthClientSecretSourceType\" IS NULL AND \"OAuthClientSecretSourceKey\" IS NULL AND \"OAuthScope\" IS NULL) OR (\"AuthenticationType\" = 'Basic' AND \"ValueSourceType\" IS NULL AND \"SourceKey\" IS NULL AND \"ApiKeyHeaderName\" IS NULL AND \"ApiKeyLocation\" IS NULL AND \"UsernameSourceType\" IS NOT NULL AND \"UsernameSourceKey\" IS NOT NULL AND \"PasswordSourceType\" IS NOT NULL AND \"PasswordSourceKey\" IS NOT NULL AND \"OAuthTokenEndpoint\" IS NULL AND \"OAuthClientIdSourceType\" IS NULL AND \"OAuthClientIdSourceKey\" IS NULL AND \"OAuthClientSecretSourceType\" IS NULL AND \"OAuthClientSecretSourceKey\" IS NULL AND \"OAuthScope\" IS NULL) OR (\"AuthenticationType\" = 'OAuthClientCredentials' AND \"ValueSourceType\" IS NULL AND \"SourceKey\" IS NULL AND \"ApiKeyHeaderName\" IS NULL AND \"ApiKeyLocation\" IS NULL AND \"UsernameSourceType\" IS NULL AND \"UsernameSourceKey\" IS NULL AND \"PasswordSourceType\" IS NULL AND \"PasswordSourceKey\" IS NULL AND \"OAuthTokenEndpoint\" IS NOT NULL AND \"OAuthClientIdSourceType\" IS NOT NULL AND \"OAuthClientIdSourceKey\" IS NOT NULL AND \"OAuthClientSecretSourceType\" IS NOT NULL AND \"OAuthClientSecretSourceKey\" IS NOT NULL)");
                    table.CheckConstraint("CK_DataSourceAuthentications_SourceTypes", "(\"ValueSourceType\" IS NULL OR \"ValueSourceType\" IN ('Variable', 'UserSecret', 'EnvironmentVariable')) AND (\"UsernameSourceType\" IS NULL OR \"UsernameSourceType\" IN ('Variable', 'UserSecret', 'EnvironmentVariable')) AND (\"PasswordSourceType\" IS NULL OR \"PasswordSourceType\" IN ('Variable', 'UserSecret', 'EnvironmentVariable')) AND (\"OAuthClientIdSourceType\" IS NULL OR \"OAuthClientIdSourceType\" IN ('Variable', 'UserSecret', 'EnvironmentVariable')) AND (\"OAuthClientSecretSourceType\" IS NULL OR \"OAuthClientSecretSourceType\" IN ('Variable', 'UserSecret', 'EnvironmentVariable'))");
                    table.ForeignKey(
                        name: "FK_DataSourceAuthentications_DataSources_DataSourceId",
                        column: x => x.DataSourceId,
                        principalTable: "DataSources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DataSourceEnvironments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DataSourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "citext", maxLength: 100, nullable: false),
                    BaseUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataSourceEnvironments", x => x.Id);
                    table.CheckConstraint("CK_DataSourceEnvironments_Name_Length", "char_length(\"Name\") <= 100");
                    table.ForeignKey(
                        name: "FK_DataSourceEnvironments_DataSources_DataSourceId",
                        column: x => x.DataSourceId,
                        principalTable: "DataSources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Operations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DataSourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApiName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Endpoint = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    MethodType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Body = table.Column<string>(type: "text", nullable: true),
                    ContentType = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    AuthenticationMode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Operations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Operations_DataSources_DataSourceId",
                        column: x => x.DataSourceId,
                        principalTable: "DataSources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Variables",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DataSourceEnvironmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "citext", maxLength: 100, nullable: false),
                    Value = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    IsSecret = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Variables", x => x.Id);
                    table.CheckConstraint("CK_Variables_Key_Length", "char_length(\"Key\") <= 100");
                    table.ForeignKey(
                        name: "FK_Variables_DataSourceEnvironments_DataSourceEnvironmentId",
                        column: x => x.DataSourceEnvironmentId,
                        principalTable: "DataSourceEnvironments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Headers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DataSourceId = table.Column<Guid>(type: "uuid", nullable: true),
                    OperationId = table.Column<Guid>(type: "uuid", nullable: true),
                    Key = table.Column<string>(type: "citext", maxLength: 100, nullable: false),
                    Value = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ValueSourceType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    SourceKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Headers", x => x.Id);
                    table.CheckConstraint("CK_Headers_ExactlyOneParent", "((\"DataSourceId\" IS NOT NULL AND \"OperationId\" IS NULL) OR (\"DataSourceId\" IS NULL AND \"OperationId\" IS NOT NULL))");
                    table.CheckConstraint("CK_Headers_Key_Length", "char_length(\"Key\") <= 100");
                    table.CheckConstraint("CK_Headers_ValueSourceShape", "((\"ValueSourceType\" = 'General' AND \"Value\" IS NOT NULL AND \"SourceKey\" IS NULL) OR (\"ValueSourceType\" <> 'General' AND \"Value\" IS NULL AND \"SourceKey\" IS NOT NULL))");
                    table.ForeignKey(
                        name: "FK_Headers_DataSources_DataSourceId",
                        column: x => x.DataSourceId,
                        principalTable: "DataSources",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Headers_Operations_OperationId",
                        column: x => x.OperationId,
                        principalTable: "Operations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QueryParameters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OperationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Value = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QueryParameters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QueryParameters_Operations_OperationId",
                        column: x => x.OperationId,
                        principalTable: "Operations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DataSourceAuthentications_DataSourceId",
                table: "DataSourceAuthentications",
                column: "DataSourceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DataSourceEnvironments_DataSourceId_Name",
                table: "DataSourceEnvironments",
                columns: new[] { "DataSourceId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DataSources_Key",
                table: "DataSources",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Headers_DataSourceId_Key",
                table: "Headers",
                columns: new[] { "DataSourceId", "Key" },
                unique: true,
                filter: "\"DataSourceId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Headers_OperationId_Key",
                table: "Headers",
                columns: new[] { "OperationId", "Key" },
                unique: true,
                filter: "\"OperationId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Operations_DataSourceId",
                table: "Operations",
                column: "DataSourceId");

            migrationBuilder.CreateIndex(
                name: "IX_QueryParameters_OperationId",
                table: "QueryParameters",
                column: "OperationId");

            migrationBuilder.CreateIndex(
                name: "IX_Variables_DataSourceEnvironmentId_Key",
                table: "Variables",
                columns: new[] { "DataSourceEnvironmentId", "Key" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DataSourceAuthentications");

            migrationBuilder.DropTable(
                name: "Headers");

            migrationBuilder.DropTable(
                name: "QueryParameters");

            migrationBuilder.DropTable(
                name: "Variables");

            migrationBuilder.DropTable(
                name: "Operations");

            migrationBuilder.DropTable(
                name: "DataSourceEnvironments");

            migrationBuilder.DropTable(
                name: "DataSources");
        }
    }
}
