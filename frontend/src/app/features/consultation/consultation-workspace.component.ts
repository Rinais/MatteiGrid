import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../core/api.service';
import { AuthService } from '../../core/auth.service';
import { MateriaMedicaEntry, Patient, PrescriptionItem } from '../../core/models';

@Component({
  selector: 'app-consultation-workspace',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="workspace-container">
      <!-- Left Panel: Patient History & Clinical Notes -->
      <aside class="panel left-panel">
        <div class="panel-header">
          <h2>Patient File</h2>
          <span class="patient-id-badge">#p-1001</span>
        </div>

        <div class="patient-card">
          <div class="patient-name">Ramesh Kumar</div>
          <div class="patient-meta">42 Yrs • Male • +919845012345</div>
          <div class="patient-history-summary">
            <strong>Past History:</strong> Chronic lymphatic congestion, seasonal bronchial irritation.
          </div>
        </div>

        <div class="clinical-notes-section">
          <h3>Consultation Notes</h3>
          <textarea
            [(ngModel)]="clinicalNotes"
            rows="6"
            class="notes-textarea"
            placeholder="Record symptoms, pulse diagnosis observations, and clinical findings..."
          ></textarea>
        </div>

        <div class="past-prescriptions">
          <h3>Recent Prescriptions</h3>
          <div class="past-rx-card">
            <div class="rx-date">12 Aug 2026 • v1 (Completed)</div>
            <div class="rx-summary">Scrofoloso 1 (30C) + Red Electricity</div>
          </div>
        </div>
      </aside>

      <!-- Center Panel: Grid-Based Prescription Builder -->
      <main class="panel center-panel">
        <div class="panel-header">
          <h2>Electro-Homoeopathy Remedy Grid</h2>
          <div class="rx-version-tag">Draft Version 1.0</div>
        </div>

        <!-- 1. Remedy Selection Grid -->
        <div class="grid-section">
          <div class="section-instruction">Step 1: Select Electro-Homoeopathy Remedy</div>
          <div class="remedy-grid">
            <button
              *ngFor="let remedy of availableRemedies"
              class="remedy-btn"
              [class.selected]="selectedRemedy?.code === remedy.code"
              (click)="selectRemedy(remedy)"
            >
              <span class="remedy-code">{{ remedy.code }}</span>
              <span class="remedy-name">{{ remedy.name }}</span>
            </button>
          </div>
        </div>

        <!-- 2. Electricities / Polarity Picker (WCAG 2.1 AA Accessible) -->
        <div class="polarity-section">
          <div class="section-instruction">
            Step 2: Choose Electricity Polarity
            <small class="wcag-note">(Equipped with symbols & text badges for non-color accessibility)</small>
          </div>
          <div class="polarity-picker" role="radiogroup" aria-label="Electricity Polarity">
            <button
              *ngFor="let p of polarityOptions"
              class="polarity-btn"
              [ngClass]="p.cssClass"
              [class.active]="selectedPolarity === p.name"
              (click)="selectedPolarity = p.name"
              role="radio"
              [attr.aria-checked]="selectedPolarity === p.name"
            >
              <span class="polarity-symbol" aria-hidden="true">{{ p.symbol }}</span>
              <span class="polarity-text">{{ p.name }} Electricity</span>
              <span class="polarity-desc">{{ p.desc }}</span>
            </button>
          </div>
        </div>

        <!-- 3. Dilution & Vehicle Configuration -->
        <div class="step-controls-grid">
          <div class="control-group">
            <label>Step 3: Dilution</label>
            <select [(ngModel)]="selectedDilution" class="control-select">
              <option value="1X">1X (Decimal Lowest)</option>
              <option value="3X">3X (Decimal Active)</option>
              <option value="6X">6X (Decimal Middle)</option>
              <option value="30C">30C (Centesimal Standard)</option>
              <option value="200C">200C (High Dynamic)</option>
            </select>
          </div>

          <div class="control-group">
            <label>Step 4: Vehicle</label>
            <select [(ngModel)]="selectedVehicle" class="control-select">
              <option value="Distilled Water">Distilled Water</option>
              <option value="Sugar Globules">Sugar Globules</option>
              <option value="Alcoholic Drops">Alcoholic Drops</option>
              <option value="Ointment / Salve">Ointment / Salve</option>
            </select>
          </div>

          <div class="control-group">
            <label>Dosage</label>
            <input type="text" [(ngModel)]="dosage" class="control-input" placeholder="e.g. 5 drops 3x daily" />
          </div>

          <div class="control-group">
            <label>Duration</label>
            <input type="text" [(ngModel)]="duration" class="control-input" placeholder="e.g. 14 days" />
          </div>
        </div>

        <button class="btn btn-add" (click)="addItemToPrescription()" [disabled]="!selectedRemedy">
          + Add to Prescription
        </button>

        <!-- Current Prescription Items List -->
        <div class="active-prescription-list">
          <h3>Active Prescription Items ({{ currentItems.length }})</h3>
          <div *ngIf="currentItems.length === 0" class="empty-rx-notice">
            No items added yet. Choose a remedy above and click "+ Add to Prescription".
          </div>

          <div *ngFor="let item of currentItems; let i = index" class="rx-item-card">
            <div class="rx-item-header">
              <span class="rx-item-remedy">{{ item.medicineName }} ({{ item.dilutionLabel }})</span>
              <span class="polarity-tag" [ngClass]="item.electricityPolarity.toLowerCase()">
                {{ item.electricityPolarity }} Electricity
              </span>
            </div>
            <div class="rx-item-details">
              Vehicle: {{ item.vehicleName }} • Dosage: {{ item.dosage }} • Duration: {{ item.duration }}
            </div>
            <button class="rx-item-remove" (click)="removeItem(i)">Remove</button>
          </div>
        </div>

        <!-- Action Bar: Finalize & Send to Lab -->
        <div class="finalize-bar">
          <div class="final-summary">
            <strong>Ready to send to Lab?</strong> This creates the preparation ticket & issues the UPI billing link.
          </div>
          <button class="btn btn-finalize" [disabled]="currentItems.length === 0" (click)="finalizePrescription()">
            ✓ Finalize Prescription & Dispatch to Lab
          </button>
        </div>

        <!-- Post Finalization Success Banner -->
        <div *ngIf="finalizedResult" class="success-banner">
          <h4>🎉 Prescription Finalized Successfully!</h4>
          <p><strong>Laboratory Order #{{ finalizedResult.labOrder.id.slice(0, 8) }}</strong> has been queued in real-time.</p>
          <p><strong>Invoice #{{ finalizedResult.invoice.id.slice(0, 8) }}</strong> generated for ₹{{ finalizedResult.invoice.totalAmount }}.</p>
          <p class="upi-link-notice">
            📲 Razorpay UPI Payment Link SMS dispatched to patient (Valid for 48 hours).
          </p>
        </div>
      </main>

      <!-- Right Panel: Materia Medica Knowledge Drawer -->
      <aside class="panel right-panel">
        <div class="panel-header">
          <h2>Digital Materia Medica</h2>
        </div>

        <div class="mm-search">
          <input
            type="text"
            placeholder="Search indications, codes, families..."
            [(ngModel)]="mmSearchQuery"
            (ngModelChange)="searchMateriaMedica()"
            class="search-input"
          />
        </div>

        <div class="mm-list">
          <div
            *ngFor="let entry of materiaMedicaEntries()"
            class="mm-entry-card"
            [class.active]="selectedRemedy?.code === entry.code"
            (click)="selectFromMateriaMedica(entry)"
          >
            <div class="mm-entry-header">
              <span class="mm-code">{{ entry.code }}</span>
              <span class="mm-name">{{ entry.name }}</span>
            </div>
            <div class="mm-family">{{ entry.family }}</div>
            <div class="mm-indications">{{ entry.indications }}</div>
          </div>
        </div>
      </aside>
    </div>
  `,
  styles: [`
    .workspace-container {
      display: grid;
      grid-template-columns: 300px 1fr 340px;
      height: calc(100vh - 105px);
      background: #f3f4f6;
    }
    .panel {
      background: white;
      border-right: 1px solid #e5e7eb;
      display: flex;
      flex-direction: column;
      overflow-y: auto;
    }
    .panel-header {
      padding: 1rem 1.25rem;
      border-bottom: 1px solid #e5e7eb;
      display: flex;
      justify-content: space-between;
      align-items: center;
      background: #fafafa;
    }
    .panel-header h2 {
      margin: 0;
      font-size: 1.1rem;
      font-weight: 700;
      color: #111827;
    }
    .patient-id-badge {
      font-size: 0.75rem;
      background: #e0e7ff;
      color: #3730a3;
      padding: 0.2rem 0.5rem;
      border-radius: 4px;
    }
    .patient-card {
      padding: 1.25rem;
      border-bottom: 1px solid #e5e7eb;
    }
    .patient-name { font-size: 1.1rem; font-weight: 600; color: #111827; }
    .patient-meta { font-size: 0.8rem; color: #6b7280; margin: 0.2rem 0 0.5rem; }
    .patient-history-summary { font-size: 0.85rem; color: #374151; }
    .clinical-notes-section { padding: 1.25rem; border-bottom: 1px solid #e5e7eb; }
    .clinical-notes-section h3, .past-prescriptions h3 { font-size: 0.9rem; font-weight: 600; margin: 0 0 0.5rem; color: #374151; }
    .notes-textarea {
      width: 100%;
      box-sizing: border-box;
      border: 1px solid #d1d5db;
      border-radius: 6px;
      padding: 0.75rem;
      font-size: 0.85rem;
      font-family: inherit;
      resize: vertical;
    }
    .past-prescriptions { padding: 1.25rem; }
    .past-rx-card {
      background: #f9fafb;
      border: 1px solid #e5e7eb;
      border-radius: 6px;
      padding: 0.75rem;
      font-size: 0.8rem;
    }
    .rx-date { color: #6b7280; margin-bottom: 0.25rem; }
    .rx-summary { font-weight: 500; color: #111827; }
    .center-panel {
      padding: 1.25rem 2rem;
      overflow-y: auto;
    }
    .rx-version-tag {
      background: #ecfdf5;
      color: #065f46;
      border: 1px solid #a7f3d0;
      font-size: 0.75rem;
      padding: 0.2rem 0.5rem;
      border-radius: 4px;
      font-weight: 600;
    }
    .section-instruction {
      font-size: 0.9rem;
      font-weight: 600;
      color: #374151;
      margin: 1rem 0 0.5rem;
      display: flex;
      align-items: center;
      gap: 0.5rem;
    }
    .wcag-note { font-size: 0.75rem; color: #059669; font-weight: normal; }
    .remedy-grid {
      display: grid;
      grid-template-columns: repeat(6, 1fr);
      gap: 0.5rem;
      margin-bottom: 1rem;
    }
    .remedy-btn {
      background: #f9fafb;
      border: 1px solid #d1d5db;
      border-radius: 6px;
      padding: 0.6rem 0.4rem;
      text-align: center;
      cursor: pointer;
      display: flex;
      flex-direction: column;
      align-items: center;
      transition: all 0.15s;
    }
    .remedy-btn:hover { background: #e5e7eb; }
    .remedy-btn.selected {
      background: #065f46;
      border-color: #047857;
      color: white;
    }
    .remedy-code { font-weight: 700; font-size: 0.95rem; }
    .remedy-name { font-size: 0.7rem; margin-top: 0.2rem; }
    .polarity-picker {
      display: grid;
      grid-template-columns: repeat(5, 1fr);
      gap: 0.5rem;
      margin-bottom: 1.25rem;
    }
    .polarity-btn {
      border: 2px solid #d1d5db;
      border-radius: 6px;
      padding: 0.6rem 0.4rem;
      cursor: pointer;
      display: flex;
      flex-direction: column;
      align-items: center;
      background: white;
      transition: all 0.15s;
    }
    .polarity-symbol { font-size: 1.25rem; font-weight: bold; }
    .polarity-text { font-size: 0.8rem; font-weight: 600; margin-top: 0.2rem; }
    .polarity-desc { font-size: 0.65rem; color: #6b7280; }
    .polarity-btn.red.active { border-color: #ef4444; background: #fef2f2; color: #991b1b; }
    .polarity-btn.blue.active { border-color: #3b82f6; background: #eff6ff; color: #1e40af; }
    .polarity-btn.green.active { border-color: #10b981; background: #ecfdf5; color: #065f46; }
    .polarity-btn.yellow.active { border-color: #f59e0b; background: #fffbeb; color: #92400e; }
    .polarity-btn.white.active { border-color: #6b7280; background: #f3f4f6; color: #1f2937; }
    .step-controls-grid {
      display: grid;
      grid-template-columns: repeat(4, 1fr);
      gap: 0.75rem;
      margin-bottom: 1rem;
    }
    .control-group label { display: block; font-size: 0.8rem; font-weight: 500; margin-bottom: 0.3rem; color: #4b5563; }
    .control-select, .control-input {
      width: 100%;
      padding: 0.5rem;
      border: 1px solid #d1d5db;
      border-radius: 4px;
      font-size: 0.85rem;
      box-sizing: border-box;
    }
    .btn-add {
      background: #10b981;
      color: white;
      border: none;
      padding: 0.6rem 1.25rem;
      border-radius: 6px;
      font-weight: 600;
      cursor: pointer;
      margin-bottom: 1.5rem;
    }
    .btn-add:disabled { opacity: 0.5; cursor: not-allowed; }
    .active-prescription-list {
      border-top: 1px solid #e5e7eb;
      padding-top: 1rem;
      margin-bottom: 1.5rem;
    }
    .empty-rx-notice {
      padding: 1.5rem;
      text-align: center;
      background: #fafafa;
      border: 1px dashed #d1d5db;
      border-radius: 6px;
      color: #6b7280;
      font-size: 0.85rem;
    }
    .rx-item-card {
      border: 1px solid #e5e7eb;
      border-radius: 6px;
      padding: 0.75rem 1rem;
      margin-bottom: 0.5rem;
      background: #fafafa;
      display: flex;
      justify-content: space-between;
      align-items: center;
    }
    .rx-item-remedy { font-weight: 600; font-size: 0.95rem; margin-right: 0.5rem; }
    .polarity-tag {
      font-size: 0.7rem;
      padding: 0.15rem 0.4rem;
      border-radius: 4px;
      font-weight: 600;
    }
    .polarity-tag.red { background: #fee2e2; color: #b91c1c; }
    .polarity-tag.blue { background: #dbeafe; color: #1d4ed8; }
    .polarity-tag.green { background: #d1fae5; color: #047857; }
    .polarity-tag.yellow { background: #fef3c7; color: #b45309; }
    .polarity-tag.white { background: #f3f4f6; color: #374151; }
    .rx-item-details { font-size: 0.8rem; color: #4b5563; }
    .rx-item-remove {
      background: none;
      border: none;
      color: #ef4444;
      font-size: 0.8rem;
      cursor: pointer;
    }
    .finalize-bar {
      background: #f8fafc;
      border: 1px solid #cbd5e1;
      border-radius: 8px;
      padding: 1rem 1.5rem;
      display: flex;
      justify-content: space-between;
      align-items: center;
    }
    .btn-finalize {
      background: #059669;
      color: white;
      border: none;
      padding: 0.75rem 1.5rem;
      border-radius: 6px;
      font-weight: 700;
      cursor: pointer;
      font-size: 0.95rem;
    }
    .btn-finalize:disabled { opacity: 0.5; cursor: not-allowed; }
    .success-banner {
      margin-top: 1.5rem;
      background: #ecfdf5;
      border: 1px solid #10b981;
      border-radius: 8px;
      padding: 1.25rem;
      color: #065f46;
    }
    .success-banner h4 { margin: 0 0 0.5rem; }
    .upi-link-notice { font-size: 0.85rem; color: #047857; margin-top: 0.5rem; }
    .right-panel { border-right: none; border-left: 1px solid #e5e7eb; }
    .mm-search { padding: 0.75rem 1rem; border-bottom: 1px solid #e5e7eb; }
    .search-input { width: 100%; box-sizing: border-box; padding: 0.5rem; border: 1px solid #d1d5db; border-radius: 4px; font-size: 0.85rem; }
    .mm-list { padding: 0.75rem; overflow-y: auto; }
    .mm-entry-card {
      border: 1px solid #e5e7eb;
      border-radius: 6px;
      padding: 0.75rem;
      margin-bottom: 0.5rem;
      cursor: pointer;
      transition: all 0.15s;
    }
    .mm-entry-card:hover { border-color: #10b981; background: #f0fdf4; }
    .mm-entry-card.active { border-color: #059669; background: #ecfdf5; }
    .mm-entry-header { display: flex; align-items: center; gap: 0.5rem; }
    .mm-code { font-weight: bold; color: #059669; }
    .mm-name { font-weight: 600; font-size: 0.85rem; }
    .mm-family { font-size: 0.75rem; color: #6b7280; }
    .mm-indications { font-size: 0.75rem; color: #4b5563; margin-top: 0.3rem; }
  `]
})
export class ConsultationWorkspaceComponent implements OnInit {
  api = inject(ApiService);
  auth = inject(AuthService);

  clinicalNotes = '';
  selectedRemedy: any = null;
  selectedPolarity = 'Red';
  selectedDilution = '30C';
  selectedVehicle = 'Sugar Globules';
  dosage = '5 globules 3x daily';
  duration = '14 days';

  currentItems: PrescriptionItem[] = [];
  finalizedResult: any = null;

  mmSearchQuery = '';
  materiaMedicaEntries = signal<MateriaMedicaEntry[]>([]);

  availableRemedies = [
    { code: 'S1', name: 'Scrofoloso 1' },
    { code: 'S2', name: 'Scrofoloso 2' },
    { code: 'A1', name: 'Angiotico 1' },
    { code: 'A2', name: 'Angiotico 2' },
    { code: 'C1', name: 'Canceroso 1' },
    { code: 'C2', name: 'Canceroso 2' },
    { code: 'F1', name: 'Febbrifugo 1' },
    { code: 'P1', name: 'Pettorale 1' },
    { code: 'L1', name: 'Linfatico 1' },
    { code: 'Ven1', name: 'Venereo 1' },
    { code: 'Ver1', name: 'Vermifugo 1' },
    { code: 'Sy', name: 'Sinassio' }
  ];

  polarityOptions = [
    { name: 'Red', symbol: '(+)', desc: 'Stimulating / Dynamic', cssClass: 'red' },
    { name: 'Blue', symbol: '(-)', desc: 'Sedating / Calming', cssClass: 'blue' },
    { name: 'Green', symbol: '(~)', desc: 'Neutral / Balancer', cssClass: 'green' },
    { name: 'Yellow', symbol: '(⚡)', desc: 'Vital / Thermal', cssClass: 'yellow' },
    { name: 'White', symbol: '(⚪)', desc: 'Universal / Unpolarized', cssClass: 'white' }
  ];

  ngOnInit() {
    this.searchMateriaMedica();
  }

  selectRemedy(remedy: any) {
    this.selectedRemedy = remedy;
  }

  selectFromMateriaMedica(entry: MateriaMedicaEntry) {
    this.selectedRemedy = { code: entry.code, name: entry.name };
  }

  searchMateriaMedica() {
    this.api.getMateriaMedica(this.mmSearchQuery).subscribe(entries => {
      this.materiaMedicaEntries.set(entries);
    });
  }

  addItemToPrescription() {
    if (!this.selectedRemedy) return;

    this.currentItems.push({
      id: `item-${Date.now()}`,
      medicineId: `med-${this.selectedRemedy.code}`,
      medicineName: this.selectedRemedy.name,
      dilutionId: `dil-${this.selectedDilution}`,
      dilutionLabel: this.selectedDilution,
      vehicleId: `veh-${this.selectedVehicle}`,
      vehicleName: this.selectedVehicle,
      electricityPolarity: this.selectedPolarity,
      quantity: 1,
      dosage: this.dosage,
      duration: this.duration
    });
  }

  removeItem(index: number) {
    this.currentItems.splice(index, 1);
  }

  finalizePrescription() {
    if (this.currentItems.length === 0) return;

    const payload = {
      patientId: 'p1000000-0000-0000-0000-000000000001',
      items: this.currentItems,
      clinicalNotes: this.clinicalNotes
    };

    this.api.finalizePrescription(payload).subscribe(res => {
      this.finalizedResult = res;
    });
  }
}
