# Mattei Grid — Known Issues & Bug Backlog

This backlog tracks known bugs, architectural gaps, and technical debt in the skeleton.  
*Rule: When resolving an issue, use the `/fix-bug` skill and check it off here.*

---

## 1. High Priority / Security

- [ ] **[SEC-01] Hardcoded Credentials in `appsettings.json`**
  - **Location**: `backend/src/MatteiGrid.Api/appsettings.json`
  - **Issue**: Database password and JWT signing key are currently hardcoded in source control.
  - **Remediation**: Extract sensitive connection strings and secrets to `.env`, user-secrets (`dotnet user-secrets`), or environment variables. Provide generic placeholders in `appsettings.json`.

- [ ] **[SEC-02] Razorpay Webhook Signature Verification**
  - **Location**: `backend/src/MatteiGrid.Api/Endpoints/ApiEndpoints.cs` (`/razorpay-webhook`)
  - **Issue**: Endpoint accepts payload without validating HMAC SHA256 webhook signature header from Razorpay.
  - **Remediation**: Implement `X-Razorpay-Signature` validation using `Razorpay__WebhookSecret` before acknowledging payment.

---

## 2. Backend & Database

- [ ] **[DB-01] Database Migration Verification**
  - **Location**: `backend/src/MatteiGrid.Shared.Infrastructure/Persistence/Migrations/`
  - **Issue**: Ensure initial PostgreSQL migrations match the full ERD v4 schema and composite unique constraints `(ClinicId, Id)`.

- [ ] **[BE-01] Prescription Revision Version Guard**
  - **Location**: `backend/src/MatteiGrid.Modules.Prescriptions/Services/PrescriptionService.cs`
  - **Issue**: Verify that revising a prescription enforces version monotonicity (`Version = PreviousVersion.Version + 1`) and locks past finalized prescriptions from mutation.

- [ ] **[BE-02] Inventory Stock Deduction Concurrency**
  - **Location**: `backend/src/MatteiGrid.Modules.Inventory/Services/InventoryService.cs`
  - **Issue**: Concurrent lab order completions could trigger race conditions on `QuantityDelta` in `InventoryTransaction`.
  - **Remediation**: Ensure pessimistic or optimistic concurrency tokens (`xmin` in PostgreSQL) on inventory balance updates.

---

## 3. Frontend & Real-Time

- [ ] **[FE-01] SignalR Hub Automatic Reconnection**
  - **Location**: `frontend/src/app/core/` (SignalR connection services)
  - **Issue**: If the connection drops, Angular components do not automatically re-subscribe to clinic groups `clinic-{id}:lab`.
  - **Remediation**: Configure `.withAutomaticReconnect()` and re-join tenant groups upon reconnection.

- [ ] **[FE-02] Replace Static Mock Data in Clinical / Lab Views**
  - **Location**: `frontend/src/app/features/`
  - **Issue**: Certain views still rely on placeholder stub arrays rather than live calls to `api.service.ts`.
  - **Remediation**: Bind all UI tables and forms to live endpoints documented in `docs/API.md`.

- [ ] **[FE-03] Global HTTP 401 Interceptor**
  - **Location**: `frontend/src/app/core/`
  - **Issue**: Expired access tokens should trigger a silent refresh via `/api/v1/auth/refresh` before redirecting the user to `/login`.
