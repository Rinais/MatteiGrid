namespace MatteiGrid.Shared.Kernel;

public static class Permissions
{
    // Patient
    public const string PatientView = "Patient.View";
    public const string PatientCreate = "Patient.Create";
    public const string PatientEdit = "Patient.Edit";
    public const string PatientArchive = "Patient.Archive";
    public const string PatientErase = "Patient.Erase";

    // Appointment
    public const string AppointmentView = "Appointment.View";
    public const string AppointmentCreate = "Appointment.Create";
    public const string AppointmentEdit = "Appointment.Edit";
    public const string AppointmentCancel = "Appointment.Cancel";

    // Clinical
    public const string ClinicalNoteView = "ClinicalNote.View";
    public const string ClinicalNoteCreate = "ClinicalNote.Create";
    public const string ClinicalNoteEdit = "ClinicalNote.Edit";

    // Consultation
    public const string ConsultationView = "Consultation.View";
    public const string ConsultationCreate = "Consultation.Create";

    // Prescription
    public const string PrescriptionView = "Prescription.View";
    public const string PrescriptionCreate = "Prescription.Create";
    public const string PrescriptionEdit = "Prescription.Edit";
    public const string PrescriptionFinalize = "Prescription.Finalize";

    // Materia Medica
    public const string MateriaMedicaView = "MateriaMedica.View";
    public const string MateriaMedicaEdit = "MateriaMedica.Edit";

    // Laboratory
    public const string LaboratoryView = "Laboratory.View";
    public const string LaboratoryUpdateStatus = "Laboratory.UpdateStatus";
    public const string LaboratoryReject = "Laboratory.Reject";

    // Inventory
    public const string InventoryView = "Inventory.View";
    public const string InventoryEdit = "Inventory.Edit";

    // Billing
    public const string BillingView = "Billing.View";
    public const string BillingCreate = "Billing.Create";
    public const string BillingManualPayment = "Billing.ManualPayment";
    public const string BillingRefund = "Billing.Refund";
    public const string BillingCancel = "Billing.Cancel";

    // Admin
    public const string AdminUsers = "Admin.Users";
    public const string AdminRoles = "Admin.Roles";
    public const string AdminSettings = "Admin.Settings";
    public const string AdminMasterData = "Admin.MasterData";
    public const string AdminAuditLogs = "Admin.AuditLogs";
    public const string AdminReports = "Admin.Reports";

    // Reports
    public const string ReportsView = "Reports.View";

    public static readonly IReadOnlyList<string> All = new[]
    {
        PatientView, PatientCreate, PatientEdit, PatientArchive, PatientErase,
        AppointmentView, AppointmentCreate, AppointmentEdit, AppointmentCancel,
        ClinicalNoteView, ClinicalNoteCreate, ClinicalNoteEdit,
        ConsultationView, ConsultationCreate,
        PrescriptionView, PrescriptionCreate, PrescriptionEdit, PrescriptionFinalize,
        MateriaMedicaView, MateriaMedicaEdit,
        LaboratoryView, LaboratoryUpdateStatus, LaboratoryReject,
        InventoryView, InventoryEdit,
        BillingView, BillingCreate, BillingManualPayment, BillingRefund, BillingCancel,
        AdminUsers, AdminRoles, AdminSettings, AdminMasterData, AdminAuditLogs, AdminReports,
        ReportsView
    };

    public static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> RolePermissions =
        new Dictionary<string, IReadOnlyList<string>>
        {
            ["Doctor"] = new[]
            {
                PatientView, PatientCreate, PatientEdit,
                AppointmentView,
                ClinicalNoteView, ClinicalNoteCreate, ClinicalNoteEdit,
                ConsultationView, ConsultationCreate,
                PrescriptionView, PrescriptionCreate, PrescriptionEdit, PrescriptionFinalize,
                MateriaMedicaView,
                LaboratoryView,
                BillingView, BillingRefund, BillingCancel,
                ReportsView
            },
            ["Receptionist"] = new[]
            {
                PatientView, PatientCreate, PatientEdit, PatientArchive,
                AppointmentView, AppointmentCreate, AppointmentEdit, AppointmentCancel,
                BillingView, BillingManualPayment,
                ReportsView
            },
            ["Lab Technician"] = new[]
            {
                PatientView,
                LaboratoryView, LaboratoryUpdateStatus, LaboratoryReject,
                ReportsView
            },
            ["Administrator"] = new[]
            {
                PatientView, PatientCreate, PatientEdit, PatientArchive, PatientErase,
                AppointmentView, AppointmentCreate, AppointmentEdit, AppointmentCancel,
                MateriaMedicaView, MateriaMedicaEdit,
                InventoryView, InventoryEdit,
                BillingView, BillingCreate, BillingManualPayment, BillingRefund, BillingCancel,
                AdminUsers, AdminRoles, AdminSettings, AdminMasterData, AdminAuditLogs, AdminReports,
                ReportsView
            }
        };
}
