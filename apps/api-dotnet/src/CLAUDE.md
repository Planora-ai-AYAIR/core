# CLAUDE.md

This repo is a .NET 10 REST API using Clean Architecture, Vertical Slice Architecture, CQRS, Minimal APIs, FluentValidation, EF Core, and PostgreSQL.

For deeper architectural guidance, see [ARCHITECTURE.md](ARCHITECTURE.md).

## Tech Stack

- Runtime: .NET 10, ASP.NET Core Minimal APIs
- Database: PostgreSQL, EF Core, Npgsql
- Validation: FluentValidation
- Docs/Tools: Scalar OpenAPI UI, Health Checks, Logging Decorators, EF Core Audit Interceptor

## Architecture Rules

- Layers: WebApi -> Infrastructure -> Application -> Domain
- Domain: entities + business rules only; no dependencies outward
- Application: vertical slices and CQRS handlers; depends only on Domain
- Infrastructure: persistence/external integrations; depends on Application + Domain
- WebApi: endpoints, DI, middleware, composition only

## CQRS Rules

- Commands: Create/Update/Delete
- Queries: GetAll/GetById
- Handlers implement `IHandler<TRequest, TResponse>`
- Handlers return `Result<T>` for success/failure

## DTO + Validation Rules

- Requests/responses must be C# records
- Validators live in the feature folder
- Use FluentValidation for all requests

## Entity Rules

- All entities inherit `AuditableEntity`
- `CreatedOn` and `UpdatedOn` are set by the audit interceptor

## Endpoint Rules

- Minimal APIs only
- Endpoints are thin and delegate to handlers
- Return correct HTTP status codes

## Database + Operations

- Connection string: appsettings.json
- Migrations: `dotnet ef migrations add MigrationName -p src/Infrastructure -s src/WebApi`
- Update DB: `dotnet ef database update -p src/Infrastructure -s src/WebApi`

## Health + Docs

- Health: /health
- OpenAPI: /openapi/v1.json
- Scalar UI: /scalar/v1

## Adding a Feature (Vertical Slice)

1. Create a folder under Application/Features/<Feature>/<Action>.
2. Add handler, validator, endpoint, and request/response records.
3. Keep logic in the handler; keep the endpoint thin.

## Files Not To Modify

- bin/
- obj/
- Migrations/
- \*.csproj
- appsettings.Production.json

Only modify source code in Domain, Application, Infrastructure, and WebApi.
