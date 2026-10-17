---
name: add-feature
description: Structured workflow for designing and adding new features across backend modules, database, and frontend components.
---

# Add Feature Workflow

Follow this end-to-end workflow when designing and introducing a new feature to Mattei Grid:

## Step 1: Requirements & Module Scoping
1. Read the feature requirement and cross-reference with `docs/PRD.md` and `docs/ARCHITECTURE.md`.
2. Identify which bounded context / module owns the data and logic (e.g., Clinical, Prescriptions, Laboratory, Billing).
3. If new cross-module data exchange is needed, define the interface contract in `src/MatteiGrid.Modules.<Module>/Contracts/`.

## Step 2: Database & Domain Layer
1. Add/modify entities inside the module's `Domain/` folder.
2. Ensure tenant-owned entities include `ClinicId`.
3. If new DB migrations are required, generate them in `MatteiGrid.Shared.Infrastructure`.
4. Update `docs/DATABASE.md` with new tables, foreign keys, or constraints.

## Step 3: Application / API Layer
1. Implement the service logic and register it in module configuration.
2. Expose Minimal API endpoints under `/api/v1/<feature>` in `MatteiGrid.Api/Endpoints/ApiEndpoints.cs`.
3. Apply appropriate authorization policies (`[RequirePermission(...)]` or role checks).
4. Update `docs/API.md` with endpoints, request/response models, and roles.

## Step 4: Frontend Implementation
1. Add typed interfaces in `frontend/src/app/core/models.ts` or feature-specific model files.
2. Add API client methods in `frontend/src/app/core/api.service.ts`.
3. Build the UI using standalone Angular 19 components with reactive Signals and Tailwind CSS.
4. Ensure error handling and loading indicators are present.

## Step 5: Verification & Documentation Sync
1. Run backend tests:
   ```powershell
   dotnet test backend/MatteiGrid.sln
   ```
2. Verify architecture boundaries:
   ```powershell
   dotnet test backend/tests/MatteiGrid.ArchitectureTests/MatteiGrid.ArchitectureTests.csproj
   ```
3. Update `docs/ARCHITECTURE.md` if any new services or components were created.
4. Review git diff and present a summary of changes.
