# SimpleApiTester

## Purpose
SimpleApiTester is a small ASP.NET Core Web API for storing reusable API call definitions and executing them on demand.

It is intentionally focused on a narrow V1 workflow: define a data source, define operations under that data source, attach query parameters, variables, and headers, then execute the configured HTTP request.

Variables currently support header resolution only.

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

## V1 Scope

### DataSources
- CRUD support
- unique `Key`
- `BaseUrl`
- `IsActive`

### Operations
- CRUD support
- linked to a `DataSource`
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
- belong to a `DataSource`
- `Key`
- `Value`
- `IsEnabled`
- case-insensitive unique key per `DataSource`
- used only for header value resolution in V1

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

### Header Value Sources
- `General` - literal `Value`
- `Variable` - resolve from an enabled variable in the same `DataSource`
- `UserSecret` - resolve through `IConfiguration`
- `EnvironmentVariable` - resolve through `Environment.GetEnvironmentVariable(...)`

### Reserved Headers
The following custom headers cannot be configured through the header endpoints:
- `Content-Type`
- `Content-Length`
- `Host`
- `Transfer-Encoding`

`Authorization` is allowed as a normal raw header.

## Execution Behavior
When an operation is executed, the application:
- loads the `Operation`
- loads the related `DataSource`
- rejects execution if the `DataSource` is inactive
- loads enabled query parameters
- builds the final request URI
- loads enabled data-source and operation headers
- merges headers case-insensitively
- resolves header values before the outbound call
- sends the request through `IHttpClientFactory`
- returns remote HTTP responses, including non-2xx results, as normal execution results
- returns transport failures and header resolution failures as execution errors

If header resolution fails, the outbound HTTP call is not sent and the execution result reports:
- `HasExecutionError = true`
- `ErrorType = "HeaderResolutionError"`

`ContentType` belongs to the `Operation`, not to headers.

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

## Typical Workflow
1. Create a `DataSource` with a base URL.
2. Create an `Operation` under that data source.
3. Add optional query parameters.
4. Add optional variables.
5. Add optional data-source headers.
6. Add optional operation headers.
7. Execute the operation.

## Main API Endpoints
- `POST /api/data-sources`
- `GET /api/data-sources`
- `GET /api/data-sources/{id}`
- `PUT /api/data-sources/{id}`
- `DELETE /api/data-sources/{id}`
- `POST /api/data-sources/{dataSourceId}/operations`
- `GET /api/data-sources/{dataSourceId}/operations`
- `GET /api/operations/{id}`
- `PUT /api/operations/{id}`
- `DELETE /api/operations/{id}`
- `POST /api/operations/{id}/execute`
- `POST /api/operations/{operationId}/query-parameters`
- `GET /api/operations/{operationId}/query-parameters`
- `PUT /api/query-parameters/{id}`
- `DELETE /api/query-parameters/{id}`
- `POST /api/data-sources/{dataSourceId}/variables`
- `GET /api/data-sources/{dataSourceId}/variables`
- `PUT /api/variables/{id}`
- `DELETE /api/variables/{id}`
- `POST /api/data-sources/{dataSourceId}/headers`
- `GET /api/data-sources/{dataSourceId}/headers`
- `POST /api/operations/{operationId}/headers`
- `GET /api/operations/{operationId}/headers`
- `PUT /api/headers/{id}`
- `DELETE /api/headers/{id}`

## Known Non-Goals
V1 intentionally does not include:
- collections/workspaces
- request history
- UI
- retry policies
- file upload helpers
- advanced auth flows
- request scripting
- broad variable substitution outside header resolution
- Postman-style feature parity
