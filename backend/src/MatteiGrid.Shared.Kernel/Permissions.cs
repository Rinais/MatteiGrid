namespace MatteiGrid.Shared.Kernel;

public static class Roles
{
    public const string Doctor = "Doctor";
    public const string Receptionist = "Receptionist";
    public const string LabTechnician = "Lab Technician";
    public const string Administrator = "Administrator";
    public const string SuperAdmin = "SuperAdmin";

    public static readonly IReadOnlyList<string> All = new[]
    {
        Doctor,
        Receptionist,
        LabTechnician,
        Administrator,
        SuperAdmin
    };
}

public static class Permissions
{
    // Patients (pluralized resource, DPDP ErasePii)
    public const string PatientsRead = "Patients.Read";
    public const string PatientsCreate = "Patients.Create";
    public const string PatientsEdit = "Patients.Edit";
    public const string PatientsArchive = "Patients.Archive";
    public const string PatientsErasePii = "Patients.ErasePii";

    // Appointments
    public const string AppointmentsRead = "Appointments.Read";
    public const string AppointmentsCreate = "Appointments.Create";
    public const string AppointmentsUpdate = "Appointments.Update";
    public const string AppointmentsCancel = "Appointments.Cancel";

    // Clinical Notes
    public const string ClinicalNotesRead = "ClinicalNotes.Read";
    public const string ClinicalNotesCreate = "ClinicalNotes.Create";
    public const string ClinicalNotesEdit = "ClinicalNotes.Edit";

    // Consultations
    public const string ConsultationsRead = "Consultations.Read";
    public const string ConsultationsCreate = "Consultations.Create";
    public const string ConsultationsEdit = "Consultations.Edit";

    // Prescriptions (pluralized per matrix)
    public const string PrescriptionsRead = "Prescriptions.Read";
    public const string PrescriptionsCreate = "Prescriptions.Create";
    public const string PrescriptionsEdit = "Prescriptions.Edit";
    public const string PrescriptionsFinalize = "Prescriptions.Finalize";

    // Materia Medica
    public const string MateriaMedicaRead = "MateriaMedica.Read";
    public const string MateriaMedicaEdit = "MateriaMedica.Edit";

    // Laboratory
    public const string LaboratoryRead = "Laboratory.Read";
    public const string LaboratoryUpdateStatus = "Laboratory.UpdateStatus";
    public const string LaboratoryReject = "Laboratory.Reject";

    // Inventory (includes Doctor read per review)
    public const string InventoryRead = "Inventory.Read";
    public const string InventoryManage = "Inventory.Manage";

    // Billing
    public const string BillingRead = "Billing.Read";
    public const string BillingCreate = "Billing.Create";
    public const string BillingManualPayment = "Billing.ManualPayment";
    public const string BillingRefund = "Billing.Refund";
    public const string BillingCancel = "Billing.Cancel";

    // Administration
    public const string AdministrationManageUsers = "Administration.ManageUsers";
    public const string AdministrationAssignRoles = "Administration.AssignRoles";
    public const string AdministrationSettings = "Administration.Settings";
    public const string AdministrationMasterData = "Administration.MasterData";
    public const string AdministrationAuditLogs = "Administration.AuditLogs";
    public const string AdministrationReports = "Administration.Reports";

    // Reports
    public const string ReportsRead = "Reports.Read";

    // SuperAdmin Platform Operations (Zero default clinical access)
    public const string SuperAdminPlatformOps = "SuperAdmin.PlatformOps";
    public const string SuperAdminManageTenants = "SuperAdmin.ManageTenants";
    public const string SuperAdminBreakGlass = "SuperAdmin.BreakGlass";

    #region Backward Compatibility Aliases
    public const string PatientView = PatientsRead;
    public const string PatientCreate = PatientsCreate;
    public const string PatientEdit = PatientsEdit;
    public const string PatientArchive = PatientsArchive;
    public const string PatientErase = PatientsErasePii;

    public const string AppointmentView = AppointmentsRead;
    public const string AppointmentEdit = AppointmentsUpdate;

    public const string ClinicalNoteView = ClinicalNotesRead;
    public const string ClinicalNoteCreate = ClinicalNotesCreate;
    public const string ClinicalNoteEdit = ClinicalNotesEdit;

