# Architecture Guide

This document explains the Clean Architecture concepts used in this solution and how they map to the codebase.

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
- Application handlers must use abstractions or repository interfaces, not web concerns.
- Application defines repository interfaces; Infrastructure provides EF Core implementations.
- Infrastructure implements persistence and integrations behind abstractions used by Application.
- WebApi only wires controllers, DI, middleware, and cross-cutting concerns.

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
