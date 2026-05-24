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
- WebApi: Entry point and wiring. Depends on Infrastructure and Application.

### Key Benefits

- Business rules are isolated from frameworks and storage.
- The core remains stable as external details change.
- Testing is easier because handlers and domain logic are framework-agnostic.

### Enforcement Guidelines

- Domain should not reference EF Core, ASP.NET, or any infrastructure types.
- Application handlers must use abstractions or DbContext interfaces, not web concerns.
- Infrastructure implements persistence and integrations behind abstractions used by Application.
- WebApi only wires endpoints, DI, middleware, and cross-cutting concerns.

## Vertical Slice Architecture

Vertical Slices group all code for a feature together so that change is localized.

### Slice Structure

Each feature lives in its own folder under Application/Features and contains:

- Request/response records
- Handler that implements IHandler<TRequest, TResponse>
- FluentValidation validator
- Endpoint definition

Example:

Application/Features/Post/CreatePost/

- CreatePostRequest.cs
- CreatePostResponse.cs
- CreatePostHandler.cs
- CreatePostValidator.cs
- CreatePostEndpoint.cs

### Why It Matters

- Features are self-contained and discoverable.
- Less cross-feature coupling and fewer shared files.
- Easier to reason about change impact and testing scope.

### Slice Rules

- All business logic stays in the handler, not in endpoints.
- The endpoint maps HTTP to the request record and calls the handler.
- Validation is defined inside the same feature folder.
- DTOs are records and should live with the feature.

## CQRS in This Codebase

Commands and Queries are separate:

- Commands modify state: Create, Update, Delete
- Queries read state: GetAll, GetById

Handlers return Result<T> so failures are explicit and consistent.

## Recommended Flow for New Features

1. Create a feature folder under Application/Features/<FeatureName>/<ActionName>.
2. Add request/response records, handler, validator, and endpoint.
3. Keep endpoints thin and map to HTTP status codes.
4. Use async EF Core calls from handlers.
5. Return Result<T> from handlers for success and failure.
