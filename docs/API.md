# Mattei Grid — API & Real-Time Hub Specification

Base URL: `/api/v1`  
Protocol: HTTPS (JSON over REST) & WSS (SignalR)  
Authentication: JWT Bearer Token (`Authorization: Bearer <token>`) + HTTP-Only Cookie (`__Host-mattei-refresh-token`)

---

## 1. Authentication & Session (`/api/v1/auth`)

| Method | Endpoint | Permission / Auth | Description |
|---|---|---|---|
| `POST` | `/api/v1/auth/login` | Anonymous | Authenticates staff with email & password. Issues access JWT and sets refresh cookie. |
| `POST` | `/api/v1/auth/select-tenant` | Anonymous / Partial | Sets active clinic if user has multiple authorized tenants. |
| `POST` | `/api/v1/auth/refresh` | Anonymous / Refresh Token | Exchanges valid refresh token for a new access token. |
| `POST` | `/api/v1/auth/logout` | Authenticated | Revokes refresh token and clears cookie. |
| `GET` | `/api/v1/auth/me` | Authenticated | Returns current authenticated staff profile and permissions. |

---

## 2. Patients (`/api/v1/patients`)

| Method | Endpoint | Permission Required | Description |
|---|---|---|---|
| `GET` | `/api/v1/patients?query={q}` | `Patients.Read` | Search patient records within the tenant clinic. Logs PHI read audit. |
| `GET` | `/api/v1/patients/{id}` | `Patients.Read` | Fetch complete patient details by Guid. |
| `POST` | `/api/v1/patients` | `Patients.Create` | Register a new patient. Checks duplicate phone/email warning. |
| `POST` | `/api/v1/patients/{id}/archive` | `Patients.Archive` | Soft-archive patient record. |
| `POST` | `/api/v1/patients/{id}/erase-pii` | `Patients.ErasePii` | Anonymize PII for DPDP Act 2023 compliance. Logs security event. |

---

## 3. Appointments (`/api/v1/appointments`)

| Method | Endpoint | Permission Required | Description |
|---|---|---|---|
| `GET` | `/api/v1/appointments` | `Appointments.Read` | Filter appointments by date range (`from`, `to`) or `doctorStaffId`. |
| `POST` | `/api/v1/appointments` | `Appointments.Create` | Book appointment; broadcasts `AppointmentBooked` via `DashboardHub`. |
| `PUT` | `/api/v1/appointments/{id}/status`| `Appointments.Update` | Update status (Scheduled, CheckedIn, InConsultation, Completed, Cancelled). |

---

## 4. Consultations & Clinical (`/api/v1/consultations`)

| Method | Endpoint | Permission Required | Description |
|---|---|---|---|
| `POST` | `/api/v1/consultations` | `Consultations.Create` | Start consultation from appointment. |
| `GET` | `/api/v1/consultations/{id}` | `Consultations.Read` | Retrieve consultation record, notes, and vitals. |
| `POST` | `/api/v1/consultations/{id}/notes`| `ClinicalNotes.Edit` | Add or update SOAP clinical note. |
| `POST` | `/api/v1/consultations/{id}/complete`| `Consultations.Edit` | Complete consultation session. |

---

## 5. Materia Medica (`/api/v1/materia-medica`)

| Method | Endpoint | Permission Required | Description |
|---|---|---|---|
| `GET` | `/api/v1/materia-medica?query={q}` | `MateriaMedica.Read` | Search remedy entries, organ affinities, and keynotes. |
| `GET` | `/api/v1/materia-medica/{id}` | `MateriaMedica.Read` | View remedy details and practitioner notes. |
| `POST` | `/api/v1/materia-medica/{id}/notes`| `MateriaMedica.Edit` | Add clinic practitioner note to remedy. |

---

## 6. Prescriptions (`/api/v1/prescriptions`)

| Method | Endpoint | Permission Required | Description |
|---|---|---|---|
| `GET` | `/api/v1/prescriptions/medicines` | `Prescriptions.Read` | List active medicines. |
| `GET` | `/api/v1/prescriptions/dilutions` | `Prescriptions.Read` | List available dilutions (D1–D30, 200C, etc.). |
| `GET` | `/api/v1/prescriptions/vehicles` | `Prescriptions.Read` | List vehicles (Distilled water, Globules, Alcohol drops, Syrup). |
| `GET` | `/api/v1/prescriptions/{id}` | `Prescriptions.Read` | Get prescription details and items. |
| `GET` | `/api/v1/prescriptions/consultation/{id}` | `Prescriptions.Read` | List prescriptions for consultation. |
| `POST` | `/api/v1/prescriptions` | `Prescriptions.Create` | Create a draft prescription with remedy items. |
| `POST` | `/api/v1/prescriptions/revise` | `Prescriptions.Edit` | Create a revised version of an existing prescription. |
| `POST` | `/api/v1/prescriptions/{id}/finalize` | `Prescriptions.Finalize` | Locks prescription, generates Laboratory Order, triggers LabHub alert, and generates draft Invoice. |

