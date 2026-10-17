Mattei Grid — Product Requirements Document
Version: 2.4 (Consolidated) | Status: Product scope — Draft; Technical architecture — Locked (Section 11); ERD — Drafted, pending sign-off (Section 9)
Author: Inayath | Consolidated by: Architecture review pass | Date: August 2026
Changes in v2.4: the formal ER Diagram (v4) and Payment/Invoice State Machine are complete and cross-checked (two independent review passes) — see companion documents. Section 9 status updated from "Pending" to "Drafted." Section 15.5 added: five business decisions the ERD process surfaced that the PRD doesn't yet answer.
1. Product Vision
Mattei Grid is a B2B clinical management platform built specifically for electro-homoeopathy practices. It digitises the full patient lifecycle — scheduling, intake, consultation, grid-based remedy selection, laboratory preparation, and billing — into one connected workflow, while preserving the parts of electro-homoeopathic practice a generic clinic system would flatten: remedy selection from the grid, dilution/vehicle choices, and a preparation queue that carries the doctor's prescription accurately through to the lab bench. The core tension the product is designed around: clinical and financial records must be rigorous and auditable, while the day-to-day interface must stay fast enough for a live consultation.
Key differentiators: an interactive prescription builder for dilution/vehicle selection, real-time lab queue sync, automated billing reminders, and a searchable digital materia medica. The platform is multi-tenant SaaS from day one, with strict per-clinic data isolation (mechanism detailed in Architecture, Sec. 11). Tenancy Model A: each user belongs to exactly one clinic; multi-clinic membership per user is an explicit non-goal for MVP.
2. Business Problem
Clinic operations today are almost entirely manual:
Handwritten prescriptions, prone to misreading and error
Manual medicine preparation with no digital specification to check against
Paper appointment registers with no shared visibility across staff
No centralized patient history — clinical, prescription, and billing data live separately
No real-time visibility into laboratory preparation status
Billing follow-up is manual, with no systematic reminder process
Difficult to search or reference previous treatments and outcomes
The system replaces these with a single digital pipeline from consultation through lab to billing.
3. Goals
Primary goals (MVP)
Digitize clinic workflow end to end.
Reduce medicine prep errors via structured digital prescriptions.
Centralize patient management into one record.
Improve lab efficiency via a trackable prep queue.
Maintain complete, searchable patient history.
Simplify billing and payment collection.
Improve follow-up and collection rates.
Success Metrics
Each primary goal above maps to exactly one measurable target below, reviewed monthly against the baseline captured in month 1 of go-live. This table is the single source of truth for targets — figures are not repeated in prose elsewhere to avoid the two copies drifting apart.
Goal
Metric
Baseline
Target
Prep error reduction
% prescriptions with a prep error (lab-reported)
Set from month-1 error log audit
-50% by month 6
Prescription → payment time
Avg hours from invoice generation to payment received
Current manual-process avg (est.)
< 48 hrs post-launch
Collections rate
% invoices overdue > 30 days
Current overdue %
-30% within 6 months
Staff adoption
% active staff using system daily
0% pre-launch
90%+ by week 4
Patient digitisation
% new patients registered digitally
0% pre-launch
100% from launch
Record completeness
% consultations/prescriptions logged digitally
0% pre-launch
100%, zero paper fallback by month 3
Secondary goal (MVP)
Multi-clinic SaaS platform, one user → one clinic (Tenancy Model A — see Sec. 11).
4. User Roles & Permissions
Five roles. Authorization is permission-based (e.g. Patient.View, Prescription.Finalize), not hardcoded role checks — a role is a named bundle of permissions. This lets a clinic grant or withdraw a single capability without inventing a new role, and application code never branches on "if role == Doctor."
Role
Responsibilities
Key Permissions
Doctor
Consultations, Materia Medica, prescriptions, lab review, invoices/history.
Only role with Prescription.* and ClinicalNote.*.
Receptionist
Patient registration, appointments, waiting list, payments, invoices, reminders.
No Prescription.* or ClinicalNote.*.
Lab Technician
Preparation queue, status updates, technician notes/time.
No Prescription.Edit or MateriaMedica.Edit.
Administrator
User/role mgmt, clinic settings, master data, inventory oversight, templates, reports, audit logs.
Full config permissions; no clinical permissions by default.
Patient (Future)
Book appointments, view appointments/invoices, receive notifications.
Read-only, own records only.
5. Functional Modules
5.1 Authentication & Authorization
Login / logout
Permission-based access control, evaluated per request
Password reset
Session management: JWT + refresh-token rotation
Audit logging of access and changes
Stack: ASP.NET Core Identity + JWT. Current clinic resolved from trusted JWT claims, propagated via CurrentTenantContext, which scopes every clinic-owned query. Access control and audit logging are designed against India's DPDP Act, 2023 from the outset — see Section 13.
5.2 Patient Management
Purpose: a complete, centralised patient record.
Search / register / edit patient
Patient timeline (single chronological view)
Archive patient
Consultation, prescription, and invoice history
Duplicate detection: registration performs a fuzzy match on name + date of birth + phone number and surfaces a non-blocking warning with the possible match(es) shown; staff can proceed or merge. This is deliberately advisory, not a hard block — clinics register walk-ins under time pressure and a false-positive match should never stop registration.
Business rules: phone number + relation unique per patient; archived patients cannot be edited (deletion policy — see Section 7).
5.3 Appointment Management
Owned by Receptionists.
Schedule / cancel / reschedule appointment
Follow-up appointment
Waiting list
SMS and WhatsApp reminders
The system flags patients nearing the end of a medication cycle so a follow-up can be triggered, and separately tracks pending billing amounts and due dates for phone follow-up on overdue accounts — regardless of whether the reminder itself is automated or manual.
Double-booking policy: overlapping bookings allowed by default, not hard-blocked. System flags overlap with a warning at booking time (e.g. "Dr. X already booked 3:00–3:15 PM with [patient]"). Receptionist can proceed; override requires a reason (e.g. "emergency," "walk-in," "patient request"), logged against the appointment. Applies per-doctor — two doctors can be booked the same slot with no warning. Rationale: clinics routinely overbook intentionally (no-shows, quick follow-ups); a hard block would break real workflow.
5.4 Consultation Management
Clinical information is recorded separately from the prescription, so a doctor can update notes without affecting what's already been sent to the lab.
Symptoms, diagnosis (pulse-reading imitation), clinical notes
Historical data (read-only)
Follow-up notes (next actions)
Rule: clinical notes require ClinicalNote.Create/Edit (Doctor by default); every prescription is tied to a specific consultation.
5.5 Materia Medica
A searchable, structured remedy knowledge base doctors can consult mid-consultation without breaking workflow — a remedy selected here is added directly into the active Prescription Builder session.
Each entry: code, name, family, description, traditional indications/preparation notes, available dilutions/vehicles, related medicines, practitioner notes, references
Search and filter, favorites, recently viewed
AI-assisted search — future (see Section 14)
5.6 Prescription Builder
Visual, grid-based interface: remedies, electricities (polarity: red, blue, green, yellow, white), dilution, and vehicle selected via clickable elements, not free text. Electricities/polarity selection also carries a non-color indicator (icon or label) alongside color, per the accessibility requirement in Section 13.
Workflow: Select Medicine → Choose Dilution → Choose Vehicle → Specify Quantity → Specify Dosage → Specify Duration → Preview → Send to Laboratory
Multiple medicines per prescription
Electricities / polarity selection
Grid view and card view
Prescription templates, favorites
On finalisation: invoice auto-generated, Razorpay UPI payment link sent via SMS. Payment link TTL = 48 hours; on expiry, receptionist or patient can request regeneration, invalidating the prior link. Payment status tracked downstream via webhook into the collections workflow.
Manual payment fallback: if the patient cannot pay via UPI, the receptionist can mark the invoice as paid via Cash or Card (POS, off-platform) directly from the invoice screen. Requires amount entered + payment mode selected; marks invoice Paid immediately, no webhook dependency. Logged in the audit trail with staff ID + timestamp, same as digital payments.
Revision policy: prescription revision is blocked once any payment (UPI or manual) has been received against the invoice. To change the prescription after partial/full payment, the existing invoice must be cancelled/refunded and a new prescription + invoice created.
5.7 Laboratory Module
Workflow: Received → Accepted → Preparing → Quality Check → Prepared → Ready → Collected
Preparation queue
Barcode / QR code identification
Preparation history, technician notes, time tracking
Real-time status sync to doctor and admin dashboards (SignalR)
Rejection & resubmission: lab staff may reject an order at any stage before Ready, with mandatory comments. Rejected order returns to the doctor's queue with the rejection comment attached. Doctor must create a revised prescription (new version) — the original rejected order is archived, not deleted, and stays visible in patient history. Revised prescription re-enters this workflow at Received.
Offline resilience: lab tablet status updates queue locally on network loss and sync automatically on reconnect; an "unsynced" badge is shown while offline. Maximum offline window before staff must escalate to a manual phone update: 30 minutes. This matters because the lab bench is the one workflow step physically furthest from a reliable network drop, and a silently-lost status update looks identical to a status that was never set.
5.8 Inventory Management
Raw medicines, vehicles, bottles, labels
Batch numbers, expiry tracking, suppliers
Low-stock alerts
Rule: inventory is deducted automatically once the lab marks a preparation complete.
5.9 Billing & Payments
Consultation fee, medicine fee, discounts, GST
Payment methods: Razorpay (UPI-first, digital), Cash (manual), Card (manual, POS off-platform)
Refunds
Invoice statuses: Paid, Pending, Partially Paid, Cancelled
Payment collection integrated with the SMS link from Prescription Builder (5.6). Manual payments (Cash/Card) are entered directly by the receptionist on the invoice screen — amount + mode required, marks Paid immediately, no webhook dependency, logged in the audit trail with staff ID + timestamp.
Reconciliation: every invoice is tagged with a payment source — Digital (Razorpay webhook-confirmed) or Manual (receptionist-entered). Reports (5.11) split revenue by source for cash-handling reconciliation.
Merchant-of-record model: decided — Razorpay Route (see Section 15.1). Each clinic onboards as a KYC'd linked account under Razorpay's PA licence; the platform never holds settlement funds. This is now a locked input to Billing module design, not an open item — clinic onboarding (Section 5.12) must gate a clinic's ability to accept digital payments on completed Razorpay KYC.
5.10 Notification Module
Event-driven, not a single generic reminder type.
Channels: SMS, WhatsApp, Email
Events: appointment reminder, follow-up reminder, payment reminder, medicine ready, low-inventory alert
5.11 Reports & Analytics
Doctor: patient visits, follow-ups
Laboratory: preparation time, queue statistics
Billing: revenue, pending invoices
Inventory: stock usage, expiry
5.12 Administration
User management, roles (permission bundles), clinic settings
Master data (medicines, dilutions, vehicles)
Prescription and notification templates
Audit logs
5.13 Role-Based Dashboards
Doctor: today's appointments, pending lab orders, follow-ups due
Reception: today's bookings, payment reminders due
Laboratory: preparation queue, in progress, completed today
Administrator: revenue, active patients, pending invoices, low inventory
6. Business Workflows
6.1 Patient Consultation — end to end
Patient Arrives → Reception Searches Patient → Register if New → Appointment → Doctor Consultation → Clinical Notes → Materia Medica Lookup → Prescription Builder → Laboratory Queue → Medicine Prepared → Billing/Invoice → Payment → Medicine Dispensed → Follow-up Reminder
6.2 Laboratory Fulfillment
Doctor Sends Order → Queue → Preparing → Quality Check → Ready → Doctor Notified
6.3 Billing & Collections
Invoice Generated → Payment Attempted → Paid? → No → Reminder Sent → Payment Received → Marked Paid.
Accounts that don't respond to automated reminders surface on the administrator and reception dashboards for manual phone follow-up.
7. Business Rules
Only Prescription/ClinicalNote permission holders (Doctor by default) create or edit prescriptions and clinical notes
Receptionists cannot modify prescriptions
Lab staff cannot edit medicines or Materia Medica master data
Every consultation belongs to exactly one patient; every prescription to exactly one consultation; every invoice to exactly one patient
Inventory is deducted automatically after preparation is marked complete
Patient phone number + name must be unique
Patients are archived, never permanently deleted
Lab staff may reject a preparation order, with comments
Prescriptions can be revised; last-revised time is shown to lab staff
Doctors can mark an invoice cancelled or refunded
Retention vs. Erasure — two separate mechanisms
Retention (default): archived patient records are kept, reversible and staff-viewable (read-only), for 7 years from last activity — aligned to standard Indian medical record retention practice, subject to legal/clinical review per state requirements. This protects the clinic: proof of treatment for legal, insurance, or dispute purposes.
Erasure (patient-triggered, DPDP Act 2023): on a valid erasure request, PII (name, phone, address) is anonymized in place — irreversible, can happen any time, does not wait for the 7-year retention period. Underlying clinical/financial data (diagnosis, treatment, billing) is retained in de-identified form for audit and statutory purposes, but can no longer be traced back to the patient. This protects the patient's legal right to be forgotten.
Rule of precedence: an erasure request always overrides the retention timer for PII fields. The clinical/financial record itself still survives — anonymized — until the 7-year retention period ends. See Section 13 (DPDP compliance).
8. Domain Model
Separates reference/catalogue data (rarely changes) from operational/transactional data (changes with every consultation): Clinic (Tenant), Staff/User, Patient, Appointment, Consultation, Materia Medica, Formulary, Prescription/Prescription Item, Laboratory Order, Inventory Item, Invoice/Payment. A ClinicId foreign key is carried on every clinic-scoped table; the isolation mechanism is defined once in Architecture, Sec. 11.
9. Database Design (ER Diagram)
Drafted — see the companion ERD v4 document (Mermaid erDiagram, full attribute list, cross-checked across two independent review passes) and the Payment/Invoice State Machine document. Pending your sign-off on the five items in Section 15.5 before Phase 1 migrations are written. Remaining: per-module authorization permission list, and Context + Container diagrams (Mermaid).
10. Wireframes
Pending — Figma, once the domain model is confirmed. Screens: Login, Role-based Dashboard, Patient Search, Patient Registration, Consultation, Materia Medica, Prescription Builder, Laboratory Queue, Billing, Reports, Inventory.
Consultation screen: left panel — patient history (clinical + financial); centre — interactive remedy grid; right drawer — Materia Medica lookup.
11. System Architecture — Status: Locked
Canonical engineering baseline.
Layer
Choice
Notes
Frontend
Angular
Team already fluent — chosen over React for switching-cost reasons.
Backend
ASP.NET Core, Modular Monolith
No microservices at MVP; boundaries enforced by architecture tests, not folder convention alone.
Database
PostgreSQL + EF Core
Single DB, shared schema, tenant-scoped via ClinicId.
Auth
ASP.NET Core Identity, JWT + refresh
Replaces earlier OIDC reference.
Authorization
Permission-based (e.g. Patient.View)
Roles are permission bundles, not the authorization primitive.
Multi-tenancy
Tenancy Model A (1 user : 1 clinic)
JWT claims → CurrentTenantContext → EF Core global query filter on ClinicId.
Cache
Redis
Provisioned Day 1; adopted per-workload only on demonstrated need.
File Storage
Cloudflare R2 (S3-compatible)
No-egress-fee model, chosen over Azure Blob.
Payments
Razorpay, UPI-first, Route model
Decided — Razorpay Route; settlement model detailed in Sec. 15.1.
Background Jobs
Hangfire
Scheduled/background work.
Real-time
SignalR
Lab queue and dashboard sync; replaces raw WebSockets/SSE reference.
Logging
Serilog + dedicated AuditLogs table
Operational logging and security audit trail are separate systems.
Testing
xUnit + FluentAssertions, Dockerized PostgreSQL
In-memory DB provider is not the primary integration target.
CI/CD
GitHub Actions, from Week 1
Automated build/test on every change.
Hosting (MVP)
DigitalOcean, Bangalore
Cost/complexity-pragmatic choice.
Hosting (scale)
AWS Mumbai
Explicit future migration path, not a Day-1 requirement.
Deployment
Docker Compose
Kubernetes explicitly out of scope until a concrete need exists.
11.1 Release & Environment Strategy
Environments: Staging (mirrors production, used for UAT before every release) and Production.
Promotion flow: GitHub Actions CI runs on every PR; merge to main auto-deploys to Staging; Production deploy is a manual, gated promotion from a verified Staging build.
Rollback: EF Core migrations must ship with a tested Down() migration; deployment rollback is a redeploy of the previous Docker Compose build, given single-region MVP hosting. Blue-green deployment is deferred until the AWS Mumbai migration.
12. API Specification
Pending — to follow once the domain model and screens are finalised. Groups: Authentication, Patient, Appointment, Consultation, Materia Medica, Prescription, Laboratory, Inventory, Billing, Reports.
POST /api/v1/prescriptions — submits the finalized prescription grid, triggers invoice/payment-link generation, creates the laboratory order
GET /api/v1/lab/queue — real-time queue feed (SignalR) for lab technicians and dashboards
13. Non-Functional Requirements
Performance
Dashboard loads in under 2 seconds
Patient search returns in under 1 second
Concurrent users: 3–8 staff per clinic simultaneously (confirm with stakeholders during onboarding sizing)
Platform-wide peak load (MVP planning assumption): up to 50 clinics × 5 concurrent users = 250 concurrent sessions at peak consultation hours (morning/evening). Informs DB connection pooling and hosting-tier sizing on DigitalOcean (Sec. 11); revisit before the AWS Mumbai migration.
Security
Permission-based authorization, evaluated per request
HTTPS everywhere
Full audit logging — dedicated AuditLogs table, separate from operational logs
DPDP Act, 2023 compliance — concrete mechanisms
Explicit consent capture at registration, stored as its own auditable record
Documented breach-notification runbook (who, within what window, by what channel), drafted before go-live and ahead of the May 2027 enforcement deadline
A real erasure path (soft-delete/anonymize) distinct from the "archive, never hard-delete" clinical rule (Sec. 7) — these two are explicitly reconciled there, since DPDP erasure rights and clinical retention obligations can conflict
Availability
99.9% uptime target
Backups
Daily automated PostgreSQL backups, point-in-time recovery, stored independently of primary compute region
R2 object storage versioning enabled for uploaded documents/attachments
Scalability
Multi-tenant from the outset; Tenancy Model A locked for MVP
Accessibility
Target: WCAG 2.1 AA as the MVP baseline for the clinical-use interface
MVP-scope minimum: full keyboard navigation, sufficient color contrast, and non-color indicators (icon or label) on every status/selection element that currently relies on color alone — this directly affects the electricities/polarity picker in the Prescription Builder (5.6: red, blue, green, yellow, white), which must not rely on color alone for colorblind staff
Full WCAG audit deferred to Phase 2; the minimum bar above is set now so the frontend isn't retrofitted later
14. Future Enhancements
AI-assisted Materia Medica search
AI prescription drafting (decision support only, not autonomous)
Voice-to-prescription
OCR for digitizing old handwritten records
Patient mobile application
Online consultation
Digital signatures
Inventory demand prediction
Advanced analytics dashboard
Multi-language and multi-country support
Multi-clinic membership per user
15. Open Decisions & Recommendations
15.1 Payment Merchant Model — Razorpay Route
RBI's Master Direction for Payment Aggregators requires a PA to "aggregate funds only for the merchant with whom it has a contractual relationship" and prohibits PAs from "marketplace business." A model where the platform is sole merchant-of-record and forwards money to clinics as informal payees is not compliant.
Decision: Razorpay Route. Each clinic onboards as a KYC'd linked account under Razorpay's PA license; the platform embeds one unified checkout and Razorpay splits settlement automatically to the correct clinic account. The platform never holds funds. This satisfies the contractual-relationship requirement without the founder needing a personal PA license.
Next steps:
Contact Razorpay, initiate Route onboarding; confirm per-clinic KYC document requirements (PAN, GST if applicable, bank proof) and approval lead time.
Clinic onboarding flow (Section 5.12 Administration) requires KYC-with-Razorpay as a mandatory step before a clinic can accept payments.
Target: decision executed, Route onboarding started within 1–2 weeks of PRD sign-off.
15.2 Tenant Isolation — the query filter is necessary, not sufficient
Tenant isolation must be enforced through multiple layers:
ClinicId: every clinic-scoped entity must contain ClinicId; never trust client-provided ClinicId.
Reads: EF Core global query filters restrict queries to the current clinic.
Writes: SaveChangesInterceptor automatically stamps ClinicId from CurrentTenantContext.
Authorization: permission checks remain separate from tenant isolation.
Bypass paths: raw SQL, bulk operations, Hangfire jobs, and SignalR must explicitly preserve tenant context.
Database: ClinicId must be NOT NULL on clinic-scoped tables.
Testing: CI-enforced integration tests must verify cross-tenant reads/writes are impossible.
Flow: JWT → CurrentTenantContext → Query Filter + Authorization → SaveChangesInterceptor → PostgreSQL
15.3 Modular Monolith — boundaries need enforcement, not just structure
A modular monolith without an enforced boundary is a big ball of mud with extra folders — higher risk here given solo/AI-assisted development, with no second reviewer catching a module reaching into another module's internals.
Recommended: architecture tests (e.g. NetArchTest) in CI that fail the build if, for example, Billing reaches into Clinical module internals instead of its public contract. Cheap now, expensive to retrofit.
15.4 Third-Party Dependency Risk
External dependencies that sit on the critical path and are outside engineering's direct control:
Dependency
Blocks
Lead Time / Risk
Mitigation
WhatsApp Business API
Reminders (5.10), Appointments (5.3)
Approval: 1–4 weeks
Start approval immediately. SMS is primary/fallback channel until approved — do not block MVP messaging on WhatsApp.
SMS Gateway (e.g. MSG91, Twilio)
Reminders, prescription payment link (5.6)
Vendor selection + delivery SLA confirmation
Confirm uptime SLA before go-live; this is on the critical path for 5.6 and 5.10.
Razorpay Route
Billing & Payments (5.9), Sec. 15.1
Per-clinic KYC onboarding time
Factor into per-clinic onboarding timeline, not just platform build timeline.
15.5 Engineering-Surfaced Open Decisions (from ERD v4 cross-check)
Producing the formal ERD (Sec 9) exposed five business decisions the PRD doesn't currently make. These aren't schema bugs — the schema supports either answer — but Billing, Consultation, and Inventory module design (5.2–5.9) cannot finalize without them.
Consultation-only billing: can a clinic charge a consultation fee when no prescription is issued? The ERD anchors Invoice to Consultation (mandatory) with Prescription as optional specifically to avoid pre-deciding this — but the answer changes what Billing module UI needs to support.
Consultation.Status values: likely candidates are Scheduled/InProgress/Completed/Cancelled/NoShow, but this also depends on whether a Consultation row is created at appointment-booking time or only when the doctor starts seeing the patient — that timing choice isn't decided yet.
Inventory restock/adjustment scope: the ledger structure already supports StockReceipt/ManualAdjustment/Wastage alongside lab-triggered deduction (a schema decision, already made) — open is whether the MVP Administration UI (5.12) needs a "receive stock" screen at launch or can defer it.
Per-role permission customization: schema is many-to-many regardless of the answer; this only affects Phase 1 Admin UI scope. Recommendation: defer customization UI, ship fixed seeded roles for MVP.
Consent policy-version tracking: field semantics are now defined (a snapshot string of the active policy identifier at consent time, not a full versioned-documents system) — open is only whether this is needed for MVP or deferrable to the Phase 4 compliance hardening pass.