    public const string ConsultationView = ConsultationsRead;
    public const string ConsultationCreate = ConsultationsCreate;

    public const string PrescriptionView = PrescriptionsRead;
    public const string PrescriptionCreate = PrescriptionsCreate;
    public const string PrescriptionEdit = PrescriptionsEdit;
    public const string PrescriptionFinalize = PrescriptionsFinalize;

    public const string MateriaMedicaView = MateriaMedicaRead;
    public const string LaboratoryView = LaboratoryRead;
    public const string InventoryView = InventoryRead;
    public const string InventoryEdit = InventoryManage;
    public const string BillingView = BillingRead;

    public const string AdminUsers = AdministrationManageUsers;
    public const string AdminRoles = AdministrationAssignRoles;
    public const string AdminSettings = AdministrationSettings;
    public const string AdminMasterData = AdministrationMasterData;
    public const string AdminAuditLogs = AdministrationAuditLogs;
    public const string AdminReports = AdministrationReports;

    public const string ReportsView = ReportsRead;
    #endregion

    public static readonly IReadOnlyList<string> All = new[]
    {
        PatientsRead, PatientsCreate, PatientsEdit, PatientsArchive, PatientsErasePii,
        AppointmentsRead, AppointmentsCreate, AppointmentsUpdate, AppointmentsCancel,
        ClinicalNotesRead, ClinicalNotesCreate, ClinicalNotesEdit,
        ConsultationsRead, ConsultationsCreate, ConsultationsEdit,
        PrescriptionsRead, PrescriptionsCreate, PrescriptionsEdit, PrescriptionsFinalize,
        MateriaMedicaRead, MateriaMedicaEdit,
        LaboratoryRead, LaboratoryUpdateStatus, LaboratoryReject,
        InventoryRead, InventoryManage,
        BillingRead, BillingCreate, BillingManualPayment, BillingRefund, BillingCancel,
        AdministrationManageUsers, AdministrationAssignRoles, AdministrationSettings, AdministrationMasterData, AdministrationAuditLogs, AdministrationReports,
        ReportsRead,
        SuperAdminPlatformOps, SuperAdminManageTenants, SuperAdminBreakGlass
    };

    public static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> RolePermissions =
        new Dictionary<string, IReadOnlyList<string>>
        {
            [Roles.Doctor] = new[]
            {
                PatientsRead, PatientsCreate, PatientsEdit,
                AppointmentsRead,
                ClinicalNotesRead, ClinicalNotesCreate, ClinicalNotesEdit,
                ConsultationsRead, ConsultationsCreate, ConsultationsEdit,
                PrescriptionsRead, PrescriptionsCreate, PrescriptionsEdit, PrescriptionsFinalize,
                MateriaMedicaRead,
                LaboratoryRead,
                InventoryRead,
                BillingRead, BillingRefund, BillingCancel,
                ReportsRead
            },
            [Roles.Receptionist] = new[]
            {
                PatientsRead, PatientsCreate, PatientsEdit, PatientsArchive,
                AppointmentsRead, AppointmentsCreate, AppointmentsUpdate, AppointmentsCancel,
                BillingRead, BillingManualPayment,
                ReportsRead
            },
            [Roles.LabTechnician] = new[]
            {
                LaboratoryRead, LaboratoryUpdateStatus, LaboratoryReject,
                ReportsRead
            },
            [Roles.Administrator] = new[]
            {
                PatientsRead, PatientsCreate, PatientsEdit, PatientsArchive, PatientsErasePii,
                AppointmentsRead, AppointmentsCreate, AppointmentsUpdate, AppointmentsCancel,
                MateriaMedicaRead, MateriaMedicaEdit,
                InventoryRead, InventoryManage,
                BillingRead, BillingCreate, BillingManualPayment, BillingRefund, BillingCancel,
                AdministrationManageUsers, AdministrationAssignRoles, AdministrationSettings, AdministrationMasterData, AdministrationAuditLogs, AdministrationReports,
                ReportsRead
            },
            [Roles.SuperAdmin] = new[]
            {
                SuperAdminPlatformOps, SuperAdminManageTenants, SuperAdminBreakGlass
            }
        };
}
