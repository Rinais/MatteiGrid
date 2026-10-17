# Mattei Grid - Agent Guidelines & Engineering Standards

Mattei Grid is an enterprise multi-tenant B2B clinical management platform tailored for Electro-Homoeopathy practices.

---

## 1. High-Level Architecture & Tech Stack

- **Backend**: ASP.NET Core (.NET 8) Modular Monolith following Clean Architecture principles.
  - Startup / Presentation: `src/MatteiGrid.Api` (Minimal APIs, SignalR hubs, JWT Auth).
  - Business Modules: `src/MatteiGrid.Modules.*` (Independent bounded contexts: Clinical, Prescriptions, Laboratory, Inventory, Billing, Patients, MateriaMedica, Appointments, Administration, Clinic, Reports, Notifications, Identity).
  - Shared: `MatteiGrid.Shared.Kernel` (Domain primitives, Result pattern, Permissions) and `MatteiGrid.Shared.Infrastructure` (DbContext, EF Interceptors, Tenancy, Auditing).
  - Database: PostgreSQL with EF Core.
- **Frontend**: Angular 19 SPA with standalone components, Signals, and Tailwind CSS.
- **Real-Time Communication**: ASP.NET Core SignalR (`/hubs/lab-queue`, `/hubs/consultation`).

---

## 2. Invariant Rules & Security Guardrails

### Multi-Tenancy (Strict Rule - Tenancy Model A)
1. **ClinicId Is Mandatory**: Every tenant-owned entity MUST have a `ClinicId` (Guid).
2. **Never Query Cross-Tenant**: All EF Core queries must utilize global query filters or enforce `WHERE ClinicId = @currentTenant`.
3. **Save Interceptor**: New entities must have `ClinicId` stamped via `TenantSaveChangesInterceptor`. Never allow client requests to override or spoof `ClinicId`.
4. **Composite Keys**: Composite unique constraints and foreign keys `(ClinicId, Id)` must be respected for cross-entity references (e.g., Medicine, Dilution, Vehicle).

### Module Independence
1. **No Direct Module DbContext Access**: A module must NEVER directly query another module's database tables or internal services.
2. **Contract-Based Communication**: Inter-module communication must go through public contracts (`I<Module>Module`) or domain events.
3. **Architecture Tests**: Do not violate boundary rules defined in `backend/tests/MatteiGrid.ArchitectureTests`.

### Secrets & Configuration
1. **Never Hardcode Secrets**: No database passwords, JWT signing keys, or payment gateway secrets in source code or committed JSON files.
2. Use `.env` or user secrets locally, referenced through configuration abstractions (`IConfiguration`).

---

## 3. Mandatory Definition of Done (DoD)

Before marking any task, bugfix, or feature as complete, the agent must ensure:
1. **Tests Pass**: Run relevant tests (`dotnet test backend/MatteiGrid.sln`, frontend specs if applicable).
2. **API Documentation Sync**: If any endpoint, route, or request/response payload is modified or added, update [`docs/API.md`](file:///C:/SaaS/AntiGravity/MatteiGrid/docs/API.md).
3. **Database Schema Sync**: If any entity, relationship, or migration is created or altered, update [`docs/DATABASE.md`](file:///C:/SaaS/AntiGravity/MatteiGrid/docs/DATABASE.md).
4. **Architecture Sync**: If module boundaries, services, or system flows change, update [`docs/ARCHITECTURE.md`](file:///C:/SaaS/AntiGravity/MatteiGrid/docs/ARCHITECTURE.md).
5. **Issue Backlog Sync**: If addressing an issue listed in [`docs/KNOWN_ISSUES.md`](file:///C:/SaaS/AntiGravity/MatteiGrid/docs/KNOWN_ISSUES.md), mark it resolved or remove it.
6. **Summary of Changes**: Always provide a file-by-file summary of changed code and docs with rationale.
