using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimpleApiTester.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDataSourceEnvironments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Variables_DataSources_DataSourceId",
                table: "Variables");

            migrationBuilder.RenameColumn(
                name: "DataSourceId",
                table: "Variables",
                newName: "DataSourceEnvironmentId");

            migrationBuilder.RenameIndex(
                name: "IX_Variables_DataSourceId_Key",
                table: "Variables",
                newName: "IX_Variables_DataSourceEnvironmentId_Key");

            migrationBuilder.CreateTable(
                name: "DataSourceEnvironments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DataSourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, collation: "SQL_Latin1_General_CP1_CI_AS"),
                    BaseUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataSourceEnvironments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DataSourceEnvironments_DataSources_DataSourceId",
                        column: x => x.DataSourceId,
                        principalTable: "DataSources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DataSourceEnvironments_DataSourceId_Name",
                table: "DataSourceEnvironments",
                columns: new[] { "DataSourceId", "Name" },
                unique: true);

            migrationBuilder.Sql(
                """
                INSERT INTO [DataSourceEnvironments] ([Id], [DataSourceId], [Name], [BaseUrl], [IsActive])
                SELECT NEWID(), [Id], N'Default', [BaseUrl], CAST(1 AS bit)
                FROM [DataSources];
                """);

            migrationBuilder.Sql(
                """
                UPDATE [Variables]
                SET [DataSourceEnvironmentId] = [environments].[Id]
                FROM [Variables]
                INNER JOIN [DataSourceEnvironments] AS [environments]
                    ON [environments].[DataSourceId] = [Variables].[DataSourceEnvironmentId]
                   AND [environments].[Name] = N'Default';
                """);

            migrationBuilder.Sql(
                """
                IF EXISTS (
                    SELECT 1
                    FROM [Variables] AS [variables]
                    LEFT JOIN [DataSourceEnvironments] AS [environments]
                        ON [environments].[Id] = [variables].[DataSourceEnvironmentId]
                    WHERE [environments].[Id] IS NULL)
                BEGIN
                    THROW 51000, 'Failed to map one or more existing variables to a migrated default environment.', 1;
                END
                """);

            migrationBuilder.AddForeignKey(
                name: "FK_Variables_DataSourceEnvironments_DataSourceEnvironmentId",
                table: "Variables",
                column: "DataSourceEnvironmentId",
                principalTable: "DataSourceEnvironments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.DropColumn(
                name: "BaseUrl",
                table: "DataSources");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Variables_DataSourceEnvironments_DataSourceEnvironmentId",
                table: "Variables");

            migrationBuilder.AddColumn<string>(
                name: "BaseUrl",
                table: "DataSources",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(
                """
                UPDATE [dataSources]
                SET [BaseUrl] = COALESCE([defaultEnvironment].[BaseUrl], [anyEnvironment].[BaseUrl], N'')
                FROM [DataSources] AS [dataSources]
                OUTER APPLY (
                    SELECT TOP(1) [BaseUrl]
                    FROM [DataSourceEnvironments]
                    WHERE [DataSourceId] = [dataSources].[Id]
                      AND [Name] = N'Default') AS [defaultEnvironment]
                OUTER APPLY (
                    SELECT TOP(1) [BaseUrl]
                    FROM [DataSourceEnvironments]
                    WHERE [DataSourceId] = [dataSources].[Id]
                    ORDER BY [Name]) AS [anyEnvironment];
                """);

            migrationBuilder.Sql(
                """
                UPDATE [Variables]
                SET [DataSourceEnvironmentId] = [environments].[DataSourceId]
                FROM [Variables]
                INNER JOIN [DataSourceEnvironments] AS [environments]
                    ON [environments].[Id] = [Variables].[DataSourceEnvironmentId];
                """);

            migrationBuilder.DropTable(
                name: "DataSourceEnvironments");

            migrationBuilder.RenameColumn(
                name: "DataSourceEnvironmentId",
                table: "Variables",
                newName: "DataSourceId");

            migrationBuilder.RenameIndex(
                name: "IX_Variables_DataSourceEnvironmentId_Key",
                table: "Variables",
                newName: "IX_Variables_DataSourceId_Key");

            migrationBuilder.AddForeignKey(
                name: "FK_Variables_DataSources_DataSourceId",
                table: "Variables",
                column: "DataSourceId",
                principalTable: "DataSources",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
