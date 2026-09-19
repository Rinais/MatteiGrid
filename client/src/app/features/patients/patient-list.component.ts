import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
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
          <p class="page-subtitle">Centralized medical records and DPDP-compliant consent tracking</p>
        </div>
        <button class="btn btn-primary" (click)="showRegisterModal = true">+ Register New Patient</button>
      </div>

      <!-- Search Bar -->
      <div class="search-bar">
        <input
          type="text"
          placeholder="Search by patient name or phone number..."
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
              <th>Patient Name</th>
              <th>Phone</th>
              <th>Date of Birth</th>
              <th>Registered</th>
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
              </td>
              <td>{{ p.phone }}</td>
              <td>{{ p.dob }}</td>
              <td>{{ p.createdAt | date:'mediumDate' }}</td>
              <td>
                <span class="status-pill active">Consent Captured</span>
              </td>
              <td>
                <button
                  class="btn-action danger"
                  (click)="confirmErasePii(p)"
                  [disabled]="p.isAnonymized"
                  title="DPDP Act 2023 Right to Erasure"
                >
                  {{ p.isAnonymized ? 'Anonymized' : 'Anonymize PII' }}
                </button>
              </td>
            </tr>
            <tr *ngIf="patients().length === 0">
              <td colspan="6" class="text-center text-muted">No patients found.</td>
            </tr>
          </tbody>
        </table>
      </div>

      <!-- Registration Modal -->
      <div class="modal-backdrop" *ngIf="showRegisterModal">
        <div class="modal-dialog">
          <div class="modal-header">
            <h3>Register New Patient</h3>
            <button class="modal-close" (click)="showRegisterModal = false">×</button>
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
              <input type="text" [(ngModel)]="form.phone" class="form-control" placeholder="e.g. +919876543210" />
            </div>

            <div class="form-group">
              <label>Date of Birth *</label>
              <input type="date" [(ngModel)]="form.dob" class="form-control" />
            </div>

            <div class="form-group checkbox-group">
              <label>
                <input type="checkbox" [(ngModel)]="form.consentAccepted" />
                Explicit DPDP Act 2023 consent captured for medical treatment and reminders.
              </label>
            </div>
          </div>
          <div class="modal-footer">
            <button class="btn btn-outline" (click)="showRegisterModal = false">Cancel</button>
            <button class="btn btn-primary" (click)="submitRegistration()" [disabled]="!form.name || !form.phone || !form.dob">
              Confirm Registration
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
              Per Section 7 & 13 of PRD, this will irreversibly anonymize identifiable personal data (Name, Phone, DOB).
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
    .patients-container { padding: 1.5rem 2rem; max-width: 1200px; margin: 0 auto; }
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
    td { padding: 0.85rem 1rem; border-bottom: 1px solid #f3f4f6; }
    .patient-name { font-weight: 600; color: #111827; display: flex; align-items: center; gap: 0.5rem; }
    .patient-name.anonymized { color: #6b7280; font-style: italic; }
    .badge-anonymized {
      font-size: 0.7rem;
      background: #f3f4f6;
      color: #4b5563;
      padding: 0.1rem 0.4rem;
      border-radius: 4px;
      border: 1px solid #d1d5db;
    }
    .status-pill {
      font-size: 0.75rem;
      padding: 0.2rem 0.5rem;
      border-radius: 9999px;
      background: #d1fae5;
      color: #065f46;
    }
    .btn { padding: 0.5rem 1rem; border-radius: 6px; font-weight: 500; font-size: 0.875rem; cursor: pointer; border: 1px solid transparent; }
    .btn-primary { background: #10b981; color: white; }
    .btn-outline { background: white; border-color: #d1d5db; color: #374151; }
    .btn-danger { background: #ef4444; color: white; }
    .btn-action {
      background: none;
      border: 1px solid #e5e7eb;
      padding: 0.25rem 0.5rem;
      border-radius: 4px;
      font-size: 0.75rem;
      cursor: pointer;
    }
    .btn-action.danger { color: #dc2626; border-color: #fecaca; }
    .btn-action:disabled { opacity: 0.5; cursor: not-allowed; }
    .modal-backdrop {
      position: fixed; top: 0; left: 0; right: 0; bottom: 0;
      background: rgba(0,0,0,0.5);
      display: flex; align-items: center; justify-content: center;
      z-index: 1000;
    }
    .modal-dialog {
      background: white; border-radius: 8px; width: 500px; max-width: 90%;
      box-shadow: 0 20px 25px -5px rgba(0,0,0,0.1);
    }
    .modal-header { padding: 1rem 1.25rem; border-bottom: 1px solid #e5e7eb; display: flex; justify-content: space-between; align-items: center; }
    .modal-header h3 { margin: 0; font-size: 1.15rem; }
    .modal-header.danger h3 { color: #dc2626; }
    .modal-close { background: none; border: none; font-size: 1.5rem; cursor: pointer; color: #9ca3af; }
    .modal-body { padding: 1.25rem; }
    .modal-footer { padding: 0.75rem 1.25rem; border-top: 1px solid #e5e7eb; display: flex; justify-content: flex-end; gap: 0.75rem; }
    .form-group { margin-bottom: 1rem; }
    .form-group label { display: block; font-size: 0.85rem; font-weight: 500; margin-bottom: 0.35rem; }
    .form-control { width: 100%; padding: 0.5rem 0.75rem; border: 1px solid #d1d5db; border-radius: 4px; box-sizing: border-box; }
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

  patients = signal<Patient[]>([]);
  searchQuery = '';
  showRegisterModal = false;
  erasingPatient: Patient | null = null;
  duplicateWarning = signal<DuplicatePatientWarning | null>(null);

  form = {
    name: '',
    phone: '',
    dob: '',
    consentAccepted: true
  };

  ngOnInit() {
    this.loadPatients();
  }

  loadPatients() {
    this.api.getPatients(this.searchQuery).subscribe(p => this.patients.set(p));
  }

  onSearch() {
    this.loadPatients();
  }

  submitRegistration() {
    this.api.registerPatient({
      name: this.form.name,
      phone: this.form.phone,
      dob: this.form.dob,
      consentType: this.form.consentAccepted ? 'TreatmentAndReminders' : undefined
    }).subscribe(res => {
      if (res.warning.hasPotentialDuplicate) {
        this.duplicateWarning.set(res.warning);
      } else {
        this.showRegisterModal = false;
        this.resetForm();
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

  private resetForm() {
    this.form = { name: '', phone: '', dob: '', consentAccepted: true };
    this.duplicateWarning.set(null);
  }
}
