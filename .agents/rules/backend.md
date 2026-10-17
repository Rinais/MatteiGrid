# Backend Development Rules (.NET 8 Modular Monolith)

## 1. Architecture & Clean Architecture Principles
- **Clean Architecture inside each module**:
  - `Domain/`: Entities, Value Objects, Domain Events, Enums. No external dependencies.
  - `Application/` or `Contracts/`: DTOs, interfaces, commands, queries, validators.
  - `Services/` or `Infrastructure/`: Implementation of module interfaces, persistence mappings.
- **Root Startup**: `MatteiGrid.Api` should only register modules and configure ASP.NET Core pipelines. It must not contain business domain logic.

## 2. Multi-Tenancy Invariants
- Every database entity representing clinical, financial, or organizational data MUST implement or have `ClinicId` (Guid).
- Never allow client HTTP requests to specify or override `ClinicId` for an operation; resolve `ClinicId` solely from `ICurrentTenantContext` (derived from validated JWT claims).
- Use `TenantSaveChangesInterceptor` to automatically assign `ClinicId` to newly added entities.
- Enforce global query filters in EF Core `OnModelCreating` to ensure soft deletion and tenant segregation:
  ```csharp
  builder.HasQueryFilter(e => e.ClinicId == currentTenantContext.ClinicId);
  ```

## 3. Inter-Module Boundaries
- Modules must be loosely coupled.
- Direct database joins across module tables are prohibited. If Module A needs data from Module B, consume `I{ModuleB}Module` contract or publish an integration event.
- Keep `MatteiGrid.ArchitectureTests` green at all times.

## 4. Error Handling & Validation
- Use the `Result` or `Result<T>` pattern from `MatteiGrid.Shared.Kernel` for domain logic outcomes rather than throwing raw business exceptions.
- HTTP responses: Return standard ProblemDetails (`Results.Problem()`) or appropriate typed status codes (`Results.BadRequest()`, `Results.NotFound()`, `Results.Unauthorized()`, `Results.Forbid()`).

## 5. Persistence & Migrations
- Use PostgreSQL-specific migrations in `MatteiGrid.Shared.Infrastructure`.
- Ensure appropriate indexes on `(ClinicId, Id)` and high-query filter columns.
- Ensure decimal financial fields specify precision (e.g., `decimal(18,2)`).
