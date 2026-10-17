using MatteiGrid.Api.Authorization;
using MatteiGrid.Api.Hubs;
using MatteiGrid.Modules.Appointments.Contracts;
using MatteiGrid.Modules.Billing.Contracts;
using MatteiGrid.Modules.Clinical.Contracts;
using MatteiGrid.Modules.Identity.Contracts;
using MatteiGrid.Modules.Inventory.Contracts;
using MatteiGrid.Modules.Laboratory.Contracts;
using MatteiGrid.Modules.MateriaMedica.Contracts;
using MatteiGrid.Modules.Patients.Contracts;
using MatteiGrid.Modules.Prescriptions.Contracts;
using MatteiGrid.Modules.Prescriptions.Domain;
using MatteiGrid.Shared.Infrastructure.Audit;
using MatteiGrid.Shared.Infrastructure.Persistence;
using MatteiGrid.Shared.Infrastructure.Tenancy;
using MatteiGrid.Shared.Kernel;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace MatteiGrid.Api.Endpoints;

public static class ApiEndpoints
{
    private const string RefreshTokenCookieName = "__Host-mattei-refresh-token";

    public static void MapMatteiGridEndpoints(this IEndpointRouteBuilder app)
    {
        var v1 = app.MapGroup("/api/v1");

        // --- AUTH ---
        var auth = v1.MapGroup("/auth");

        auth.MapPost("/login", async (LoginRequest req, HttpContext ctx, IIdentityModule identity) =>
        {
            var clientIp = GetClientIp(ctx);
            var res = await identity.LoginAsync(req, clientIp);
            if (res is null) return Results.Unauthorized();

            if (!string.IsNullOrEmpty(res.RefreshToken))
            {
                SetRefreshTokenCookie(ctx, res.RefreshToken, res.ExpiresAt ?? DateTimeOffset.UtcNow.AddDays(7));
            }

            return Results.Ok(res);
        }).AllowAnonymous();

        auth.MapPost("/select-tenant", async (SelectTenantRequest req, HttpContext ctx, IIdentityModule identity) =>
        {
            var clientIp = GetClientIp(ctx);
            var res = await identity.SelectTenantAsync(req, clientIp);
            if (res is null) return Results.Unauthorized();

            if (!string.IsNullOrEmpty(res.RefreshToken))
            {
                SetRefreshTokenCookie(ctx, res.RefreshToken, res.ExpiresAt ?? DateTimeOffset.UtcNow.AddDays(7));
            }

            return Results.Ok(res);
        }).AllowAnonymous();

        auth.MapPost("/refresh", async (RefreshRequest? req, HttpContext ctx, IIdentityModule identity) =>
        {
            var token = req?.RefreshToken;
            if (string.IsNullOrWhiteSpace(token))
            {
                token = ctx.Request.Cookies[RefreshTokenCookieName];
            }

            if (string.IsNullOrWhiteSpace(token)) return Results.Unauthorized();

            var clientIp = GetClientIp(ctx);
            var res = await identity.RefreshAsync(new RefreshRequest(token), clientIp);
            if (res is null) return Results.Unauthorized();

            if (!string.IsNullOrEmpty(res.RefreshToken))
            {
                SetRefreshTokenCookie(ctx, res.RefreshToken, res.ExpiresAt ?? DateTimeOffset.UtcNow.AddDays(7));
            }

            return Results.Ok(res);
        }).AllowAnonymous();

        auth.MapPost("/logout", async (RefreshRequest? req, HttpContext ctx, IIdentityModule identity) =>
        {
            var token = req?.RefreshToken ?? ctx.Request.Cookies[RefreshTokenCookieName];
            if (!string.IsNullOrWhiteSpace(token))
            {
                await identity.LogoutAsync(token);
            }

            ctx.Response.Cookies.Delete(RefreshTokenCookieName);
            return Results.NoContent();
        });

        auth.MapGet("/me", async (ICurrentTenantContext tenantContext, IIdentityModule identity) =>
        {
            if (tenantContext.UserId is null) return Results.Unauthorized();
            var user = await identity.GetCurrentUserAsync(tenantContext.UserId.Value);
            return user is null ? Results.NotFound() : Results.Ok(user);
        });

        // --- SUPERADMIN PLATFORM & BREAK-GLASS ---
        var superadmin = v1.MapGroup("/superadmin");
        superadmin.MapPost("/break-glass", async (BreakGlassRequest req, ICurrentTenantContext tenantContext, IIdentityModule identity) =>
        {
            if (tenantContext.UserId is null) return Results.Unauthorized();
            var res = await identity.InitiateBreakGlassAsync(tenantContext.UserId.Value, req);
            return Results.Ok(res);
        }).RequirePermission(Permissions.SuperAdminBreakGlass);

        // --- PATIENTS ---
        var patients = v1.MapGroup("/patients");
        patients.MapGet("/", async (string? query, IPatientService service, IAuditService audit) =>
        {
            var list = await service.SearchPatientsAsync(query);
            await audit.LogSecurityEventAsync(AuditEventTypes.PhiRead, "SearchPatients", "Patient", null, $"Query: {query}");
            return Results.Ok(list);
        }).RequirePermission(Permissions.PatientsRead);

        patients.MapGet("/{id:guid}", async (Guid id, IPatientService service, IAuditService audit) =>
        {
            var p = await service.GetPatientByIdAsync(id);
            if (p is not null)
            {
                await audit.LogSecurityEventAsync(AuditEventTypes.PhiRead, "ViewPatient", "Patient", id);
            }
            return p is null ? Results.NotFound() : Results.Ok(p);
        }).RequirePermission(Permissions.PatientsRead);

        patients.MapPost("/", async (RegisterPatientRequest req, IPatientService service) =>
        {
            var (patient, warning) = await service.RegisterPatientAsync(req);
            return Results.Created($"/api/v1/patients/{patient.Id}", new { patient, warning });
        }).RequirePermission(Permissions.PatientsCreate);

        patients.MapPost("/{id:guid}/archive", async (Guid id, IPatientService service) =>
        {
            var success = await service.ArchivePatientAsync(id);
            return success ? Results.Ok() : Results.NotFound();
        }).RequirePermission(Permissions.PatientsArchive);

        patients.MapPost("/{id:guid}/erase-pii", async (Guid id, IPatientService service, IAuditService audit) =>
        {
            var success = await service.ErasePatientPiiAsync(id);
            if (success)
            {
                await audit.LogSecurityEventAsync(
                    AuditEventTypes.PiiErased,
                    "ErasePatientPii",
                    "Patient",
                    id,
                    "Patient PII anonymized in compliance with DPDP Act 2023");
            }
            return success ? Results.Ok(new { message = "Patient PII anonymized in compliance with DPDP Act 2023." }) : Results.NotFound();
        }).RequirePermission(Permissions.PatientsErasePii);

        // --- APPOINTMENTS ---
        var appointments = v1.MapGroup("/appointments");
        appointments.MapGet("/", async (DateTimeOffset? from, DateTimeOffset? to, Guid? doctorStaffId, IAppointmentService service) =>
        {
            var list = await service.GetAppointmentsAsync(from, to, doctorStaffId);
            return Results.Ok(list);
        }).RequirePermission(Permissions.AppointmentsRead);

        appointments.MapPost("/", async (BookAppointmentRequest req, IAppointmentService service, IHubContext<DashboardHub, IDashboardClient> hub) =>
        {
            var (appointment, overlap) = await service.BookAppointmentAsync(req);
            await hub.Clients.Group($"clinic-{appointment.ClinicId}").AppointmentBooked(appointment.Id);
            return Results.Created($"/api/v1/appointments/{appointment.Id}", new { appointment, overlap });
        }).RequirePermission(Permissions.AppointmentsCreate);

        appointments.MapPut("/{id:guid}/status", async (Guid id, string status, IAppointmentService service) =>
        {
            var success = await service.UpdateStatusAsync(id, status);
            return success ? Results.Ok() : Results.NotFound();
        }).RequirePermission(Permissions.AppointmentsUpdate);

        // --- CLINICAL / CONSULTATIONS ---
        var clinical = v1.MapGroup("/consultations");
        clinical.MapPost("/", async (StartConsultationRequest req, IClinicalService service) =>
        {
            var consultation = await service.StartConsultationAsync(req);
            return Results.Created($"/api/v1/consultations/{consultation.Id}", consultation);
        }).RequirePermission(Permissions.ConsultationsCreate);

        clinical.MapGet("/{id:guid}", async (Guid id, IClinicalService service, IAuditService audit) =>
        {
            var c = await service.GetConsultationAsync(id);
            if (c is not null)
            {
                await audit.LogSecurityEventAsync(AuditEventTypes.PhiConsultationAccess, "ViewConsultation", "Consultation", id);
            }
            return c is null ? Results.NotFound() : Results.Ok(c);
        }).RequirePermission(Permissions.ConsultationsRead);

        clinical.MapPost("/{id:guid}/notes", async (Guid id, AddClinicalNoteRequest req, IClinicalService service) =>
        {
            var note = await service.AddOrUpdateNoteAsync(id, req.StaffId, req.Content, req.NoteId);
            return Results.Ok(note);
        }).RequirePermission(Permissions.ClinicalNotesEdit);

        clinical.MapPost("/{id:guid}/complete", async (Guid id, IClinicalService service) =>
        {
            var success = await service.CompleteConsultationAsync(id);
            return success ? Results.Ok() : Results.NotFound();
        }).RequirePermission(Permissions.ConsultationsEdit);

        // --- MATERIA MEDICA ---
        var materiaMedica = v1.MapGroup("/materia-medica");
        materiaMedica.MapGet("/", async (string? query, IMateriaMedicaService service) =>
        {
            var entries = await service.SearchEntriesAsync(query);
            return Results.Ok(entries);
        }).RequirePermission(Permissions.MateriaMedicaRead);

        materiaMedica.MapGet("/{id:guid}", async (Guid id, IMateriaMedicaService service) =>
        {
            var entry = await service.GetEntryByIdAsync(id);
            return entry is null ? Results.NotFound() : Results.Ok(entry);
        }).RequirePermission(Permissions.MateriaMedicaRead);

        materiaMedica.MapPost("/{id:guid}/notes", async (Guid id, AddPractitionerNoteRequest req, IMateriaMedicaService service) =>
        {
            var note = await service.AddPractitionerNoteAsync(id, req.StaffId, req.Content);
            return Results.Created($"/api/v1/materia-medica/{id}/notes/{note.Id}", note);
        }).RequirePermission(Permissions.MateriaMedicaEdit);

        // --- PRESCRIPTIONS ---
        var prescriptions = v1.MapGroup("/prescriptions");
        prescriptions.MapGet("/medicines", async (AppDbContext db) =>
            Results.Ok(await db.Set<Medicine>().Where(m => m.IsActive).ToListAsync())
        ).RequirePermission(Permissions.PrescriptionsRead);

        prescriptions.MapGet("/dilutions", async (AppDbContext db) =>
            Results.Ok(await db.Set<Dilution>().ToListAsync())
        ).RequirePermission(Permissions.PrescriptionsRead);

        prescriptions.MapGet("/vehicles", async (AppDbContext db) =>
            Results.Ok(await db.Set<Vehicle>().ToListAsync())
        ).RequirePermission(Permissions.PrescriptionsRead);

        prescriptions.MapGet("/{id:guid}", async (Guid id, IPrescriptionService service, IAuditService audit) =>
        {
            var p = await service.GetPrescriptionByIdAsync(id);
            if (p is not null)
            {
                await audit.LogSecurityEventAsync(AuditEventTypes.PhiPrescriptionView, "ViewPrescription", "Prescription", id);
            }
            return p is null ? Results.NotFound() : Results.Ok(p);
        }).RequirePermission(Permissions.PrescriptionsRead);

        prescriptions.MapGet("/consultation/{consultationId:guid}", async (Guid consultationId, IPrescriptionService service) =>
        {
            var list = await service.GetPrescriptionsByConsultationIdAsync(consultationId);
            return Results.Ok(list);
        }).RequirePermission(Permissions.PrescriptionsRead);

        prescriptions.MapPost("/", async (CreatePrescriptionRequest req, IPrescriptionService service) =>
        {
            var p = await service.CreatePrescriptionAsync(req);
            return Results.Created($"/api/v1/prescriptions/{p.Id}", p);
        }).RequirePermission(Permissions.PrescriptionsCreate);

        prescriptions.MapPost("/revise", async (RevisePrescriptionRequest req, IPrescriptionService service) =>
        {
            var p = await service.RevisePrescriptionAsync(req);
            return Results.Created($"/api/v1/prescriptions/{p.Id}", p);
        }).RequirePermission(Permissions.PrescriptionsEdit);

        prescriptions.MapPost("/{id:guid}/finalize", async (
            Guid id,
            FinalizePrescriptionRequest req,
            IPrescriptionService prescriptionService,
            ILaboratoryService labService,
            IBillingService billingService,
            IClinicalService clinicalService,
            IHubContext<LabQueueHub, ILabQueueClient> hub) =>
        {
            var p = await prescriptionService.FinalizePrescriptionAsync(id, req.StaffId);

            // 1. Create Laboratory Order
            var labOrder = await labService.CreateOrderForPrescriptionAsync(p.Id);
            await hub.Clients.Group($"clinic-{p.ClinicId}:lab").OrderReceived(labOrder.Id, p.Id);

            // 2. Fetch Consultation to get PatientId for billing
            var consultation = await clinicalService.GetConsultationAsync(p.ConsultationId);
            if (consultation is not null)
            {
                var lineItems = p.Items.Select(item =>
                    new CreateInvoiceLineItemRequest(item.MedicineId, $"{item.MedicineName} ({item.DilutionLabel}, {item.VehicleName})", item.Quantity, 150m)
                ).ToList();

                lineItems.Insert(0, new CreateInvoiceLineItemRequest(null, "Consultation Fee", 1, 300m));

                var invoice = await billingService.CreateInvoiceAsync(new CreateInvoiceRequest(
                    consultation.PatientId,
                    p.ConsultationId,
                    p.Id,
                    lineItems
                ));

                return Results.Ok(new { prescription = p, labOrder, invoice });
            }

            return Results.Ok(new { prescription = p, labOrder });
        }).RequirePermission(Permissions.PrescriptionsFinalize);

        // --- LABORATORY QUEUE ---
        var lab = v1.MapGroup("/lab");
        lab.MapGet("/queue", async (string? status, ILaboratoryService service) =>
        {
            var queue = await service.GetQueueAsync(status);
            return Results.Ok(queue);
        }).RequirePermission(Permissions.LaboratoryRead);

        lab.MapPost("/{id:guid}/transition", async (
            Guid id,
            TransitionLabStatusRequest req,
            ILaboratoryService labService,
            IInventoryService inventoryService,
            IPrescriptionService prescriptionService,
            IHubContext<LabQueueHub, ILabQueueClient> hub) =>
        {
            var order = await labService.TransitionStatusAsync(id, req.TargetStatus, req.StaffId);
            await hub.Clients.Group($"clinic-{order.ClinicId}:lab").OrderStatusChanged(order.Id, order.Status);

            if (req.TargetStatus == MatteiGrid.Modules.Laboratory.Domain.LaboratoryOrderStatus.Prepared ||
                req.TargetStatus == MatteiGrid.Modules.Laboratory.Domain.LaboratoryOrderStatus.Ready)
            {
                var prescription = await prescriptionService.GetPrescriptionByIdAsync(order.PrescriptionId);
                if (prescription is not null)
                {
                    foreach (var item in prescription.Items)
                    {
                        await inventoryService.RecordLabDeductionAsync(item.MedicineId, item.Quantity, order.Id);
                    }
                }
            }

            return Results.Ok(order);
        }).RequirePermission(Permissions.LaboratoryUpdateStatus);

        lab.MapPost("/{id:guid}/reject", async (
            Guid id,
            RejectLabOrderRequest req,
            ILaboratoryService labService,
            IHubContext<LabQueueHub, ILabQueueClient> hub) =>
        {
            var order = await labService.RejectOrderAsync(id, req.Comment, req.StaffId);
            await hub.Clients.Group($"clinic-{order.ClinicId}:lab").OrderRejected(order.Id, req.Comment);
            return Results.Ok(order);
        }).RequirePermission(Permissions.LaboratoryReject);

        // --- INVENTORY ---
        var inventory = v1.MapGroup("/inventory");
        inventory.MapGet("/", async (bool? lowStockOnly, IInventoryService service) =>
        {
            var items = await service.GetInventoryItemsAsync(lowStockOnly ?? false);
            return Results.Ok(items);
        }).RequirePermission(Permissions.InventoryRead);

        inventory.MapPost("/receive-stock", async (ReceiveStockRequest req, IInventoryService service) =>
        {
            var item = await service.ReceiveStockAsync(req);
            return Results.Ok(item);
        }).RequirePermission(Permissions.InventoryManage);

        inventory.MapGet("/transactions", async (Guid? itemId, IInventoryService service) =>
        {
            var txs = await service.GetTransactionsAsync(itemId);
            return Results.Ok(txs);
        }).RequirePermission(Permissions.InventoryRead);

        // --- BILLING & INVOICES ---
        var billing = v1.MapGroup("/billing");
        billing.MapGet("/invoices/{id:guid}", async (Guid id, IBillingService service) =>
        {
            var invoice = await service.GetInvoiceByIdAsync(id);
            return invoice is null ? Results.NotFound() : Results.Ok(invoice);
        }).RequirePermission(Permissions.BillingRead);

        billing.MapPost("/invoices/{id:guid}/manual-payment", async (Guid id, RecordManualPaymentRequest req, IBillingService service, IAuditService audit) =>
        {
            var updated = await service.RecordManualPaymentAsync(id, req);
            await audit.LogSecurityEventAsync(
                AuditEventTypes.BillingManualPayment,
                "ManualPaymentReceived",
                "Invoice",
                id,
                $"Payment recorded: {req.Amount} via {req.Mode}");
            return Results.Ok(updated);
        }).RequirePermission(Permissions.BillingManualPayment);

        billing.MapPost("/invoices/{id:guid}/cancel", async (Guid id, IBillingService service) =>
        {
            var updated = await service.CancelInvoiceAsync(id);
            return Results.Ok(updated);
        }).RequirePermission(Permissions.BillingCancel);

        billing.MapPost("/invoices/{id:guid}/refund", async (Guid id, ProcessInvoiceRefundRequest req, IBillingService service, IAuditService audit) =>
        {
            var updated = await service.ProcessRefundAsync(id, req.PaymentId, new ProcessRefundRequest(req.Amount, req.Reason, req.StaffId));
            await audit.LogSecurityEventAsync(
                AuditEventTypes.BillingRefund,
                "ProcessRefund",
                "Invoice",
                id,
                $"Refund processed: {req.Amount}. Reason: {req.Reason}");
            return Results.Ok(updated);
        }).RequirePermission(Permissions.BillingRefund);

        billing.MapPost("/invoices/{id:guid}/payment-link/regenerate", async (Guid id, IBillingService service) =>
        {
            var link = await service.RegeneratePaymentLinkAsync(id);
            return Results.Ok(link);
        }).RequirePermission(Permissions.BillingCreate);

        billing.MapPost("/razorpay-webhook", async (RazorpayWebhookPayload payload, IBillingService service) =>
        {
            var success = await service.HandleRazorpayWebhookAsync(payload.RazorpayPaymentId, payload.Amount, payload.InvoiceId);
            return success ? Results.Ok() : Results.BadRequest();
        }).AllowAnonymous();

        // --- ADMINISTRATION ---
        var admin = v1.MapGroup("/admin");
        admin.MapGet("/users", async (IIdentityModule identity) =>
        {
            var users = await identity.GetUsersAsync();
            return Results.Ok(users);
        }).RequirePermission(Permissions.AdministrationManageUsers);

        admin.MapPost("/users", async (CreateUserRequest req, IIdentityModule identity) =>
        {
            var user = await identity.CreateUserAsync(req);
            return Results.Created($"/api/v1/admin/users/{user.Id}", user);
        }).RequirePermission(Permissions.AdministrationManageUsers);

        admin.MapPost("/users/{id:guid}/role", async (Guid id, AssignRoleRequest req, IIdentityModule identity) =>
        {
            await identity.AssignRoleAsync(id, req.Role);
            return Results.Ok();
        }).RequirePermission(Permissions.AdministrationAssignRoles);

        admin.MapGet("/audit-logs", async (AppDbContext db, ICurrentTenantContext tenantContext) =>
        {
            var logs = await db.AuditLogs
                .OrderByDescending(a => a.CreatedAt)
                .Take(100)
                .ToListAsync();
            return Results.Ok(logs);
        }).RequirePermission(Permissions.AdministrationAuditLogs);
    }

    private static string? GetClientIp(HttpContext ctx)
    {
        return ctx.Request.Headers.TryGetValue("X-Forwarded-For", out var forwarded)
            ? forwarded.FirstOrDefault()?.Split(',').FirstOrDefault()?.Trim()
            : ctx.Connection.RemoteIpAddress?.ToString();
    }

    private static void SetRefreshTokenCookie(HttpContext ctx, string refreshToken, DateTimeOffset expiresAt)
    {
        ctx.Response.Cookies.Append(RefreshTokenCookieName, refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = expiresAt
        });
    }
}

// Request record contracts
public record AssignRoleRequest(string Role);
public record AddClinicalNoteRequest(Guid StaffId, string Content, Guid? NoteId = null);
public record AddPractitionerNoteRequest(Guid StaffId, string Content);
public record FinalizePrescriptionRequest(Guid StaffId);
public record TransitionLabStatusRequest(string TargetStatus, Guid? StaffId = null);
public record RejectLabOrderRequest(string Comment, Guid StaffId);
public record ProcessInvoiceRefundRequest(Guid PaymentId, decimal Amount, string Reason, Guid StaffId);
public record RazorpayWebhookPayload(string RazorpayPaymentId, decimal Amount, Guid InvoiceId);

