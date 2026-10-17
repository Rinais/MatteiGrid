---
name: update-docs
description: Audits the codebase against docs/*.md, identifies discrepancies, and updates documentation files to match working code.
---

# Update Documentation Workflow

Use this skill periodically or before merging changes to ensure project documentation never drifts away from the actual implementation.

## Step 1: Scan and Audit
Inspect the active code and compare against documentation files:
1. **API Audit**:
   - Check `backend/src/MatteiGrid.Api/Endpoints/ApiEndpoints.cs` and `SignalRHubs.cs`.
   - Compare all routes, HTTP methods, authorization requirements, and parameter types against `docs/API.md`.
2. **Database Audit**:
   - Check entity configurations in `backend/src/MatteiGrid.Modules.*/Domain/` and DbContext in `MatteiGrid.Shared.Infrastructure`.
   - Compare tables, columns, indexes, and foreign keys against `docs/DATABASE.md`.
3. **Architecture Audit**:
   - Check registered modules, services, dependency injections, and frontend routing in `frontend/src/app/app.routes.ts`.
   - Compare component boundaries, modules, and flows against `docs/ARCHITECTURE.md`.
4. **Known Issues Audit**:
   - Check `docs/KNOWN_ISSUES.md` against completed tasks or newly discovered bugs in the skeleton.

## Step 2: Report Discrepancies
List every detected discrepancy:
- Missing endpoints or payload mismatches
- Missing database tables or column mismatches
- Architectural changes not captured in docs
- Resolved issues still marked open

## Step 3: Apply Documentation Updates
- Update `docs/API.md`, `docs/DATABASE.md`, `docs/ARCHITECTURE.md`, or `docs/KNOWN_ISSUES.md` with accurate, up-to-date details.
- Present a clear diff summary of what documentation was corrected.
