# CLAUDE.md

This repo is a .NET 8 REST API using Clean Architecture, Vertical Slice Architecture, CQRS, Controllers, FluentValidation, EF Core, and PostgreSQL.

For deeper architectural guidance, see [ARCHITECTURE.md](ARCHITECTURE.md).

## Tech Stack

- Runtime: .NET 8, ASP.NET Core Controllers (attribute-routed)
- Database: PostgreSQL, EF Core, Npgsql
- Validation: FluentValidation
- Docs/Tools: Swagger UI, Health Checks, Logging Decorators, EF Core Audit Interceptor

## Architecture Rules

- Layers: WebApi -> Infrastructure -> Application -> Domain
- Domain: entities + business rules only; no dependencies outward
- Application: vertical slices and CQRS handlers; depends only on Domain
- Infrastructure: persistence/external integrations; depends on Application + Domain
- WebApi: controllers, DI, middleware, composition only

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

- Use attribute-routed Controllers (`[ApiController]`, `[Route("api/[controller]")]`)
- Controllers are thin and delegate to MediatR handlers
- Return correct HTTP status codes via `Ok()`, `Created()`, `NotFound()`, etc.
- Group endpoints by domain concern (e.g., `AuthController`, `ParcelsController`, `ReportsController`)

## Database + Operations

- Connection string: appsettings.json
- Migrations: `dotnet ef migrations add MigrationName -p Planora.Infrastructure -s Planora.Api`
- Update DB: `dotnet ef database update -p Planora.Infrastructure -s Planora.Api`

## Health + Docs

- Health: /health
- Swagger JSON: /swagger/v1/swagger.json
- Swagger UI: /swagger

## Adding a Feature (Vertical Slice)

1. Create a folder under Application/Features/<Feature>/<Action>.
2. Add handler, validator, and request/response records.
3. Add or update the corresponding controller in WebApi/Controllers/.
4. Keep logic in the handler; keep the controller thin.

## Files Not To Modify

- bin/
- obj/
- Migrations/
- \*.csproj
- appsettings.Production.json

Only modify source code in Domain, Application, Infrastructure, and WebApi.
