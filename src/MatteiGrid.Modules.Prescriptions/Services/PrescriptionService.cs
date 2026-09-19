using MatteiGrid.Modules.Prescriptions.Contracts;
using MatteiGrid.Modules.Prescriptions.Domain;
using MatteiGrid.Shared.Infrastructure.Persistence;
using MatteiGrid.Shared.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;

using MatteiGrid.Shared.Kernel;

namespace MatteiGrid.Modules.Prescriptions.Services;

public class PrescriptionService : IPrescriptionService
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentTenantContext _tenantContext;
    private readonly IPaymentLockChecker? _paymentLockChecker;

    public PrescriptionService(
        AppDbContext dbContext,
        ICurrentTenantContext tenantContext,
        IPaymentLockChecker? paymentLockChecker = null)
    {
        _dbContext = dbContext;
        _tenantContext = tenantContext;
        _paymentLockChecker = paymentLockChecker;
    }

    public async Task<PrescriptionDto> CreatePrescriptionAsync(CreatePrescriptionRequest request, CancellationToken ct = default)
    {
        var prescription = new Prescription
        {
            ClinicId = _tenantContext.ClinicId ?? Guid.Empty,
            ConsultationId = request.ConsultationId,
            Version = 1,
            Status = PrescriptionStatus.Draft
        };

        foreach (var itemReq in request.Items)
        {
            ValidateElectricity(itemReq.ElectricityPolarity);

            prescription.Items.Add(new PrescriptionItem
            {
                ClinicId = _tenantContext.ClinicId ?? Guid.Empty,
                MedicineId = itemReq.MedicineId,
                DilutionId = itemReq.DilutionId,
                VehicleId = itemReq.VehicleId,
                ElectricityPolarity = itemReq.ElectricityPolarity,
                Quantity = itemReq.Quantity,
                Dosage = itemReq.Dosage,
                Duration = itemReq.Duration
            });
        }

        _dbContext.Set<Prescription>().Add(prescription);
        await _dbContext.SaveChangesAsync(ct);

        return await GetPrescriptionByIdAsync(prescription.Id, ct)
            ?? throw new InvalidOperationException("Failed to load created prescription.");
    }

    public async Task<PrescriptionDto> RevisePrescriptionAsync(RevisePrescriptionRequest request, CancellationToken ct = default)
    {
        var prev = await _dbContext.Set<Prescription>()
            .FirstOrDefaultAsync(p => p.Id == request.PreviousPrescriptionId, ct)
            ?? throw new InvalidOperationException("Previous prescription not found.");

        if (prev.Status != PrescriptionStatus.Finalized)
            throw new InvalidOperationException("Only finalized prescriptions can be revised.");

        if (_paymentLockChecker != null)
        {
            if (await _paymentLockChecker.HasConfirmedPaymentForPrescriptionAsync(prev.Id, ct))
            {
                throw new InvalidOperationException("Prescription revision is blocked once any payment has been recorded against its invoice.");
            }
        }
        else if (_dbContext.Database.IsNpgsql())
        {
            var hasPayment = await _dbContext.Database.SqlQueryRaw<int>(
                "SELECT COUNT(1)::int AS \"Value\" FROM \"Payments\" p " +
                "JOIN \"Invoices\" i ON p.\"InvoiceId\" = i.\"Id\" " +
                "WHERE i.\"PrescriptionId\" = {0} AND p.\"Status\" = 'Confirmed'", prev.Id)
                .FirstOrDefaultAsync(ct);

            if (hasPayment > 0)
            {
                throw new InvalidOperationException("Prescription revision is blocked once any payment has been recorded against its invoice.");
            }
        }

        var revised = new Prescription
        {
            ClinicId = _tenantContext.ClinicId ?? Guid.Empty,
            ConsultationId = prev.ConsultationId,
            PreviousVersionId = prev.Id,
            Version = prev.Version + 1,
            Status = PrescriptionStatus.Draft
        };

        foreach (var itemReq in request.Items)
        {
            ValidateElectricity(itemReq.ElectricityPolarity);

            revised.Items.Add(new PrescriptionItem
            {
                ClinicId = _tenantContext.ClinicId ?? Guid.Empty,
                MedicineId = itemReq.MedicineId,
                DilutionId = itemReq.DilutionId,
                VehicleId = itemReq.VehicleId,
                ElectricityPolarity = itemReq.ElectricityPolarity,
                Quantity = itemReq.Quantity,
                Dosage = itemReq.Dosage,
                Duration = itemReq.Duration
            });
        }

        _dbContext.Set<Prescription>().Add(revised);
        await _dbContext.SaveChangesAsync(ct);

        return await GetPrescriptionByIdAsync(revised.Id, ct)
            ?? throw new InvalidOperationException("Failed to load revised prescription.");
    }

    public async Task<PrescriptionDto> FinalizePrescriptionAsync(Guid prescriptionId, Guid staffId, CancellationToken ct = default)
    {
        var prescription = await _dbContext.Set<Prescription>()
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == prescriptionId, ct)
            ?? throw new InvalidOperationException("Prescription not found.");

        if (prescription.Status == PrescriptionStatus.Finalized)
            throw new InvalidOperationException("Prescription is already finalized.");

        if (prescription.Items.Count == 0)
            throw new InvalidOperationException("Cannot finalize an empty prescription.");

        prescription.Status = PrescriptionStatus.Finalized;
        prescription.FinalizedByStaffId = staffId;
        prescription.FinalizedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync(ct);

        return await GetPrescriptionByIdAsync(prescription.Id, ct)
            ?? throw new InvalidOperationException("Failed to load finalized prescription.");
    }

    public async Task<PrescriptionDto?> GetPrescriptionByIdAsync(Guid id, CancellationToken ct = default)
    {
        var p = await _dbContext.Set<Prescription>()
            .Include(x => x.Items)
                .ThenInclude(i => i.Medicine)
            .Include(x => x.Items)
                .ThenInclude(i => i.Dilution)
            .Include(x => x.Items)
                .ThenInclude(i => i.Vehicle)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        return p is null ? null : MapDto(p);
    }

    public async Task<IReadOnlyList<PrescriptionDto>> GetPrescriptionsByConsultationIdAsync(Guid consultationId, CancellationToken ct = default)
    {
        var list = await _dbContext.Set<Prescription>()
            .Include(x => x.Items)
                .ThenInclude(i => i.Medicine)
            .Include(x => x.Items)
                .ThenInclude(i => i.Dilution)
            .Include(x => x.Items)
                .ThenInclude(i => i.Vehicle)
            .Where(x => x.ConsultationId == consultationId)
            .OrderByDescending(x => x.Version)
            .ToListAsync(ct);

        return list.Select(MapDto).ToList();
    }

    private static void ValidateElectricity(string polarity)
    {
        if (!Electricities.All.Contains(polarity))
            throw new ArgumentException($"Invalid electricity polarity '{polarity}'. Allowed values: Red, Blue, Green, Yellow, White.", nameof(polarity));
    }

    private static PrescriptionDto MapDto(Prescription p) =>
        new(
            p.Id,
            p.ClinicId,
            p.ConsultationId,
            p.PreviousVersionId,
            p.Version,
            p.Status,
            p.FinalizedByStaffId,
            p.CreatedAt,
            p.FinalizedAt,
            p.Items.Select(i => new PrescriptionItemDto(
                i.Id,
                i.MedicineId,
                i.Medicine?.Name ?? "Medicine",
                i.DilutionId,
                i.Dilution?.Label ?? "Dilution",
                i.VehicleId,
                i.Vehicle?.Name ?? "Vehicle",
                i.ElectricityPolarity,
                i.Quantity,
                i.Dosage,
                i.Duration
            )).ToList()
        );
}