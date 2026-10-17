---
name: fix-bug
description: Systematic bug fixing workflow ensuring root-cause analysis, boundary verification, test coverage, and documentation sync.
---

# Fix Bug Workflow

Follow this procedure step-by-step when addressing bugs or regressions in Mattei Grid:

## Step 1: Investigation & Root Cause Identification
1. Review the reported issue against `docs/KNOWN_ISSUES.md`, `docs/ARCHITECTURE.md`, and `docs/API.md`.
2. Inspect the relevant module in `backend/src/` or `frontend/src/app/`.
3. Locate the failing code path and establish the exact root cause.
4. Cite affected file paths and line ranges.

## Step 2: Fix Planning & Invariant Check
1. Formulate a minimal, targeted fix plan that does NOT break module isolation or multi-tenant query filters.
2. Verify:
   - Does this entity require `ClinicId`?
   - Is `TenantSaveChangesInterceptor` respected?
   - Are boundary tests in `MatteiGrid.ArchitectureTests` preserved?
3. If the fix requires database schema changes or API contract adjustments, state them explicitly.

## Step 3: Implement & Test
1. If possible, add or update a failing test first in `backend/tests/` (unit or integration).
2. Apply the smallest sufficient fix in code.
3. Run the test suite:
   ```powershell
   dotnet test backend/MatteiGrid.sln
   ```
4. Verify tests pass without collateral regressions.

## Step 4: Documentation & Backlog Sync
1. If any API endpoint, request DTO, or response model changed, update `docs/API.md`.
2. If database schema or entity relationships changed, update `docs/DATABASE.md`.
3. If high-level component flows changed, update `docs/ARCHITECTURE.md`.
4. If this bug was listed in `docs/KNOWN_ISSUES.md`, mark it as resolved or remove it.
5. Provide a summary of all modified code and doc files with clear rationale.
