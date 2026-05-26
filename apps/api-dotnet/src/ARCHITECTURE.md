# Architecture Guide

This document explains the Clean Architecture and Vertical Slice concepts used in this solution and how they map to the codebase.

## Clean Architecture

Clean Architecture enforces dependency direction so that business rules stay independent of infrastructure concerns.

### Layers and Dependencies

The project uses the following layers (outer depends on inner):

- WebApi -> Infrastructure -> Application -> Domain

Rules:

- Domain: Entities and core business rules. No dependencies on any other layer.
- Application: Use cases (features). Depends only on Domain.
- Infrastructure: Database and external integrations. Depends on Application and Domain.
- WebApi: Entry point, controllers, and wiring. Depends on Infrastructure and Application.

### Key Benefits

- Business rules are isolated from frameworks and storage.
- The core remains stable as external details change.
- Testing is easier because handlers and domain logic are framework-agnostic.

### Enforcement Guidelines

- Domain should not reference EF Core, ASP.NET, or any infrastructure types.
- Application handlers must use abstractions or DbContext interfaces, not web concerns.
- Infrastructure implements persistence and integrations behind abstractions used by Application.
- WebApi only wires controllers, DI, middleware, and cross-cutting concerns.

## Vertical Slice Architecture

Vertical Slices group all code for a feature together so that change is localized.

### Slice Structure

Each feature lives in its own folder under Application/Features and contains:

- Request/response records
- Handler that implements IHandler<TRequest, TResponse>
- FluentValidation validator

The corresponding **controller** lives in WebApi/Controllers/ and delegates to the handler via MediatR.

Example:

Application/Features/Post/CreatePost/

- CreatePostRequest.cs
- CreatePostResponse.cs
- CreatePostHandler.cs
- CreatePostValidator.cs

WebApi/Controllers/

- PostsController.cs  (thin — calls MediatR, maps HTTP status codes)

### Why It Matters

- Features are self-contained and discoverable.
- Less cross-feature coupling and fewer shared files.
- Easier to reason about change impact and testing scope.

### Slice Rules

- All business logic stays in the handler, not in controllers.
- The controller maps HTTP to the request record and dispatches via MediatR.
- Validation is defined inside the same feature folder.
- DTOs are records and should live with the feature.

## CQRS in This Codebase

Commands and Queries are separate:

- Commands modify state: Create, Update, Delete
- Queries read state: GetAll, GetById

Handlers return Result<T> so failures are explicit and consistent.

## Controller Conventions

- All controllers inherit `ControllerBase` and use `[ApiController]`.
- Route: `[Route("api/[controller]")]` — produces `api/posts`, `api/auth`, etc.
- Use `[HttpGet]`, `[HttpPost]`, `[HttpPut]`, `[HttpDelete]` attributes.
- Return `IActionResult` or `ActionResult<T>`.
- Keep controllers thin: validate → dispatch to MediatR → map result to HTTP response.

## Recommended Flow for New Features

1. Create a feature folder under Application/Features/<FeatureName>/<ActionName>.
2. Add request/response records, handler, and validator.
3. Create or update the controller in WebApi/Controllers/.
4. Keep controllers thin and map to HTTP status codes.
5. Use async EF Core calls from handlers.
6. Return Result<T> from handlers for success and failure.

