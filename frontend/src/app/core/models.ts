export interface User {
  id: string;
  email: string;
  fullName: string;
  role: string;
  clinicId?: string;
  permissions: string[];
}

export interface ClinicMembership {
  clinicId: string;
  clinicName: string;
  role: string;
}

export interface AuthResponse {
  accessToken?: string;
  refreshToken?: string;
  expiresAt?: string;
  user?: User;
  requiresTenantSelection?: boolean;
  tenantSelectionToken?: string;
  availableClinics?: ClinicMembership[];
}

export interface Patient {
  id: string;
  clinicId: string;
  name: string;
  phone: string;
  dob: string;
  isArchived: boolean;
  isAnonymized: boolean;
  createdAt: string;
}

export interface DuplicatePatientWarning {
  hasPotentialDuplicate: boolean;
  message?: string;
  matches: Patient[];
}

export interface Appointment {
  id: string;
  clinicId: string;
  patientId: string;
  doctorStaffId: string;
  startAt: string;
  endAt: string;
  status: string;
  overrideReason?: string;
  createdAt: string;
}

export interface ClinicalNote {
  id: string;
  consultationId: string;
  staffId: string;
  content: string;
  createdAt: string;
  updatedAt?: string;
}

export interface Consultation {
  id: string;
  clinicId: string;
  patientId: string;
  doctorStaffId: string;
  appointmentId?: string;
  status: string;
  startedAt: string;
  completedAt?: string;
  notes: ClinicalNote[];
}

export interface MateriaMedicaEntry {
  id: string;
  code: string;
  name: string;
  family: string;
  description: string;
  indications: string;
  notes?: { id: string; content: string; createdAt: string }[];
}

export interface PrescriptionItem {
  id: string;
  medicineId: string;
  medicineName: string;
  dilutionId: string;
  dilutionLabel: string;
  vehicleId: string;
  vehicleName: string;
  electricityPolarity: string;
  quantity: number;
  dosage: string;
  duration: string;
}

export interface Prescription {
  id: string;
  clinicId: string;
  consultationId: string;
  previousVersionId?: string;
  version: number;
  status: string;
  finalizedByStaffId?: string;
  createdAt: string;
  finalizedAt?: string;
  items: PrescriptionItem[];
}

export interface LaboratoryOrder {
  id: string;
  clinicId: string;
  prescriptionId: string;
  preparedByStaffId?: string;
  status: string;
  rejectionComment?: string;
  createdAt: string;
  updatedAt?: string;
}

export interface InventoryItem {
  id: string;
  clinicId: string;
  medicineId: string;
  quantityOnHand: number;
  reorderLevel: number;
  isLowStock: boolean;
  createdAt: string;
}

export interface InvoiceLineItem {
  id: string;
  medicineId?: string;
  description: string;
  quantity: number;
  unitPrice: number;
  amount: number;
}

export interface Payment {
  id: string;
  amount: number;
  source: string;
  mode: string;
  status: string;
  razorpayPaymentId?: string;
  createdAt: string;
}

export interface PaymentLink {
  id: string;
  tokenHash: string;
  status: string;
  expiresAt: string;
  consumedAt?: string;
}

export interface Invoice {
  id: string;
  clinicId: string;
  patientId: string;
  consultationId: string;
  prescriptionId?: string;
  status: string;
  totalAmount: number;
  totalPaidAmount: number;
  remainingBalance: number;
  createdAt: string;
  lineItems: InvoiceLineItem[];
  payments: Payment[];
  activePaymentLink?: PaymentLink;
}
