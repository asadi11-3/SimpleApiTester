# SimpleApiTester

## Purpose
Small API testing backend that stores API sources and operations and executes configured HTTP requests.

## Architecture
- `SimpleApiTester.Domain` - core entities and enums.
- `SimpleApiTester.Application` - CQRS handlers, validation, execution orchestration, and abstractions.
- `SimpleApiTester.Infrastructure` - EF Core persistence, migrations, and HTTP execution implementation.
- `SimpleApiTester.API` - ASP.NET Core Web API, controllers, Swagger, and global exception handling.
- `SimpleApiTester.Tests` - focused unit and integration tests.

## Features
- DataSource CRUD
- Operation CRUD
- QueryParameter CRUD
- GET/POST/PUT/PATCH/DELETE execution
- request body
- configurable content type
- query parameters
- execution result status/body/duration
- remote non-2xx handling

Request Headers are intentionally outside current scope.

## Tech Stack
- .NET 10
- ASP.NET Core Web API
- EF Core / SQL Server
- MediatR
- FluentValidation
- `IHttpClientFactory`
- xUnit

## Database Setup
Update the `DefaultConnection` string in `SimpleApiTester.API/appsettings.json` to point to your SQL Server instance.

## Running the project
```bash
dotnet restore
dotnet build "SimpleApiTester.slnx"
dotnet run --project "SimpleApiTester.API"
```

Swagger UI is available in development at `/swagger`, and `/` redirects there.

## EF migration commands
```bash
dotnet ef migrations add <MigrationName> --project "SimpleApiTester.Infrastructure" --startup-project "SimpleApiTester.API"
dotnet ef database update --project "SimpleApiTester.Infrastructure" --startup-project "SimpleApiTester.API"
```

## Main API endpoints
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

## Example workflow
1. Create a DataSource.
2. Create an Operation under that DataSource.
3. Add a QueryParameter to the Operation.
4. Execute the Operation.

## Testing
```bash
dotnet test "SimpleApiTester.slnx"
```

## Scope / Non-goals
SimpleApiTester is intentionally not a full Postman clone.

Current non-goals include request headers, auth systems, environments, variable substitution, collections, request history, retries, file upload, and UI.
