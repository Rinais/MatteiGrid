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
using MatteiGrid.Shared.Infrastructure.Persistence;
using MatteiGrid.Shared.Infrastructure.Tenancy;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace MatteiGrid.Api.Endpoints;

public static class ApiEndpoints
{
    public static void MapMatteiGridEndpoints(this IEndpointRouteBuilder app)
    {
        var v1 = app.MapGroup("/api/v1");

        // --- AUTH ---
        var auth = v1.MapGroup("/auth");
        auth.MapPost("/login", async (LoginRequest req, IIdentityModule identity) =>
        {
            var res = await identity.LoginAsync(req);
            return res is null ? Results.Unauthorized() : Results.Ok(res);
        });

        auth.MapPost("/refresh", async (RefreshRequest req, IIdentityModule identity) =>
        {
            var res = await identity.RefreshAsync(req);
            return res is null ? Results.Unauthorized() : Results.Ok(res);
        });

        auth.MapPost("/logout", async (RefreshRequest req, IIdentityModule identity) =>
        {
            await identity.LogoutAsync(req.RefreshToken);
            return Results.NoContent();
        });

        // --- PATIENTS ---
        var patients = v1.MapGroup("/patients");
        patients.MapGet("/", async (string? query, IPatientService service) =>
        {
            var list = await service.SearchPatientsAsync(query);
            return Results.Ok(list);
        });

        patients.MapGet("/{id:guid}", async (Guid id, IPatientService service) =>
        {
            var p = await service.GetPatientByIdAsync(id);
            return p is null ? Results.NotFound() : Results.Ok(p);
        });

        patients.MapPost("/", async (RegisterPatientRequest req, IPatientService service) =>
        {
            var (patient, warning) = await service.RegisterPatientAsync(req);
            return Results.Created($"/api/v1/patients/{patient.Id}", new { patient, warning });
        });

        patients.MapPost("/{id:guid}/archive", async (Guid id, IPatientService service) =>
        {
            var success = await service.ArchivePatientAsync(id);
            return success ? Results.Ok() : Results.NotFound();
        });

        patients.MapPost("/{id:guid}/erase-pii", async (Guid id, IPatientService service) =>
        {
            var success = await service.ErasePatientPiiAsync(id);
            return success ? Results.Ok(new { message = "Patient PII anonymized in compliance with DPDP Act 2023." }) : Results.NotFound();
        });

        // --- APPOINTMENTS ---
        var appointments = v1.MapGroup("/appointments");
        appointments.MapGet("/", async (DateTimeOffset? from, DateTimeOffset? to, Guid? doctorStaffId, IAppointmentService service) =>
        {
            var list = await service.GetAppointmentsAsync(from, to, doctorStaffId);
            return Results.Ok(list);
        });

        appointments.MapPost("/", async (BookAppointmentRequest req, IAppointmentService service, IHubContext<DashboardHub, IDashboardClient> hub) =>
        {
            var (appointment, overlap) = await service.BookAppointmentAsync(req);
            await hub.Clients.Group($"clinic-{appointment.ClinicId}").AppointmentBooked(appointment.Id);
            return Results.Created($"/api/v1/appointments/{appointment.Id}", new { appointment, overlap });
        });

        appointments.MapPut("/{id:guid}/status", async (Guid id, string status, IAppointmentService service) =>
        {
            var success = await service.UpdateStatusAsync(id, status);
            return success ? Results.Ok() : Results.NotFound();
        });

        // --- CLINICAL / CONSULTATIONS ---
        var clinical = v1.MapGroup("/consultations");
        clinical.MapPost("/", async (StartConsultationRequest req, IClinicalService service) =>
        {
            var consultation = await service.StartConsultationAsync(req);
            return Results.Created($"/api/v1/consultations/{consultation.Id}", consultation);
        });

        clinical.MapGet("/{id:guid}", async (Guid id, IClinicalService service) =>
        {
            var c = await service.GetConsultationAsync(id);
            return c is null ? Results.NotFound() : Results.Ok(c);
        });

        clinical.MapPost("/{id:guid}/notes", async (Guid id, AddClinicalNoteRequest req, IClinicalService service) =>
        {
            var note = await service.AddOrUpdateNoteAsync(id, req.StaffId, req.Content, req.NoteId);
            return Results.Ok(note);
        });

        clinical.MapPost("/{id:guid}/complete", async (Guid id, IClinicalService service) =>
        {
            var success = await service.CompleteConsultationAsync(id);
            return success ? Results.Ok() : Results.NotFound();
        });

        // --- MATERIA MEDICA ---
        var materiaMedica = v1.MapGroup("/materia-medica");
        materiaMedica.MapGet("/", async (string? query, IMateriaMedicaService service) =>
        {
            var entries = await service.SearchEntriesAsync(query);
            return Results.Ok(entries);
        });

        materiaMedica.MapGet("/{id:guid}", async (Guid id, IMateriaMedicaService service) =>
        {
            var entry = await service.GetEntryByIdAsync(id);
            return entry is null ? Results.NotFound() : Results.Ok(entry);
        });

        materiaMedica.MapPost("/{id:guid}/notes", async (Guid id, AddPractitionerNoteRequest req, IMateriaMedicaService service) =>
        {
            var note = await service.AddPractitionerNoteAsync(id, req.StaffId, req.Content);
            return Results.Created($"/api/v1/materia-medica/{id}/notes/{note.Id}", note);
        });

        // --- PRESCRIPTIONS ---
        var prescriptions = v1.MapGroup("/prescriptions");
        prescriptions.MapGet("/medicines", async (AppDbContext db) =>
            Results.Ok(await db.Set<Medicine>().Where(m => m.IsActive).ToListAsync()));

        prescriptions.MapGet("/dilutions", async (AppDbContext db) =>
            Results.Ok(await db.Set<Dilution>().ToListAsync()));

        prescriptions.MapGet("/vehicles", async (AppDbContext db) =>
            Results.Ok(await db.Set<Vehicle>().ToListAsync()));

        prescriptions.MapGet("/{id:guid}", async (Guid id, IPrescriptionService service) =>
        {
            var p = await service.GetPrescriptionByIdAsync(id);
            return p is null ? Results.NotFound() : Results.Ok(p);
        });

        prescriptions.MapGet("/consultation/{consultationId:guid}", async (Guid consultationId, IPrescriptionService service) =>
        {
            var list = await service.GetPrescriptionsByConsultationIdAsync(consultationId);
            return Results.Ok(list);
        });

        prescriptions.MapPost("/", async (CreatePrescriptionRequest req, IPrescriptionService service) =>
        {
            var p = await service.CreatePrescriptionAsync(req);
            return Results.Created($"/api/v1/prescriptions/{p.Id}", p);
        });

        prescriptions.MapPost("/revise", async (RevisePrescriptionRequest req, IPrescriptionService service) =>
        {
            var p = await service.RevisePrescriptionAsync(req);
            return Results.Created($"/api/v1/prescriptions/{p.Id}", p);
        });

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
            await hub.Clients.Group($"clinic-{p.ClinicId}").OrderReceived(labOrder.Id, p.Id);

            // 2. Fetch Consultation to get PatientId for billing
            var consultation = await clinicalService.GetConsultationAsync(p.ConsultationId);
            if (consultation is not null)
            {
                var lineItems = p.Items.Select(item =>
                    new CreateInvoiceLineItemRequest(item.MedicineId, $"{item.MedicineName} ({item.DilutionLabel}, {item.VehicleName})", item.Quantity, 150m) // standard remedy fee
                ).ToList();

                // Add consultation fee
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
        });

