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
  gender?: string;
  primaryChiefComplaint?: string;
  profileStatus: 'Incomplete' | 'Complete' | string;
  completionPercentage: number;
  missingRequiredFields?: string[];
  isArchived: boolean;
  isAnonymized: boolean;
  createdAt: string;
}

export interface HomeopathicCaseRecord {
  id: string;
  patientId: string;
  clinicId: string;
  occupation?: string;
  maritalStatus?: string;
  address?: string;
  diet?: string;
  placeOfBirth?: string;
  religion?: string;
  education?: string;
  socialStatus?: string;
  nutritionalStatus?: string;
  familyStatus?: string;
  // Presenting Complaints & Modalities
  chiefComplaintTimeline?: string;
  location?: string;
  locationSensation?: string;
  sensationAndPathology?: string;
  aggravatingModalities?: string;
  amelioratingModalities?: string;
  ailmentsFrom?: string;
  concomitantSymptoms?: string;
  // Physical Generals: Thermals
  thermals?: string;
  thermalTolerance?: string;
  // Physical Generals: Functional
  appetite?: string;
  thirst?: string;
  sex?: string;
  sleep?: string;
  sleepPattern?: string;
  dreams?: string;
  cravingsAversions?: string;
  // Physical Generals: Elimination
  stool?: string;
  urine?: string;
  sweat?: string;
  breath?: string;
  discharges?: string;
  eliminationBowelUrinePerspiration?: string;
  // Mental Generals
  willAndEmotions?: string;
  understandingAndIntellect?: string;
  memory?: string;
  intellectAndMemory?: string;
  mindDisposition?: string;
  emotionalTriggers?: string;
  temperament?: string;
  // Medical History
  pastMedicalSurgicalHistory?: string;
  familyMedicalHistory?: string;
  childhoodDiseasesVaccination?: string;
  // General Examination & Vitals
  consciousState?: string;
  generalAppearance?: string;
  buildAndAppearance?: string;
  builtAndNutrition?: string;
  systemicSigns?: string;
  skinAndNails?: string;
  bloodPressure?: string;
  pulseRate?: string;
  temperatureF?: number;
  respiratoryRate?: string;
  weightKg?: number;
  heightCm?: number;
  bmi?: number;
  systemicExamNotes?: string;
  // Allergies
  knownAllergies?: string;
  hasKnownAllergies?: boolean;
  allergyAssessmentConfirmed?: boolean;
  isCompleted: boolean;
  createdAt: string;
  updatedAt?: string;
}

export interface PatientDetailedProfile {
  patient: Patient;
  caseRecord?: HomeopathicCaseRecord;
  missingRequiredFields: string[];
}

export interface UpdateHomeopathicCaseRecordRequest {
  occupation?: string;
  maritalStatus?: string;
  address?: string;
  diet?: string;
  placeOfBirth?: string;
  religion?: string;
  education?: string;
  socialStatus?: string;
  nutritionalStatus?: string;
  familyStatus?: string;
  // Presenting Complaints & Modalities
  chiefComplaintTimeline?: string;
  location?: string;
  locationSensation?: string;
  sensationAndPathology?: string;
  aggravatingModalities?: string;
  amelioratingModalities?: string;
  ailmentsFrom?: string;
  concomitantSymptoms?: string;
  // Physical Generals: Thermals
  thermals?: string;
  thermalTolerance?: string;
  // Physical Generals: Functional
  appetite?: string;
  thirst?: string;
  sex?: string;
  sleep?: string;
  sleepPattern?: string;
  dreams?: string;
  cravingsAversions?: string;
  // Physical Generals: Elimination
  stool?: string;
  urine?: string;
  sweat?: string;
  breath?: string;
  discharges?: string;
  eliminationBowelUrinePerspiration?: string;
  // Mental Generals
  willAndEmotions?: string;
  understandingAndIntellect?: string;
  memory?: string;
  intellectAndMemory?: string;
  mindDisposition?: string;
  emotionalTriggers?: string;
  temperament?: string;
  // Medical History
  pastMedicalSurgicalHistory?: string;
  familyMedicalHistory?: string;
  childhoodDiseasesVaccination?: string;
  // General Examination & Vitals
  consciousState?: string;
  generalAppearance?: string;
  buildAndAppearance?: string;
  builtAndNutrition?: string;
  systemicSigns?: string;
  skinAndNails?: string;
  bloodPressure?: string;
  pulseRate?: string;
  temperatureF?: number;
  respiratoryRate?: string;
  weightKg?: number;
  heightCm?: number;
  bmi?: number;
  systemicExamNotes?: string;
  // Allergies
  knownAllergies?: string;
  hasKnownAllergies?: boolean;
  allergyAssessmentConfirmed?: boolean;
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

export interface ConsultationObservation {
  id: string;
  consultationId: string;
  staffId: string;
  observationDate: string;
  reactionType: string;
  kentsObservation?: string;
  secondPrescriptionAction?: string;
  notes: string;
  createdAt: string;
}

export interface Consultation {
  id: string;
  clinicId: string;
  patientId: string;
  doctorStaffId: string;
  appointmentId?: string;
  status: string;
  caseType: string;
  provisionalDiagnosis: string;
  finalDiagnosis: string;
  outcome: string;
  startedAt: string;
  completedAt?: string;
  notes: ClinicalNote[];
  observations: ConsultationObservation[];
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
  selectionMethod?: 'Repertorial' | 'NonRepertorial' | string;
  frequency?: string;
  instructions?: string;
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
  dietAndRegimenInstructions?: string;
  generalInstructions?: string;
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

export interface AuditLog {
  id: string;
  clinicId?: string;
  eventType: string;
  action: string;
  entityName?: string;
  entityId?: string;
  details?: string;
  userId?: string;
  userEmail?: string;
  createdAt: string;
}