---

## 7. Laboratory Queue (`/api/v1/lab`)

| Method | Endpoint | Permission Required | Description |
|---|---|---|---|
| `GET` | `/api/v1/lab/queue?status={status}`| `Laboratory.Read` | Fetch lab orders queue. |
| `POST` | `/api/v1/lab/{id}/transition` | `Laboratory.UpdateStatus` | Transition status (Pending -> Compounding -> Prepared -> Dispensed). Triggers automatic inventory deduction upon completion. |
| `POST` | `/api/v1/lab/{id}/reject` | `Laboratory.Reject` | Reject lab order back to doctor with reason comment. |

---

## 8. Inventory (`/api/v1/inventory`)

| Method | Endpoint | Permission Required | Description |
|---|---|---|---|
| `GET` | `/api/v1/inventory?lowStockOnly={bool}` | `Inventory.Read` | List stock items with current quantities and thresholds. |
| `POST` | `/api/v1/inventory/receive-stock` | `Inventory.Manage` | Record stock receipt batch. |
| `GET` | `/api/v1/inventory/transactions` | `Inventory.Read` | Audit log of inventory stock adjustments and deductions. |

---

## 9. Billing & Invoicing (`/api/v1/billing`)

| Method | Endpoint | Permission Required | Description |
|---|---|---|---|
| `GET` | `/api/v1/billing/invoices/{id}` | `Billing.Read` | Get invoice line items and payment status. |
| `POST` | `/api/v1/billing/invoices/{id}/manual-payment` | `Billing.ManualPayment` | Record cash / UPI manual payment. |
| `POST` | `/api/v1/billing/invoices/{id}/cancel` | `Billing.Cancel` | Cancel unpaid invoice. |
| `POST` | `/api/v1/billing/invoices/{id}/refund` | `Billing.Refund` | Process refund against recorded payment. |
| `POST` | `/api/v1/billing/invoices/{id}/payment-link/regenerate` | `Billing.Create` | Regenerate online payment link. |
| `POST` | `/api/v1/billing/razorpay-webhook` | Anonymous (Signature Verified) | Process Razorpay payment captured callback. |

---

## 10. Real-Time SignalR Hubs

### A. Laboratory Queue Hub: `/hubs/lab-queue`
- **Authentication**: Required (`[Authorize]`)
- **Groups**: `clinic-{clinicId}:lab`
- **Client Events**:
  - `OrderReceived(Guid orderId, Guid prescriptionId)`
  - `OrderStatusChanged(Guid orderId, string newStatus)`
  - `OrderRejected(Guid orderId, string reason)`

### B. Dashboard Hub: `/hubs/dashboard`
- **Authentication**: Required (`[Authorize]`)
- **Groups**: `clinic-{clinicId}`
- **Client Events**:
  - `AppointmentBooked(Guid appointmentId)`
  - `PaymentReceived(Guid invoiceId, decimal amount)`
  - `LowStockAlert(Guid medicineId, decimal remainingQuantity)`

---

## 11. Error Responses & Diagnostic Headers

### 11.1. Request Diagnostics
- Every request is tagged with an opaque `TraceId` (`context.TraceIdentifier`).
- All requests and responses are captured in `logs/matteigrid-requests-yyyyMMdd_{seq}.log` including status code, route, tenant ID (`ClinicId`), and latency (ms).

### 11.2. Standardized RFC 7807 Error Response
When an unhandled exception occurs, `GlobalExceptionHandlingMiddleware` catches the failure, logs a `[FATAL] [SYSTEM_CRASH]` event to `logs/matteigrid-app-yyyyMMdd_{seq}.log`, and returns HTTP 500:

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.6.1",
  "title": "An unexpected error occurred processing your request.",
  "status": 500,
  "detail": "An internal server error occurred. Please provide the TraceId to clinic support.",
  "traceId": "0HN7B9Q69G1R8:00000001",
  "timestamp": "2026-10-17T04:30:00Z"
}
```
*Internal exception messages and database stack traces are redacted from the client response to safeguard tenant security; developers can correlate the issue via `traceId`.*