        // --- LABORATORY QUEUE ---
        var lab = v1.MapGroup("/lab");
        lab.MapGet("/queue", async (string? status, ILaboratoryService service) =>
        {
            var queue = await service.GetQueueAsync(status);
            return Results.Ok(queue);
        });

        lab.MapPost("/{id:guid}/transition", async (
            Guid id,
            TransitionLabStatusRequest req,
            ILaboratoryService labService,
            IInventoryService inventoryService,
            IPrescriptionService prescriptionService,
            IHubContext<LabQueueHub, ILabQueueClient> hub) =>
        {
            var order = await labService.TransitionStatusAsync(id, req.TargetStatus, req.StaffId);
            await hub.Clients.Group($"clinic-{order.ClinicId}").OrderStatusChanged(order.Id, order.Status);

            // PRD 5.8: Automatically deduct inventory once lab marks order complete / prepared
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
        });

        lab.MapPost("/{id:guid}/reject", async (
            Guid id,
            RejectLabOrderRequest req,
            ILaboratoryService labService,
            IHubContext<LabQueueHub, ILabQueueClient> hub) =>
        {
            var order = await labService.RejectOrderAsync(id, req.Comment, req.StaffId);
            await hub.Clients.Group($"clinic-{order.ClinicId}").OrderRejected(order.Id, req.Comment);
            return Results.Ok(order);
        });

