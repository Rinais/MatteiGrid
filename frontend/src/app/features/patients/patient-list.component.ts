import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ApiService } from '../../core/api.service';
import { Patient, DuplicatePatientWarning } from '../../core/models';

@Component({
  selector: 'app-patient-list',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="patients-container">
      <div class="patients-header">
        <div>
          <h1 class="page-title">Patient Registry</h1>
          <p class="page-subtitle">Centralized medical records, progressive case profiling, and DPDP-compliant consent tracking</p>
        </div>
        <button class="btn btn-primary" (click)="openRegisterModal()">+ Quick Intake Registration</button>
      </div>

      <!-- Search Bar -->
      <div class="search-bar">
        <input
          type="text"
          placeholder="Search by patient name, phone number, or chief complaint..."
          [(ngModel)]="searchQuery"
          (ngModelChange)="onSearch()"
          class="search-input"
        />
      </div>

      <!-- Patient Table -->
      <div class="table-container">
        <table>
          <thead>
            <tr>
              <th>Patient & Demographics</th>
              <th>Phone</th>
              <th>Primary Complaint</th>
              <th>Dossier Completion</th>
              <th>DPDP Status</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            <tr *ngFor="let p of patients()">
              <td>
                <div class="patient-name" [class.anonymized]="p.isAnonymized">
                  {{ p.name }}
                  <span *ngIf="p.isAnonymized" class="badge-anonymized">PII Anonymized</span>
                </div>
                <div class="patient-submeta">
                  <span>{{ p.gender || 'Unknown' }}</span>
                  <span class="dot">•</span>
                  <span>DOB: {{ p.dob }}</span>
                </div>
              </td>
              <td>{{ p.phone }}</td>
              <td>
                <div class="complaint-text" [title]="p.primaryChiefComplaint || 'None recorded'">
                  {{ p.primaryChiefComplaint || '—' }}
                </div>
              </td>
              <td>
                <div class="completion-wrapper">
                  <div *ngIf="p.completionPercentage === 100 || p.profileStatus === 'Complete'" class="badge-complete">
                    <span class="icon">✅</span> Complete (100%)
                  </div>
                  <div *ngIf="p.completionPercentage !== 100 && p.profileStatus !== 'Complete'" class="incomplete-block">
                    <span class="badge-incomplete" [title]="getMissingTooltip(p)">
                      ⚠️ Incomplete ({{ p.completionPercentage || 40 }}%)
                    </span>
                    <button class="btn-dossier-link" (click)="goToCaseDossier(p.id)" title="Open Homeopathic Case Dossier">
                      Complete Dossier →
                    </button>
                  </div>
                </div>
              </td>
              <td>
                <span class="status-pill active">Consent Captured</span>
              </td>
              <td>
                <div class="actions-group">
                  <button
                    class="btn-action primary"
                    (click)="goToCaseDossier(p.id)"
                    title="Open Detailed Homeopathic Case Record"
                  >
                    📋 Dossier
                  </button>
                  <button
                    class="btn-action danger"
                    (click)="confirmErasePii(p)"
                    [disabled]="p.isAnonymized"
                    title="DPDP Act 2023 Right to Erasure"
                  >
                    {{ p.isAnonymized ? 'Anonymized' : 'Anonymize PII' }}
                  </button>
                </div>
              </td>
            </tr>
            <tr *ngIf="patients().length === 0">
              <td colspan="6" class="text-center text-muted">No patients found.</td>
            </tr>
          </tbody>
        </table>
      </div>

      <!-- Quick Intake Modal (Tier 1) -->
      <div class="modal-backdrop" *ngIf="showRegisterModal">
        <div class="modal-dialog">
          <div class="modal-header">
            <div>
              <h3>Quick Intake Registration</h3>
              <p class="modal-subtitle">Tier 1: Fast check-in. Detailed case record can be completed anytime.</p>
            </div>
            <button class="modal-close" (click)="closeRegisterModal()">×</button>
          </div>
          <div class="modal-body">
            <!-- Advisory Duplicate Match Warning Banner -->
            <div class="warning-banner" *ngIf="duplicateWarning()?.hasPotentialDuplicate">
              <strong>⚠️ Potential Duplicate Advisory:</strong>
              <p>{{ duplicateWarning()?.message }}</p>
              <ul>
                <li *ngFor="let m of duplicateWarning()?.matches">
                  {{ m.name }} ({{ m.phone }}, DOB: {{ m.dob }})
                </li>
              </ul>
              <small class="text-muted">Staff may proceed with registration or merge records per PRD 5.2.</small>
            </div>

            <div class="form-group">
              <label>Full Name *</label>
              <input type="text" [(ngModel)]="form.name" class="form-control" placeholder="e.g. Anand Sharma" />
            </div>

            <div class="form-group">
              <label>Phone Number *</label>
              <input
                type="tel"
                inputmode="numeric"
                autocomplete="tel"
                maxlength="10"
                [value]="form.phone"
                (input)="onPhoneInput($event)"
                (keydown)="onPhoneKeyDown($event)"
                (blur)="phoneTouched = true"
                class="form-control"
                [class.input-error]="phoneTouched && !isPhoneValid()"
                placeholder="e.g. 9876543210"
              />
              <div class="error-feedback" *ngIf="phoneTouched && !isPhoneValid()">
                <span *ngIf="!form.phone">Phone number is required.</span>
                <span *ngIf="form.phone">Please enter a valid 10-digit phone number.</span>
              </div>
            </div>

            <div class="form-row">
              <div class="form-group half">
                <label>Date of Birth *</label>
                <input type="date" [(ngModel)]="form.dob" class="form-control" />
              </div>

              <div class="form-group half">
                <label>Gender *</label>
                <select [(ngModel)]="form.gender" class="form-control">
                  <option value="Male">Male</option>
                  <option value="Female">Female</option>
                  <option value="Other">Other</option>
                </select>
              </div>
            </div>

            <div class="form-group">
              <label>Primary Chief Complaint *</label>
              <textarea
                [(ngModel)]="form.primaryChiefComplaint"
                rows="2"
                class="form-control"
                placeholder="e.g. Persistent migraine with right-sided throbbing pain and nausea"
              ></textarea>
              <small class="field-hint">Initial presentation summary for doctor triage & baseline intake.</small>
            </div>

            <div class="form-group checkbox-group">
              <label>
                <input type="checkbox" [(ngModel)]="form.consentAccepted" />
                Explicit DPDP Act 2023 consent captured for medical treatment and reminders.
              </label>
            </div>
          </div>
          <div class="modal-footer">
            <button class="btn btn-outline" (click)="closeRegisterModal()">Cancel</button>
            <button
              class="btn btn-primary"
              (click)="submitRegistration()"
              [disabled]="!form.name || !isPhoneValid() || !form.dob || !form.gender || !form.primaryChiefComplaint.trim()"
            >
              Admit & Create Record (40%)
            </button>
          </div>
        </div>
      </div>

      <!-- DPDP Erasure Confirmation Modal -->
      <div class="modal-backdrop" *ngIf="erasingPatient">
        <div class="modal-dialog">
          <div class="modal-header danger">
            <h3>DPDP Act 2023: Right to Erasure</h3>
            <button class="modal-close" (click)="erasingPatient = null">×</button>
          </div>
          <div class="modal-body">
            <p><strong>Are you sure you want to anonymize PII for {{ erasingPatient.name }}?</strong></p>
            <p class="text-muted">
              Per Section 7 & 13 of PRD, this will irreversibly anonymize identifiable personal data (Name, Phone, DOB, Case Demographics).
              Underlying consultation and financial ledger records are retained in de-identified form for statutory medical audit.
            </p>
          </div>
          <div class="modal-footer">
            <button class="btn btn-outline" (click)="erasingPatient = null">Cancel</button>
            <button class="btn btn-danger" (click)="executeErasePii()">Confirm Anonymization</button>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .patients-container { padding: 1.5rem 2rem; max-width: 1300px; margin: 0 auto; }
    .patients-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 1.5rem; }
    .page-title { font-size: 1.75rem; font-weight: 700; color: #111827; margin: 0; }
    .page-subtitle { color: #6b7280; margin: 0.25rem 0 0; font-size: 0.9rem; }
    .search-bar { margin-bottom: 1.5rem; }
    .search-input {
      width: 100%;
      padding: 0.75rem 1rem;
      border: 1px solid #d1d5db;
      border-radius: 6px;
      font-size: 0.95rem;
    }
    .table-container {
      background: white;
      border: 1px solid #e5e7eb;
      border-radius: 8px;
      box-shadow: 0 1px 3px rgba(0,0,0,0.05);
      overflow-x: auto;
    }
    table { width: 100%; border-collapse: collapse; font-size: 0.875rem; }
    th { text-align: left; padding: 0.85rem 1rem; background: #f9fafb; color: #4b5563; border-bottom: 1px solid #e5e7eb; }
    td { padding: 0.85rem 1rem; border-bottom: 1px solid #f3f4f6; vertical-align: middle; }
    .patient-name { font-weight: 600; color: #111827; display: flex; align-items: center; gap: 0.5rem; }
    .patient-name.anonymized { color: #6b7280; font-style: italic; }
    .patient-submeta { font-size: 0.75rem; color: #6b7280; margin-top: 0.15rem; display: flex; align-items: center; gap: 0.35rem; }
    .dot { font-size: 0.7rem; color: #9ca3af; }
    .complaint-text { max-width: 240px; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; color: #374151; font-size: 0.825rem; }
    .badge-anonymized {
      font-size: 0.7rem;
      background: #f3f4f6;
      color: #4b5563;
      padding: 0.1rem 0.4rem;
      border-radius: 4px;
      border: 1px solid #d1d5db;
    }
    .completion-wrapper { display: flex; flex-direction: column; gap: 0.25rem; }
    .badge-complete {
      display: inline-flex;
      align-items: center;
      gap: 0.3rem;
      font-size: 0.75rem;
      font-weight: 600;
      color: #065f46;
      background: #d1fae5;
      padding: 0.2rem 0.55rem;
      border-radius: 9999px;
      width: fit-content;
      border: 1px solid #a7f3d0;
    }
    .incomplete-block { display: flex; flex-direction: column; gap: 0.25rem; align-items: flex-start; }
    .badge-incomplete {
      display: inline-flex;
      align-items: center;
      gap: 0.3rem;
      font-size: 0.75rem;
      font-weight: 600;
      color: #92400e;
      background: #fef3c7;
      padding: 0.2rem 0.55rem;
      border-radius: 9999px;
      cursor: help;
      border: 1px solid #fde68a;
    }
    .btn-dossier-link {
      background: none;
      border: none;
      color: #2563eb;
      padding: 0;
      font-size: 0.75rem;
      font-weight: 600;
      cursor: pointer;
      text-decoration: underline;
    }
    .btn-dossier-link:hover { color: #1d4ed8; }
    .status-pill {
      font-size: 0.75rem;
      padding: 0.2rem 0.5rem;
      border-radius: 9999px;
      background: #e0f2fe;
      color: #0369a1;
      font-weight: 500;
    }
    .actions-group { display: flex; align-items: center; gap: 0.4rem; }
    .btn { padding: 0.5rem 1rem; border-radius: 6px; font-weight: 500; font-size: 0.875rem; cursor: pointer; border: 1px solid transparent; }
    .btn-primary { background: #10b981; color: white; }
    .btn-primary:hover { background: #059669; }
    .btn-outline { background: white; border-color: #d1d5db; color: #374151; }
    .btn-danger { background: #ef4444; color: white; }
    .btn-action {
      background: white;
      border: 1px solid #e5e7eb;
      padding: 0.3rem 0.6rem;
      border-radius: 5px;
      font-size: 0.75rem;
      font-weight: 500;
      cursor: pointer;
    }
    .btn-action.primary { color: #2563eb; border-color: #bfdbfe; background: #eff6ff; }
    .btn-action.primary:hover { background: #dbeafe; }
    .btn-action.danger { color: #dc2626; border-color: #fecaca; }
    .btn-action.danger:hover { background: #fef2f2; }
    .btn-action:disabled { opacity: 0.5; cursor: not-allowed; }
    .modal-backdrop {
      position: fixed; top: 0; left: 0; right: 0; bottom: 0;
      background: rgba(0,0,0,0.5);
      display: flex; align-items: center; justify-content: center;
      z-index: 1000;
    }
    .modal-dialog {
      background: white; border-radius: 8px; width: 520px; max-width: 90%;
      box-shadow: 0 20px 25px -5px rgba(0,0,0,0.1);
    }
    .modal-header { padding: 1rem 1.25rem; border-bottom: 1px solid #e5e7eb; display: flex; justify-content: space-between; align-items: flex-start; }
    .modal-header h3 { margin: 0; font-size: 1.15rem; color: #111827; }
    .modal-subtitle { margin: 0.2rem 0 0; font-size: 0.75rem; color: #6b7280; }
    .modal-header.danger h3 { color: #dc2626; }
    .modal-close { background: none; border: none; font-size: 1.5rem; cursor: pointer; color: #9ca3af; }
    .modal-body { padding: 1.25rem; max-height: 75vh; overflow-y: auto; }
    .modal-footer { padding: 0.75rem 1.25rem; border-top: 1px solid #e5e7eb; display: flex; justify-content: flex-end; gap: 0.75rem; }
    .form-row { display: flex; gap: 1rem; }
    .form-group { margin-bottom: 1rem; }
    .form-group.half { flex: 1; margin-bottom: 1rem; }
    .form-group label { display: block; font-size: 0.85rem; font-weight: 500; margin-bottom: 0.35rem; color: #374151; }
    .form-control { width: 100%; padding: 0.5rem 0.75rem; border: 1px solid #d1d5db; border-radius: 4px; box-sizing: border-box; font-size: 0.9rem; font-family: inherit; }
    .field-hint { display: block; font-size: 0.75rem; color: #6b7280; margin-top: 0.25rem; }
    .input-error { border-color: #ef4444 !important; background-color: #fef2f2; }
    .error-feedback { color: #dc2626; font-size: 0.75rem; margin-top: 0.25rem; }
    .checkbox-group label { display: flex; align-items: flex-start; gap: 0.5rem; font-size: 0.8rem; color: #4b5563; }
    .warning-banner {
      background: #fef3c7; border: 1px solid #f59e0b; border-radius: 6px;
      padding: 0.75rem; margin-bottom: 1rem; font-size: 0.85rem; color: #92400e;
    }
    .warning-banner ul { margin: 0.5rem 0; padding-left: 1.25rem; }
    .text-muted { color: #6b7280; }
    .text-center { text-align: center; }
  `]
})
export class PatientListComponent implements OnInit {
  api = inject(ApiService);
  router = inject(Router);

  patients = signal<Patient[]>([]);
  searchQuery = '';
  showRegisterModal = false;
  erasingPatient: Patient | null = null;
  duplicateWarning = signal<DuplicatePatientWarning | null>(null);
  phoneTouched = false;

  form = {
    name: '',
    phone: '',
    dob: '',
    gender: 'Male',
    primaryChiefComplaint: '',
    consentAccepted: true
  };

  ngOnInit() {
    this.loadPatients();
  }

  openRegisterModal() {
    this.resetForm();
    this.showRegisterModal = true;
  }

  closeRegisterModal() {
    this.showRegisterModal = false;
    this.resetForm();
  }

  loadPatients() {
    this.api.getPatients(this.searchQuery).subscribe(p => this.patients.set(p));
  }

  onSearch() {
    this.loadPatients();
  }

  isPhoneValid(): boolean {
    if (!this.form.phone) return false;
    return /^[0-9]{10}$/.test(this.form.phone);
  }

  onPhoneKeyDown(event: KeyboardEvent) {
    const allowedControlKeys = [
      'Backspace', 'Delete', 'Tab', 'Escape', 'Enter',
      'ArrowLeft', 'ArrowRight', 'ArrowUp', 'ArrowDown',
      'Home', 'End'
    ];

    if (allowedControlKeys.includes(event.key) || event.ctrlKey || event.metaKey) {
      return;
    }

    if (/^[0-9]$/.test(event.key)) {
      const input = event.target as HTMLInputElement;
      const selectionLength = (input.selectionEnd ?? 0) - (input.selectionStart ?? 0);
      if (input.value.length >= 10 && selectionLength === 0) {
        event.preventDefault();
      }
      return;
    }

    event.preventDefault();
  }

  onPhoneInput(event: Event) {
    const input = event.target as HTMLInputElement;
    const val = input.value.replace(/\D/g, '').slice(0, 10);
    this.form.phone = val;
    input.value = val;
  }

  submitRegistration() {
    this.phoneTouched = true;
    if (!this.form.name || !this.isPhoneValid() || !this.form.dob || !this.form.gender || !this.form.primaryChiefComplaint.trim()) {
      return;
    }

    this.api.quickIntake({
      name: this.form.name.trim(),
      phone: this.form.phone.trim(),
      dob: this.form.dob,
      gender: this.form.gender,
      primaryChiefComplaint: this.form.primaryChiefComplaint.trim(),
      consentType: this.form.consentAccepted ? 'TreatmentAndReminders' : undefined
    }).subscribe(res => {
      if (res.warning?.hasPotentialDuplicate) {
        this.duplicateWarning.set(res.warning);
      } else {
        this.closeRegisterModal();
        this.loadPatients();
      }
    });
  }

  confirmErasePii(p: Patient) {
    this.erasingPatient = p;
  }

  executeErasePii() {
    if (!this.erasingPatient) return;
    this.api.erasePatientPii(this.erasingPatient.id).subscribe(() => {
      this.erasingPatient = null;
      this.loadPatients();
    });
  }

  goToCaseDossier(patientId: string) {
    this.router.navigate(['/patients', patientId, 'case-record']);
  }

  getMissingTooltip(p: Patient): string {
    const missing = p.missingRequiredFields || [];
    if (missing.length === 0) return 'Dossier complete';
    return 'Missing required clinical intake fields: ' + missing.join(', ');
  }

  private resetForm() {
    this.form = {
      name: '',
      phone: '',
      dob: '',
      gender: 'Male',
      primaryChiefComplaint: '',
      consentAccepted: true
    };
    this.phoneTouched = false;
    this.duplicateWarning.set(null);
  }
}
