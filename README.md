# SimpleApiTester

## Purpose
SimpleApiTester is a focused ASP.NET Core Web API for storing reusable API call definitions and executing them on demand.

It intentionally supports a narrow workflow: define a data source, create one or more environments under that data source, define operations once, configure headers and query parameters, store environment-specific variables, then execute the same operation against an explicitly selected environment.

SimpleApiTester is intentionally not a full Postman replacement.

## Architecture
- `SimpleApiTester.Domain` - entities and enums.
- `SimpleApiTester.Application` - CQRS commands/queries, MediatR handlers, validation, and execution orchestration.
- `SimpleApiTester.Infrastructure` - EF Core persistence, SQL Server migrations, external value resolution, and HTTP execution.
- `SimpleApiTester.API` - Web API endpoints, Swagger, and global exception handling.
- `SimpleApiTester.Tests` - unit and integration tests.

## Tech Stack
- .NET 10
- ASP.NET Core Web API
- EF Core / SQL Server
- CQRS + MediatR
- FluentValidation
- `IHttpClientFactory`
- Swagger / OpenAPI
- xUnit

## V2 Environment Model

### DataSources
- CRUD support
- unique `Key`
- `IsActive`
- no `BaseUrl`
- own `Operations`, `Headers`, and `Environments`

### DataSourceEnvironments
- CRUD support
- belong to a `DataSource`
- `Name`
- `BaseUrl`
- `IsActive`
- case-insensitive unique `Name` per `DataSource`
- no default-environment flag or automatic selection

### Operations
- CRUD support
- linked to a `DataSource`
- created once and reused across environments
- `ApiName`
- relative `Endpoint`
- `HttpMethodType`
- optional `Body`
- optional `ContentType`

### QueryParameters
- CRUD support
- linked to an `Operation`
- stored separately from `Endpoint`
- `Key`
- `Value`
- `IsEnabled`
- URI-encoded during execution

### Variables
- belong to a `DataSourceEnvironment`
- `Key`
- `Value`
- `IsEnabled`
- `IsSecret`
- case-insensitive unique key per `DataSourceEnvironment`
- disabled variables remain stored and listable
- used only for header value resolution
- secret variables are masked as `********` in read responses
- secret masking is presentation-only and does not encrypt values at rest
- `Value = null` on update preserves the existing stored value
- sending `"********"` explicitly stores that literal value

### Headers
- DataSource-level headers
- Operation-level headers
- immutable scope after creation
- `Key`
- `ValueSourceType`
- `Value`
- `SourceKey`
- `IsEnabled`
- case-insensitive uniqueness within the same scope
- operation-level enabled header overrides matching data-source enabled header
- disabled operation-level header does not suppress the enabled data-source header
- environment headers do not exist

### Header Value Sources
- `General` - literal `Value`
- `Variable` - resolve from an enabled variable in the selected `DataSourceEnvironment`
- `UserSecret` - resolve through `IConfiguration`
- `EnvironmentVariable` - resolve through `Environment.GetEnvironmentVariable(...)`

### Reserved Headers
The following custom headers cannot be configured through the header endpoints:
- `Content-Type`
- `Content-Length`
- `Host`
- `Transfer-Encoding`

`Authorization` is allowed as a normal custom header.

## Execution Behavior
When an operation is executed, the application:
- loads the `Operation`
- loads the related `DataSource`
- loads the selected `DataSourceEnvironment`
- verifies the selected environment belongs to the operation's data source
- rejects execution if the `DataSource` is inactive
- rejects execution if the selected `DataSourceEnvironment` is inactive
- loads enabled query parameters
- builds the final request URI from `selectedEnvironment.BaseUrl + operation.Endpoint + enabled query parameters`
- loads enabled data-source and operation headers
- merges headers case-insensitively with operation-level override
- resolves variable-backed headers from the selected environment only
- uses the real stored variable value even when the variable is marked secret
- sends the request through `IHttpClientFactory`
- returns remote HTTP responses, including non-2xx results, as normal execution results
- returns transport failures and header resolution failures as execution errors