        // --- INVENTORY ---
        var inventory = v1.MapGroup("/inventory");
        inventory.MapGet("/", async (bool? lowStockOnly, IInventoryService service) =>
        {
            var items = await service.GetInventoryItemsAsync(lowStockOnly ?? false);
            return Results.Ok(items);
        });

        inventory.MapPost("/receive-stock", async (ReceiveStockRequest req, IInventoryService service) =>
        {
            var item = await service.ReceiveStockAsync(req);
            return Results.Ok(item);
        });

        inventory.MapGet("/transactions", async (Guid? itemId, IInventoryService service) =>
        {
            var txs = await service.GetTransactionsAsync(itemId);
            return Results.Ok(txs);
        });

        // --- BILLING & INVOICES ---
        var billing = v1.MapGroup("/billing");
        billing.MapGet("/invoices/{id:guid}", async (Guid id, IBillingService service) =>
        {
            var invoice = await service.GetInvoiceByIdAsync(id);
            return invoice is null ? Results.NotFound() : Results.Ok(invoice);
        });

        billing.MapPost("/invoices/{id:guid}/manual-payment", async (Guid id, RecordManualPaymentRequest req, IBillingService service) =>
        {
            var updated = await service.RecordManualPaymentAsync(id, req);
            return Results.Ok(updated);
        });

        billing.MapPost("/invoices/{id:guid}/cancel", async (Guid id, IBillingService service) =>
        {
            var updated = await service.CancelInvoiceAsync(id);
            return Results.Ok(updated);
        });

        billing.MapPost("/invoices/{id:guid}/refund", async (Guid id, ProcessInvoiceRefundRequest req, IBillingService service) =>
        {
            var updated = await service.ProcessRefundAsync(id, req.PaymentId, new ProcessRefundRequest(req.Amount, req.Reason, req.StaffId));
            return Results.Ok(updated);
        });

        billing.MapPost("/invoices/{id:guid}/payment-link/regenerate", async (Guid id, IBillingService service) =>
        {
            var link = await service.RegeneratePaymentLinkAsync(id);
            return Results.Ok(link);
        });

        billing.MapPost("/razorpay-webhook", async (RazorpayWebhookPayload payload, IBillingService service) =>
        {
            var success = await service.HandleRazorpayWebhookAsync(payload.RazorpayPaymentId, payload.Amount, payload.InvoiceId);
            return success ? Results.Ok() : Results.BadRequest();
        });
    }
}

// Request record contracts
public record AddClinicalNoteRequest(Guid StaffId, string Content, Guid? NoteId = null);
public record AddPractitionerNoteRequest(Guid StaffId, string Content);
public record FinalizePrescriptionRequest(Guid StaffId);
public record TransitionLabStatusRequest(string TargetStatus, Guid? StaffId = null);
public record RejectLabOrderRequest(string Comment, Guid StaffId);
public record ProcessInvoiceRefundRequest(Guid PaymentId, decimal Amount, string Reason, Guid StaffId);
public record RazorpayWebhookPayload(string RazorpayPaymentId, decimal Amount, Guid InvoiceId);