If header resolution fails, the outbound HTTP call is not sent and the execution result reports:
- `HasExecutionError = true`
- `ErrorType = "HeaderResolutionError"`

`ContentType` belongs to the `Operation`, not to headers.

Environment selection is required for every execution. There is no hidden default selection.

## V1 Migration Compatibility
Existing V1 databases are migrated safely by creating one compatibility environment per existing data source:
- `Name = "Default"`
- `BaseUrl = previous DataSource.BaseUrl`
- `IsActive = true`

Existing variables are moved into that compatibility environment, and existing variable-backed headers keep working because they still resolve by `SourceKey`.

## Secret Variables
Variables can be marked with `IsSecret = true` when their values should not be exposed through read APIs.

- read responses return `Value = ********` for secret variables
- execution still uses the real stored value
- `IsSecret = false` returns the real value in read responses
- this feature does not provide encryption at rest

## Database Setup
Update `SimpleApiTester.API/appsettings.json` with a SQL Server connection string for `ConnectionStrings:DefaultConnection`.

Example:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOURSERVER;Database=SimpleApiTesterDb;Trusted_Connection=True;TrustServerCertificate=True"
  }
}
```

## Running the API
```bash
dotnet restore
dotnet build "SimpleApiTester.slnx"
dotnet run --project "SimpleApiTester.API"
```

Swagger UI is available in Development at `/swagger`, and `/` redirects there.

## Database Migrations
Apply the current migrations:

```bash
dotnet ef database update --project "SimpleApiTester.Infrastructure" --startup-project "SimpleApiTester.API"
```

Create a new migration:

```bash
dotnet ef migrations add <MigrationName> --project "SimpleApiTester.Infrastructure" --startup-project "SimpleApiTester.API"
```

## Testing
```bash
dotnet test "SimpleApiTester.slnx" -v minimal
```

## Example Workflow
1. Create a `DataSource`.
2. Create a `Development` environment under that data source.
3. Create a `Production` environment under that data source.
4. Create an `Operation` once under the data source.
5. Add optional query parameters.
6. Add environment-specific variables under each environment.
7. Add optional data-source headers.
8. Add optional operation headers.
9. Execute the same operation with `Development` using its `environmentId`.
10. Execute the same operation with `Production` using its `environmentId`.

## Main API Endpoints
- `POST /api/data-sources`
- `GET /api/data-sources`
- `GET /api/data-sources/{id}`
- `PUT /api/data-sources/{id}`
- `DELETE /api/data-sources/{id}`
- `POST /api/data-sources/{dataSourceId}/environments`
- `GET /api/data-sources/{dataSourceId}/environments`
- `GET /api/environments/{id}`
- `PUT /api/environments/{id}`
- `DELETE /api/environments/{id}`
- `POST /api/data-sources/{dataSourceId}/operations`
- `GET /api/data-sources/{dataSourceId}/operations`
- `GET /api/operations/{id}`
- `PUT /api/operations/{id}`
- `DELETE /api/operations/{id}`
- `POST /api/operations/{id}/execute?environmentId={environmentId}`
- `POST /api/operations/{operationId}/query-parameters`
- `GET /api/operations/{operationId}/query-parameters`
- `PUT /api/query-parameters/{id}`
- `DELETE /api/query-parameters/{id}`
- `POST /api/environments/{environmentId}/variables`
- `GET /api/environments/{environmentId}/variables`
- `PUT /api/variables/{id}`
- `DELETE /api/variables/{id}`
- `POST /api/data-sources/{dataSourceId}/headers`
- `GET /api/data-sources/{dataSourceId}/headers`
- `POST /api/operations/{operationId}/headers`
- `GET /api/operations/{operationId}/headers`
- `PUT /api/headers/{id}`
- `DELETE /api/headers/{id}`

## Known Non-Goals
This project intentionally does not include:
- environment headers
- data-source-level variables
- default environment selection
- variable substitution in endpoints, bodies, or query parameters
- auth frameworks or OAuth flows
- request history
- collections/workspaces
- UI
- retry policies
- file upload helpers
- request scripting
- broad Postman-style feature parity
