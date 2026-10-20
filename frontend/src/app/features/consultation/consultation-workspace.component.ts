import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { ApiService } from '../../core/api.service';
import { AuthService } from '../../core/auth.service';
import {
  Consultation,
  ConsultationObservation,
  HomeopathicCaseRecord,
  MateriaMedicaEntry,
  Patient,
  PatientDetailedProfile,
  PrescriptionItem
} from '../../core/models';

@Component({
  selector: 'app-consultation-workspace',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="workspace-container">
      <!-- ========================================== -->
      <!-- Left Panel: Organon Case Dossier & Clinical -->
      <!-- ========================================== -->
      <aside class="panel left-panel">
        <div class="panel-header">
          <div class="header-title-row">
            <h2>Organon Case Dossier</h2>
            <span class="organon-tag">§275–282 / Page 6</span>
          </div>

          <!-- Patient Context Selector -->
          <div class="patient-selector-box">
            <label for="patientSelect" class="selector-label">Active Patient:</label>
            <select
              id="patientSelect"
              class="patient-select-input"
              [ngModel]="activePatient()?.id"
              (ngModelChange)="onPatientChange($event)"
            >
              <option *ngFor="let p of allPatients()" [value]="p.id">
                {{ p.name }} ({{ p.phone }})
              </option>
            </select>
          </div>
        </div>

        <div class="left-scroll-content">
          <!-- Patient Summary Card -->
          <div class="patient-card" *ngIf="activePatient() as p">
            <div class="patient-card-header">
              <span class="patient-name">{{ p.name }}</span>
              <span class="patient-id-badge">#{{ p.id.slice(0, 8) }}</span>
            </div>
            <div class="patient-meta">
              {{ p.gender || 'Not specified' }} • DOB: {{ p.dob || 'N/A' }} • {{ p.phone }}
            </div>
            <div class="patient-complaint">
              <strong>Chief Complaint:</strong> {{ p.primaryChiefComplaint || 'Seasonal bronchial irritation with lymphatic sluggishness.' }}
            </div>
          </div>

          <!-- Persistent Non-Blocking Clinical Dossier Warning Banner -->
          <div
            class="case-warning-banner"
            *ngIf="activePatient() && activePatient()?.completionPercentage !== 100"
          >
            <div class="warning-header-row">
              <span class="warning-pill">⚠️ Incomplete Case Record ({{ activePatient()?.completionPercentage || 40 }}%)</span>
            </div>
            <p class="warning-text">
              Missing required intake fields:
              <strong>{{ (activePatient()?.missingRequiredFields || []).join(', ') || 'Thermals, Functional, Vitals' }}</strong>
            </p>
            <button class="btn-dossier-action" (click)="openCaseDossier(activePatient()!.id)">
              📋 Complete Case Dossier →
            </button>
          </div>

          <!-- Completed Dossier Badge -->
          <div
            class="case-complete-banner"
            *ngIf="activePatient() && activePatient()?.completionPercentage === 100"
          >
            <div class="complete-header-row">
              <span class="complete-pill">✅ Case Dossier Complete (100%)</span>
            </div>
            <button class="btn-dossier-view" (click)="openCaseDossier(activePatient()!.id)">
              View Detailed Dossier
            </button>
          </div>

          <!-- Organon Case Totality Snapshot Accordion -->
          <div class="totality-section">
            <div class="totality-toggle-header" (click)="isTotalityExpanded.set(!isTotalityExpanded())">
              <span class="totality-title">
                📜 Case Totality Reference (Pages 3–5)
              </span>
              <span class="toggle-icon">{{ isTotalityExpanded() ? '▲' : '▼' }}</span>
            </div>

            <div class="totality-body" *ngIf="isTotalityExpanded()">
              <div class="totality-group" *ngIf="caseRecord() as cr">
                <div class="totality-item">
                  <strong>Location & Sensation:</strong>
                  <span>{{ cr.locationSensation || cr.sensationAndPathology || 'Temporal pressure, throat constriction' }}</span>
                </div>
                <div class="totality-item">
                  <strong>Modalities:</strong>
                  <div><span class="mod-agg">&lt; Aggravation:</span> {{ cr.aggravatingModalities || 'Cold drafts, exertion, noise' }}</div>
                  <div><span class="mod-amel">&gt; Amelioration:</span> {{ cr.amelioratingModalities || 'Warm compresses, rest in dark room' }}</div>
                </div>
                <div class="totality-item">
                  <strong>Thermals:</strong>
                  <span>{{ cr.thermals || cr.thermalTolerance || 'Chilly, highly sensitive to drafts' }}</span>
                </div>
                <div class="totality-item">
                  <strong>Functional Generals:</strong>
                  <div>Appetite: {{ cr.appetite || 'Moderate' }} • Thirst: {{ cr.thirst || 'Small sips warm water' }}</div>
                  <div>Sleep: {{ cr.sleep || 'Disturbed before 2 AM' }} • Dreams: {{ cr.dreams || 'Anxious dreams' }}</div>
                  <div>Elimination: Stool ({{ cr.stool || 'Normal' }}), Urine ({{ cr.urine || 'Clear' }}), Sweat ({{ cr.sweat || 'Scanty' }})</div>
                </div>
                <div class="totality-item">
                  <strong>Vitals & Physical Exam:</strong>
                  <div>BP: {{ cr.bloodPressure || '120/80' }} • Pulse: {{ cr.pulseRate || '74 bpm' }} • Temp: {{ cr.temperatureF ? cr.temperatureF + '°F' : '98.4°F' }}</div>
                  <div>Weight: {{ cr.weightKg ? cr.weightKg + ' kg' : '62 kg' }} • Height: {{ cr.heightCm ? cr.heightCm + ' cm' : '168 cm' }}</div>
                </div>
              </div>
            </div>
          </div>

          <!-- Organon Case Metadata Form (Pages 2 & 6) -->
          <div class="case-meta-section">
            <h3>Organon Clinical Evaluation</h3>
            <div class="form-row">
              <label>Case Type:</label>
              <select [(ngModel)]="caseType" class="meta-select">
                <option value="Acute">Acute</option>
                <option value="Chronic">Chronic</option>
                <option value="Gynaecological">Gynaecological</option>
                <option value="Surgical">Surgical</option>
                <option value="Epidemic">Epidemic</option>
                <option value="Others">Others</option>
              </select>
            </div>

            <div class="form-group-spaced">
              <label>Provisional Diagnosis:</label>
              <input
                type="text"
                [(ngModel)]="provisionalDiagnosis"
                class="meta-input"
                placeholder="e.g. Chronic Lymphatic Bronchitis"
              />
            </div>

            <div class="form-group-spaced">
              <label>Final Diagnosis:</label>
              <input
                type="text"
                [(ngModel)]="finalDiagnosis"
                class="meta-input"
                placeholder="e.g. Scrofulous Bronchial Diathesis"
              />
            </div>

            <div class="form-row">
              <label>Case Result / Outcome:</label>
              <select [(ngModel)]="outcome" class="meta-select">
                <option value="UnderTreatment">Under Treatment</option>
                <option value="Cured">Cured</option>
                <option value="Relieved">Relieved</option>
                <option value="Referred">Referred</option>
                <option value="Otherwise">Otherwise</option>
                <option value="Expired">Expired</option>
              </select>
            </div>

            <button class="btn-save-meta" (click)="saveClinicalDetails()">
              💾 Save Diagnosis & Outcome
            </button>
            <span *ngIf="metaSavedNotice()" class="saved-notice">✓ Saved</span>
          </div>

          <!-- Clinical Observations & Follow-Up Log (Organon Page 6) -->
          <div class="observations-section">
            <div class="obs-header-row">
              <h3>Follow-up Observations</h3>
              <button class="btn-add-obs" (click)="isAddingObservation.set(!isAddingObservation())">
                {{ isAddingObservation() ? 'Cancel' : '+ New Observation' }}
              </button>
            </div>

            <!-- New Observation Form -->
            <div class="obs-form-card" *ngIf="isAddingObservation()">
              <h4>Record Organon Follow-up Reaction</h4>

              <div class="form-group-spaced">
                <label>Reaction / Action of Medicine:</label>
                <select [(ngModel)]="newReactionType" class="meta-select">
                  <option value="InitialEncounter">Initial Encounter</option>
                  <option value="Amelioration">Amelioration (General & Particular)</option>
                  <option value="HomoeopathicAggravation">Homoeopathic Aggravation (Favorable reaction)</option>
                  <option value="DiseaseAggravation">Disease Aggravation (Unfavorable progress)</option>
                  <option value="NoChange">No Change (Inert / Potency too low)</option>
                  <option value="NewSymptoms">New Symptoms Appeared (Wrong remedy)</option>
                </select>
              </div>

              <div class="form-group-spaced">
                <label>Kent's 12 Observations Reference:</label>
                <select [(ngModel)]="newKentsObservation" class="meta-select">
                  <option value="None">-- Select Kent's Observation --</option>
                  <option value="Obs 1: Prolonged aggravation and then final decline">Obs 1: Prolonged aggravation and then final decline</option>
                  <option value="Obs 2: Long aggravation but final and slow improvement">Obs 2: Long aggravation but final and slow improvement</option>
                  <option value="Obs 3: Aggravation quick, short, strong with rapid improvement">Obs 3: Aggravation quick, short, strong with rapid improvement</option>
                  <option value="Obs 4: Recovery without any aggravation">Obs 4: Recovery without any aggravation</option>
                  <option value="Obs 5: Amelioration comes first, aggravation follows">Obs 5: Amelioration comes first, aggravation follows</option>
                  <option value="Obs 6: Too short relief of symptoms">Obs 6: Too short relief of symptoms</option>
                  <option value="Obs 7: Full amelioration of symptoms, but no relief of patient">Obs 7: Full amelioration of symptoms, but no relief of patient</option>
                  <option value="Obs 8: Patients prove every remedy they take">Obs 8: Patients prove every remedy they take</option>
                  <option value="Obs 9: Action of medicine upon provers">Obs 9: Action of medicine upon provers</option>
                  <option value="Obs 10: New symptoms appearing after remedy">Obs 10: New symptoms appearing after remedy</option>
                  <option value="Obs 11: Old symptoms reappear">Obs 11: Old symptoms reappear (Favorable)</option>
                  <option value="Obs 12: Symptoms take a wrong direction">Obs 12: Symptoms take a wrong direction</option>
                </select>
              </div>

              <div class="form-group-spaced">
                <label>Second Prescription Plan:</label>
                <select [(ngModel)]="newSecondPrescriptionAction" class="meta-select">
                  <option value="Continue">Continue Current Rx</option>
                  <option value="WaitAndWatch">Wait & Watch (Placebo / Sac Lac)</option>
                  <option value="ChangePotency">Change Potency (Higher / Lower Dilution)</option>
                  <option value="ChangeRemedy">Change Remedy (Complementary / Antidote)</option>
                </select>
              </div>

              <div class="form-group-spaced">
                <label>Observation & Pulse Findings:</label>
                <textarea
                  [(ngModel)]="newObservationNotes"
                  rows="3"
                  class="obs-textarea"
                  placeholder="Record patient response, physical changes, and clinical findings..."
                ></textarea>
              </div>

              <button class="btn-submit-obs" (click)="submitObservation()">
                ✓ Save Observation
              </button>
            </div>

            <!-- Observations Timeline -->
            <div class="obs-timeline">
              <div *ngIf="observations().length === 0" class="empty-obs">
                No follow-up observations recorded yet.
              </div>
              <div *ngFor="let obs of observations()" class="obs-card">
                <div class="obs-card-header">
                  <span class="obs-reaction-pill" [ngClass]="getReactionClass(obs.reactionType)">
                    {{ obs.reactionType }}
                  </span>
                  <span class="obs-date">{{ obs.observationDate | date:'dd MMM yyyy, HH:mm' }}</span>
                </div>
                <div *ngIf="obs.kentsObservation && obs.kentsObservation !== 'None'" class="obs-kent">
                  <strong>Kent:</strong> {{ obs.kentsObservation }}
                </div>
                <div *ngIf="obs.secondPrescriptionAction" class="obs-action">
                  <strong>Plan:</strong> {{ obs.secondPrescriptionAction }}
                </div>
                <div class="obs-notes">{{ obs.notes }}</div>
              </div>
            </div>
          </div>
        </div>
      </aside>

      <!-- ========================================== -->
      <!-- Center Panel: Electro-Homoeopathy Rx Grid  -->
      <!-- ========================================== -->
      <main class="panel center-panel">
        <div class="panel-header center-header">
          <div>
            <h2>Electro-Homoeopathy Remedy Grid</h2>
            <div class="sub-header-notes">Formulated by Count Cesare Mattei • Organon Page 6 Prescription (Rx)</div>
          </div>
          <div class="rx-version-tag">Draft Version 1.0</div>
        </div>

        <div class="center-scroll-content">
          <!-- Step 0: Method Selection (Organon Page 6 requirement) -->
          <div class="method-bar">
            <span class="method-label">Prescription Method (Organon Page 6):</span>
            <div class="method-pill-group">
              <button
                type="button"
                class="method-btn"
                [class.active]="selectedMethod === 'Repertorial'"
                (click)="selectedMethod = 'Repertorial'"
              >
                🎯 Repertorial (Totality-Based)
              </button>
              <button
                type="button"
                class="method-btn"
                [class.active]="selectedMethod === 'NonRepertorial'"
                (click)="selectedMethod = 'NonRepertorial'"
              >
                ⚡ Non-Repertorial (Organopathic / Specific)
              </button>
            </div>
          </div>

          <!-- Step 1: Remedy Family Category Tabs -->
          <div class="remedy-families-tab-bar">
            <span class="family-label">Remedy Family:</span>
            <button
              *ngFor="let fam of remedyFamilies"
              class="family-tab"
              [class.active]="activeFamilyFilter === fam.key"
              (click)="activeFamilyFilter = fam.key"
            >
              {{ fam.label }}
            </button>
          </div>

          <!-- Remedy Selection Grid -->
          <div class="grid-section">
            <div class="section-instruction">
              Step 1: Select Electro-Homoeopathy Remedy
              <span class="instruction-selection" *ngIf="selectedRemedy">
                Selected: <strong>{{ selectedRemedy.code }} - {{ selectedRemedy.name }}</strong>
              </span>
            </div>
            <div class="remedy-grid">
              <button
                *ngFor="let remedy of filteredRemedies"
                class="remedy-btn"
                [class.selected]="selectedRemedy?.code === remedy.code"
                (click)="selectRemedy(remedy)"
              >
                <span class="remedy-code">{{ remedy.code }}</span>
                <span class="remedy-name">{{ remedy.name }}</span>
                <span class="remedy-affinity">{{ remedy.affinity }}</span>
              </button>
            </div>
          </div>

          <!-- Step 2: Electricities / Polarity Picker (WCAG 2.1 AA Accessible) -->
          <div class="polarity-section">
            <div class="section-instruction">
              Step 2: Choose Electricity Polarity
              <small class="wcag-note">(Equipped with symbols & high-contrast text badges for non-color accessibility)</small>
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

          <!-- Step 3–7: Dilution, Vehicle, Dosage, Frequency & Duration -->
          <div class="step-controls-grid">
            <div class="control-group">
              <label>Step 3: Dilution / Potency</label>
              <select [(ngModel)]="selectedDilution" class="control-select">
                <option value="1X">1X (Decimal Lowest - Material)</option>
                <option value="2X">2X (Decimal Active)</option>
                <option value="3X">3X (Decimal Physiological)</option>
                <option value="6X">6X (Decimal Middle)</option>
                <option value="30C">30C (Centesimal Standard Systemic)</option>
                <option value="200C">200C (High Dynamic Constitutional)</option>
                <option value="1M">1M (Millensimal Deep-Acting)</option>
              </select>
            </div>

            <div class="control-group">
              <label>Step 4: Vehicle / Form</label>
              <select [(ngModel)]="selectedVehicle" class="control-select">
                <option value="Distilled Water">Distilled Water (Drops)</option>
                <option value="Sugar Globules">Sugar Globules (Cane Sugar)</option>
                <option value="Alcoholic Drops">Alcoholic Drops</option>
                <option value="Ointment / Salve">Ointment / Salve (Topical)</option>
                <option value="Compress / Lotion">Compress / Lotion (External)</option>
              </select>
            </div>

            <div class="control-group">
              <label>Step 5: Dosage / Quantity</label>
              <input type="text" [(ngModel)]="dosage" class="control-input" placeholder="e.g. 5 globules, 10 drops" />
            </div>

            <div class="control-group">
              <label>Step 6: Frequency</label>
              <select [(ngModel)]="frequency" class="control-select">
                <option value="OD">OD (Once daily)</option>
                <option value="BD">BD (Twice daily)</option>
                <option value="TDS">TDS (3 times daily)</option>
                <option value="QID">QID (4 times daily)</option>
                <option value="Hourly">Hourly (Acute crisis)</option>
                <option value="SOS">SOS (As needed)</option>
              </select>
            </div>
          </div>

          <div class="step-controls-grid-secondary">
            <div class="control-group">
              <label>Step 7: Duration</label>
              <input type="text" [(ngModel)]="duration" class="control-input" placeholder="e.g. 14 days" />
            </div>

            <div class="control-group span-2">
              <label>Step 8: Specific Directions for Remedy</label>
              <input
                type="text"
                [(ngModel)]="itemInstructions"
                class="control-input"
                placeholder="e.g. Dissolve in half glass pure lukewarm water"
              />
            </div>

            <div class="control-group action-align">
              <button
                class="btn btn-add"
                (click)="addItemToPrescription()"
                [disabled]="!selectedRemedy"
              >
                + Add Remedy to Rx
              </button>
            </div>
          </div>

          <!-- Step 9: Diet & Regimen Presets (Organon §259–§263) -->
          <div class="diet-regimen-section">
            <div class="diet-header">
              <label>Diet & Regimen Instructions (Organon §259–§263):</label>
              <div class="diet-chips">
                <button
                  type="button"
                  class="diet-chip"
                  *ngFor="let preset of dietPresets"
                  (click)="appendDietPreset(preset)"
                >
                  + {{ preset }}
                </button>
              </div>
            </div>
            <textarea
              [(ngModel)]="dietAndRegimenInstructions"
              rows="2"
              class="diet-textarea"
              placeholder="e.g. Avoid coffee, raw onion, camphor, and strong spices during remedy action. Take 30 mins before food."
            ></textarea>
          </div>

          <!-- Current Prescription Items List -->
          <div class="active-prescription-list">
            <div class="rx-list-header">
              <h3>Active Prescription Items ({{ currentItems.length }})</h3>
              <span class="rx-count-tag" *ngIf="currentItems.length > 0">{{ currentItems.length }} Remedies</span>
            </div>

            <div *ngIf="currentItems.length === 0" class="empty-rx-notice">
              No remedies added yet. Select a remedy above, configure dilution/vehicle, and click "+ Add Remedy to Rx".
            </div>

            <div *ngFor="let item of currentItems; let i = index" class="rx-item-card">
              <div class="rx-item-main">
                <div class="rx-item-header">
                  <span class="rx-item-remedy">{{ item.medicineName }} ({{ item.dilutionLabel }})</span>
                  <span class="rx-method-badge" [class.repertorial]="item.selectionMethod === 'Repertorial'">
                    {{ item.selectionMethod || 'Repertorial' }}
                  </span>
                  <span class="polarity-tag" [ngClass]="item.electricityPolarity.toLowerCase()">
                    {{ item.electricityPolarity }} Electricity
                  </span>
                </div>
                <div class="rx-item-details">
                  Vehicle: <strong>{{ item.vehicleName }}</strong> •
                  Dose: <strong>{{ item.dosage }}</strong> •
                  Frequency: <strong>{{ item.frequency || 'TDS' }}</strong> •
                  Duration: <strong>{{ item.duration }}</strong>
                </div>
                <div class="rx-item-instructions" *ngIf="item.instructions">
                  <em>Instructions: {{ item.instructions }}</em>
                </div>
              </div>
              <button class="rx-item-remove" (click)="removeItem(i)" title="Remove item">
                ✕ Remove
              </button>
            </div>
          </div>

          <!-- Action Bar: Finalize & Send to Lab -->
          <div class="finalize-bar">
            <div class="final-summary">
              <strong>Ready to finalize consultation & dispatch to Lab?</strong>
              <div>Creates the preparation ticket, deducts stock, and sends the UPI payment link.</div>
            </div>
            <button
              class="btn btn-finalize"
              [disabled]="currentItems.length === 0"
              (click)="finalizePrescription()"
            >
              ✓ Finalize Prescription & Dispatch to Lab
            </button>
          </div>

          <!-- Post Finalization Success Banner -->
          <div *ngIf="finalizedResult" class="success-banner">
            <h4>🎉 Prescription Finalized & Dispatched Successfully!</h4>
            <p><strong>Laboratory Ticket #{{ finalizedResult.labOrder?.id?.slice(0, 8) }}</strong> has been queued in real-time.</p>
            <p><strong>Invoice #{{ finalizedResult.invoice?.id?.slice(0, 8) }}</strong> generated for ₹{{ finalizedResult.invoice?.totalAmount || 600 }}.</p>
            <p class="upi-link-notice">
              📲 Razorpay UPI Payment Link dispatched to patient <strong>{{ activePatient()?.phone }}</strong> (Valid for 48 hours).
            </p>
          </div>
        </div>
      </main>

      <!-- ========================================== -->
      <!-- Right Panel: Materia Medica Drawer         -->
      <!-- ========================================== -->
      <aside class="panel right-panel">
        <div class="panel-header">
          <h2>Digital Materia Medica</h2>
          <div class="sub-header-notes">Keynotes & Organ Affinities</div>
        </div>

        <div class="mm-search">
          <input
            type="text"
            placeholder="Search remedies, codes, indications..."
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
              <span class="mm-family-badge">{{ entry.family }}</span>
            </div>
            <div class="mm-desc">{{ entry.description }}</div>
            <div class="mm-indications">
              <strong>Affinity & Indications:</strong> {{ entry.indications }}
            </div>
            <button class="btn-quick-apply" (click)="selectFromMateriaMedica(entry); $event.stopPropagation()">
              + Select for Rx Grid
            </button>
          </div>
        </div>
      </aside>
    </div>
  `,
  styles: [`
    .workspace-container {
      display: grid;
      grid-template-columns: 340px 1fr 340px;
      height: calc(100vh - 105px);
      background: #f1f5f9;
      font-family: inherit;
    }
    .panel {
      background: white;
      border-right: 1px solid #e2e8f0;
      display: flex;
      flex-direction: column;
      overflow: hidden;
    }
    .panel-header {
      padding: 0.85rem 1rem;
      border-bottom: 1px solid #e2e8f0;
      background: #fafafa;
    }
    .header-title-row {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 0.4rem;
    }
    .panel-header h2 {
      margin: 0;
      font-size: 1.05rem;
      font-weight: 700;
      color: #0f172a;
    }
    .organon-tag {
      background: #e0f2fe;
      color: #0369a1;
      font-size: 0.65rem;
      font-weight: 700;
      padding: 0.15rem 0.4rem;
      border-radius: 4px;
      letter-spacing: 0.02em;
    }
    .patient-selector-box {
      display: flex;
      flex-direction: column;
      gap: 0.2rem;
    }
    .selector-label {
      font-size: 0.7rem;
      font-weight: 600;
      color: #64748b;
      text-transform: uppercase;
      letter-spacing: 0.03em;
    }
    .patient-select-input {
      width: 100%;
      padding: 0.35rem 0.5rem;
      border: 1px solid #cbd5e1;
      border-radius: 6px;
      font-size: 0.85rem;
      font-weight: 600;
      color: #1e293b;
      background: white;
    }
    .left-scroll-content, .center-scroll-content {
      flex: 1;
      overflow-y: auto;
      padding: 1rem;
    }
    .patient-card {
      background: #f8fafc;
      border: 1px solid #e2e8f0;
      border-radius: 8px;
      padding: 0.75rem 0.85rem;
      margin-bottom: 0.75rem;
    }
    .patient-card-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
    }
    .patient-name {
      font-weight: 700;
      font-size: 0.95rem;
      color: #0f172a;
    }
    .patient-id-badge {
      background: #e2e8f0;
      color: #334155;
      font-size: 0.7rem;
      padding: 0.1rem 0.4rem;
      border-radius: 4px;
      font-weight: 600;
    }
    .patient-meta {
      font-size: 0.75rem;
      color: #64748b;
      margin-top: 0.2rem;
    }
    .patient-complaint {
      font-size: 0.8rem;
      color: #334155;
      margin-top: 0.4rem;
      line-height: 1.3;
    }
    .case-warning-banner {
      background: #fffbeb;
      border: 1px solid #f59e0b;
      border-radius: 8px;
      padding: 0.75rem;
      margin-bottom: 0.75rem;
    }
    .warning-header-row {
      display: flex;
      align-items: center;
      margin-bottom: 0.3rem;
    }
    .warning-pill {
      background: #fef3c7;
      color: #92400e;
      font-weight: 700;
      font-size: 0.75rem;
      padding: 0.15rem 0.45rem;
      border-radius: 4px;
    }
    .warning-text {
      font-size: 0.75rem;
      color: #78350f;
      margin: 0.3rem 0;
      line-height: 1.3;
    }
    .btn-dossier-action {
      background: #d97706;
      color: white;
      border: none;
      padding: 0.35rem 0.75rem;
      border-radius: 6px;
      font-size: 0.75rem;
      font-weight: 600;
      cursor: pointer;
      width: 100%;
      margin-top: 0.25rem;
    }
    .btn-dossier-action:hover { background: #b45309; }
    .case-complete-banner {
      background: #f0fdf4;
      border: 1px solid #10b981;
      border-radius: 8px;
      padding: 0.75rem;
      margin-bottom: 0.75rem;
      display: flex;
      justify-content: space-between;
      align-items: center;
    }
    .complete-pill {
      color: #047857;
      font-weight: 700;
      font-size: 0.75rem;
    }
    .btn-dossier-view {
      background: #10b981;
      color: white;
      border: none;
      padding: 0.3rem 0.6rem;
      border-radius: 4px;
      font-size: 0.75rem;
      font-weight: 600;
      cursor: pointer;
    }
    .totality-section {
      background: #f8fafc;
      border: 1px solid #cbd5e1;
      border-radius: 8px;
      margin-bottom: 0.75rem;
      overflow: hidden;
    }
    .totality-toggle-header {
      padding: 0.6rem 0.8rem;
      display: flex;
      justify-content: space-between;
      align-items: center;
      cursor: pointer;
      background: #f1f5f9;
      font-weight: 600;
      font-size: 0.8rem;
      color: #334155;
    }
    .totality-toggle-header:hover { background: #e2e8f0; }
    .totality-body {
      padding: 0.75rem;
      font-size: 0.78rem;
      background: white;
      border-top: 1px solid #e2e8f0;
    }
    .totality-item {
      margin-bottom: 0.5rem;
      padding-bottom: 0.4rem;
      border-bottom: 1px dashed #e2e8f0;
    }
    .totality-item:last-child { border-bottom: none; margin-bottom: 0; padding-bottom: 0; }
    .mod-agg { color: #dc2626; font-weight: 600; }
    .mod-amel { color: #16a34a; font-weight: 600; }
    .case-meta-section {
      background: white;
      border: 1px solid #e2e8f0;
      border-radius: 8px;
      padding: 0.85rem;
      margin-bottom: 0.85rem;
    }
    .case-meta-section h3, .observations-section h3 {
      font-size: 0.88rem;
      font-weight: 700;
      color: #1e293b;
      margin: 0 0 0.6rem 0;
    }
    .form-row {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 0.5rem;
    }
    .form-row label {
      font-size: 0.78rem;
      font-weight: 600;
      color: #475569;
    }
    .form-group-spaced {
      margin-bottom: 0.5rem;
    }
    .form-group-spaced label {
      display: block;
      font-size: 0.75rem;
      font-weight: 600;
      color: #475569;
      margin-bottom: 0.2rem;
    }
    .meta-select, .meta-input {
      width: 100%;
      box-sizing: border-box;
      padding: 0.4rem 0.5rem;
      border: 1px solid #cbd5e1;
      border-radius: 6px;
      font-size: 0.8rem;
    }
    .form-row .meta-select { width: 55%; }
    .btn-save-meta {
      background: #3b82f6;
      color: white;
      border: none;
      padding: 0.45rem 0.85rem;
      border-radius: 6px;
      font-size: 0.78rem;
      font-weight: 600;
      cursor: pointer;
      margin-top: 0.4rem;
    }
    .btn-save-meta:hover { background: #2563eb; }
    .saved-notice {
      color: #10b981;
      font-size: 0.75rem;
      font-weight: 700;
      margin-left: 0.5rem;
    }
    .observations-section {
      background: white;
      border: 1px solid #e2e8f0;
      border-radius: 8px;
      padding: 0.85rem;
    }
    .obs-header-row {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 0.6rem;
    }
    .btn-add-obs {
      background: #f1f5f9;
      border: 1px solid #cbd5e1;
      color: #1e293b;
      padding: 0.3rem 0.6rem;
      border-radius: 4px;
      font-size: 0.72rem;
      font-weight: 600;
      cursor: pointer;
    }
    .btn-add-obs:hover { background: #e2e8f0; }
    .obs-form-card {
      background: #f8fafc;
      border: 1px solid #cbd5e1;
      border-radius: 6px;
      padding: 0.75rem;
      margin-bottom: 0.75rem;
    }
    .obs-form-card h4 {
      font-size: 0.8rem;
      font-weight: 700;
      margin: 0 0 0.5rem 0;
      color: #0f172a;
    }
    .obs-textarea {
      width: 100%;
      box-sizing: border-box;
      padding: 0.4rem;
      border: 1px solid #cbd5e1;
      border-radius: 4px;
      font-size: 0.78rem;
      font-family: inherit;
    }
    .btn-submit-obs {
      background: #10b981;
      color: white;
      border: none;
      padding: 0.4rem 0.8rem;
      border-radius: 4px;
      font-size: 0.78rem;
      font-weight: 600;
      cursor: pointer;
      margin-top: 0.4rem;
    }
    .obs-timeline {
      display: flex;
      flex-direction: column;
      gap: 0.5rem;
    }
    .empty-obs {
      font-size: 0.75rem;
      color: #94a3b8;
      text-align: center;
      padding: 0.75rem;
    }
    .obs-card {
      border: 1px solid #e2e8f0;
      border-radius: 6px;
      padding: 0.6rem;
      background: #f8fafc;
      font-size: 0.75rem;
    }
    .obs-card-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 0.3rem;
    }
    .obs-reaction-pill {
      font-size: 0.68rem;
      font-weight: 700;
      padding: 0.1rem 0.4rem;
      border-radius: 4px;
    }
    .obs-reaction-pill.amelioration { background: #dcfce7; color: #15803d; }
    .obs-reaction-pill.aggravation { background: #fef2f2; color: #b91c1c; }
    .obs-reaction-pill.initial { background: #eff6ff; color: #1d4ed8; }
    .obs-reaction-pill.neutral { background: #f3f4f6; color: #4b5563; }
    .obs-date { font-size: 0.68rem; color: #64748b; }
    .obs-kent { margin-bottom: 0.2rem; color: #334155; }
    .obs-action { margin-bottom: 0.2rem; color: #0284c7; }
    .obs-notes { color: #475569; line-height: 1.3; }

    /* Center Panel Styles */
    .center-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
    }
    .sub-header-notes {
      font-size: 0.75rem;
      color: #64748b;
      margin-top: 0.15rem;
    }
    .rx-version-tag {
      background: #dbeafe;
      color: #1e40af;
      font-size: 0.75rem;
      font-weight: 700;
      padding: 0.25rem 0.6rem;
      border-radius: 4px;
    }
    .method-bar {
      background: #f8fafc;
      border: 1px solid #e2e8f0;
      border-radius: 8px;
      padding: 0.6rem 0.85rem;
      display: flex;
      align-items: center;
      gap: 1rem;
      margin-bottom: 0.85rem;
    }
    .method-label {
      font-size: 0.8rem;
      font-weight: 700;
      color: #334155;
    }
    .method-pill-group {
      display: flex;
      gap: 0.5rem;
    }
    .method-btn {
      border: 1px solid #cbd5e1;
      background: white;
      color: #475569;
      padding: 0.4rem 0.8rem;
      border-radius: 6px;
      font-size: 0.8rem;
      font-weight: 600;
      cursor: pointer;
      transition: all 0.15s ease;
    }
    .method-btn.active {
      border-color: #0284c7;
      background: #0284c7;
      color: white;
    }
    .remedy-families-tab-bar {
      display: flex;
      align-items: center;
      gap: 0.35rem;
      overflow-x: auto;
      padding-bottom: 0.4rem;
      margin-bottom: 0.6rem;
    }
    .family-label {
      font-size: 0.75rem;
      font-weight: 700;
      color: #64748b;
      margin-right: 0.3rem;
      white-space: nowrap;
    }
    .family-tab {
      border: 1px solid #cbd5e1;
      background: white;
      color: #334155;
      padding: 0.35rem 0.65rem;
      border-radius: 6px;
      font-size: 0.75rem;
      font-weight: 600;
      cursor: pointer;
      white-space: nowrap;
      transition: all 0.15s;
    }
    .family-tab:hover { background: #f1f5f9; }
    .family-tab.active {
      background: #0f172a;
      color: white;
      border-color: #0f172a;
    }
    .grid-section {
      margin-bottom: 1rem;
    }
    .section-instruction {
      font-size: 0.82rem;
      font-weight: 700;
      color: #1e293b;
      margin-bottom: 0.5rem;
      display: flex;
      justify-content: space-between;
    }
    .instruction-selection {
      color: #0284c7;
      font-weight: 600;
    }
    .remedy-grid {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(130px, 1fr));
      gap: 0.5rem;
    }
    .remedy-btn {
      border: 1px solid #cbd5e1;
      background: white;
      border-radius: 6px;
      padding: 0.5rem;
      text-align: left;
      cursor: pointer;
      transition: all 0.15s;
      display: flex;
      flex-direction: column;
    }
    .remedy-btn:hover { border-color: #3b82f6; background: #eff6ff; }
    .remedy-btn.selected {
      border-color: #2563eb;
      background: #dbeafe;
      box-shadow: 0 0 0 2px rgba(37, 99, 235, 0.2);
    }
    .remedy-code {
      font-weight: 800;
      font-size: 0.95rem;
      color: #1d4ed8;
    }
    .remedy-name {
      font-size: 0.75rem;
      font-weight: 600;
      color: #1e293b;
      margin-top: 0.15rem;
    }
    .remedy-affinity {
      font-size: 0.65rem;
      color: #64748b;
      margin-top: 0.15rem;
    }
    .polarity-section {
      margin-bottom: 1rem;
    }
    .wcag-note {
      font-size: 0.68rem;
      font-weight: normal;
      color: #64748b;
    }
    .polarity-picker {
      display: grid;
      grid-template-columns: repeat(5, 1fr);
      gap: 0.5rem;
    }
    .polarity-btn {
      border: 2px solid #cbd5e1;
      border-radius: 6px;
      padding: 0.5rem 0.3rem;
      cursor: pointer;
      display: flex;
      flex-direction: column;
      align-items: center;
      background: white;
      transition: all 0.15s;
    }
    .polarity-symbol { font-size: 1.15rem; font-weight: bold; }
    .polarity-text { font-size: 0.75rem; font-weight: 700; margin-top: 0.15rem; }
    .polarity-desc { font-size: 0.62rem; color: #64748b; text-align: center; }
    .polarity-btn.red.active { border-color: #ef4444; background: #fef2f2; color: #991b1b; }
    .polarity-btn.blue.active { border-color: #3b82f6; background: #eff6ff; color: #1e40af; }
    .polarity-btn.green.active { border-color: #10b981; background: #ecfdf5; color: #065f46; }
    .polarity-btn.yellow.active { border-color: #f59e0b; background: #fffbeb; color: #92400e; }
    .polarity-btn.white.active { border-color: #64748b; background: #f1f5f9; color: #0f172a; }
    .step-controls-grid {
      display: grid;
      grid-template-columns: repeat(4, 1fr);
      gap: 0.75rem;
      margin-bottom: 0.75rem;
    }
    .step-controls-grid-secondary {
      display: grid;
      grid-template-columns: 1fr 2fr 1fr;
      gap: 0.75rem;
      margin-bottom: 0.85rem;
      align-items: flex-end;
    }
    .control-group label {
      display: block;
      font-size: 0.75rem;
      font-weight: 600;
      margin-bottom: 0.25rem;
      color: #334155;
    }
    .control-select, .control-input {
      width: 100%;
      box-sizing: border-box;
      padding: 0.45rem 0.5rem;
      border: 1px solid #cbd5e1;
      border-radius: 6px;
      font-size: 0.82rem;
    }
    .action-align { display: flex; align-items: flex-end; }
    .btn-add {
      background: #0284c7;
      color: white;
      border: none;
      padding: 0.5rem 1rem;
      border-radius: 6px;
      font-weight: 700;
      font-size: 0.85rem;
      cursor: pointer;
      width: 100%;
      height: 38px;
    }
    .btn-add:disabled { opacity: 0.5; cursor: not-allowed; }
    .btn-add:hover:not(:disabled) { background: #0369a1; }
    .diet-regimen-section {
      background: #fffbeb;
      border: 1px solid #fde68a;
      border-radius: 8px;
      padding: 0.75rem;
      margin-bottom: 1rem;
    }
    .diet-header {
      display: flex;
      flex-direction: column;
      gap: 0.35rem;
      margin-bottom: 0.4rem;
    }
    .diet-header label {
      font-size: 0.78rem;
      font-weight: 700;
      color: #92400e;
    }
    .diet-chips {
      display: flex;
      flex-wrap: wrap;
      gap: 0.35rem;
    }
    .diet-chip {
      background: white;
      border: 1px solid #fcd34d;
      color: #78350f;
      padding: 0.2rem 0.5rem;
      border-radius: 12px;
      font-size: 0.7rem;
      font-weight: 600;
      cursor: pointer;
    }
    .diet-chip:hover { background: #fef3c7; }
    .diet-textarea {
      width: 100%;
      box-sizing: border-box;
      padding: 0.4rem;
      border: 1px solid #fcd34d;
      border-radius: 4px;
      font-size: 0.78rem;
      font-family: inherit;
    }
    .active-prescription-list {
      border-top: 1px solid #e2e8f0;
      padding-top: 0.85rem;
      margin-bottom: 1.25rem;
    }
    .rx-list-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 0.6rem;
    }
    .rx-list-header h3 {
      font-size: 0.95rem;
      font-weight: 700;
      margin: 0;
      color: #0f172a;
    }
    .rx-count-tag {
      background: #e2e8f0;
      color: #334155;
      font-size: 0.72rem;
      font-weight: 700;
      padding: 0.15rem 0.5rem;
      border-radius: 12px;
    }
    .empty-rx-notice {
      padding: 1.25rem;
      text-align: center;
      background: #f8fafc;
      border: 1px dashed #cbd5e1;
      border-radius: 6px;
      color: #64748b;
      font-size: 0.82rem;
    }
    .rx-item-card {
      border: 1px solid #e2e8f0;
      border-radius: 6px;
      padding: 0.65rem 0.85rem;
      margin-bottom: 0.5rem;
      background: #fafafa;
      display: flex;
      justify-content: space-between;
      align-items: center;
    }
    .rx-item-main { flex: 1; }
    .rx-item-header {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      margin-bottom: 0.25rem;
    }
    .rx-item-remedy {
      font-weight: 700;
      font-size: 0.9rem;
      color: #0f172a;
    }
    .rx-method-badge {
      font-size: 0.65rem;
      font-weight: 700;
      padding: 0.1rem 0.4rem;
      border-radius: 4px;
      background: #f3f4f6;
      color: #4b5563;
    }
    .rx-method-badge.repertorial {
      background: #ecfdf5;
      color: #065f46;
    }
    .polarity-tag {
      font-size: 0.65rem;
      padding: 0.1rem 0.4rem;
      border-radius: 4px;
      font-weight: 700;
    }
    .polarity-tag.red { background: #fee2e2; color: #b91c1c; }
    .polarity-tag.blue { background: #dbeafe; color: #1d4ed8; }
    .polarity-tag.green { background: #d1fae5; color: #047857; }
    .polarity-tag.yellow { background: #fef3c7; color: #b45309; }
    .polarity-tag.white { background: #f3f4f6; color: #374151; }
    .rx-item-details {
      font-size: 0.78rem;
      color: #475569;
    }
    .rx-item-instructions {
      font-size: 0.72rem;
      color: #64748b;
      margin-top: 0.2rem;
    }
    .rx-item-remove {
      background: none;
      border: none;
      color: #ef4444;
      font-size: 0.78rem;
      font-weight: 600;
      cursor: pointer;
      padding: 0.3rem 0.5rem;
    }
    .rx-item-remove:hover { color: #dc2626; }
    .finalize-bar {
      background: #f8fafc;
      border: 1px solid #cbd5e1;
      border-radius: 8px;
      padding: 0.85rem 1.25rem;
      display: flex;
      justify-content: space-between;
      align-items: center;
    }
    .final-summary {
      font-size: 0.82rem;
      color: #334155;
    }
    .final-summary div {
      font-size: 0.72rem;
      color: #64748b;
    }
    .btn-finalize {
      background: #059669;
      color: white;
      border: none;
      padding: 0.65rem 1.25rem;
      border-radius: 6px;
      font-weight: 700;
      cursor: pointer;
      font-size: 0.88rem;
    }
    .btn-finalize:disabled { opacity: 0.5; cursor: not-allowed; }
    .btn-finalize:hover:not(:disabled) { background: #047857; }
    .success-banner {
      margin-top: 1rem;
      background: #ecfdf5;
      border: 1px solid #10b981;
      border-radius: 8px;
      padding: 1rem;
      color: #065f46;
    }
    .success-banner h4 { margin: 0 0 0.4rem; font-size: 0.95rem; }
    .success-banner p { margin: 0.25rem 0; font-size: 0.82rem; }
    .upi-link-notice { color: #047857; font-weight: 600; margin-top: 0.5rem; }

    /* Right Panel Styles */
    .right-panel { border-right: none; border-left: 1px solid #e2e8f0; }
    .mm-search { padding: 0.65rem 0.85rem; border-bottom: 1px solid #e2e8f0; }
    .search-input {
      width: 100%;
      box-sizing: border-box;
      padding: 0.45rem 0.6rem;
      border: 1px solid #cbd5e1;
      border-radius: 6px;
      font-size: 0.82rem;
    }
    .mm-list { padding: 0.75rem; overflow-y: auto; flex: 1; }
    .mm-entry-card {
      border: 1px solid #e2e8f0;
      border-radius: 6px;
      padding: 0.7rem;
      margin-bottom: 0.6rem;
      cursor: pointer;
      transition: all 0.15s;
      background: white;
    }
    .mm-entry-card:hover { border-color: #0284c7; background: #f0f9ff; }
    .mm-entry-card.active { border-color: #0284c7; background: #e0f2fe; }
    .mm-entry-header { display: flex; align-items: center; gap: 0.4rem; }
    .mm-code { font-weight: 800; color: #0369a1; font-size: 0.9rem; }
    .mm-name { font-weight: 700; font-size: 0.82rem; color: #0f172a; flex: 1; }
    .mm-family-badge {
      background: #f1f5f9;
      color: #475569;
      font-size: 0.65rem;
      font-weight: 600;
      padding: 0.1rem 0.35rem;
      border-radius: 4px;
    }
    .mm-desc {
      font-size: 0.72rem;
      color: #475569;
      margin-top: 0.25rem;
      line-height: 1.3;
    }
    .mm-indications {
      font-size: 0.72rem;
      color: #334155;
      margin-top: 0.3rem;
      line-height: 1.3;
    }
    .btn-quick-apply {
      background: #e0f2fe;
      border: 1px solid #bae6fd;
      color: #0369a1;
      padding: 0.25rem 0.5rem;
      border-radius: 4px;
      font-size: 0.7rem;
      font-weight: 700;
      cursor: pointer;
      margin-top: 0.4rem;
      width: 100%;
    }
    .btn-quick-apply:hover { background: #0284c7; color: white; }
  `]
})
export class ConsultationWorkspaceComponent implements OnInit {
  api = inject(ApiService);
  auth = inject(AuthService);
  router = inject(Router);
  route = inject(ActivatedRoute);

  allPatients = signal<Patient[]>([]);
  activePatient = signal<Patient | null>(null);
  activeConsultation = signal<Consultation | null>(null);
  caseRecord = signal<HomeopathicCaseRecord | null>(null);

  isTotalityExpanded = signal<boolean>(false);
  metaSavedNotice = signal<boolean>(false);

  // Case Metadata
  caseType = 'Chronic';
  provisionalDiagnosis = '';
  finalDiagnosis = '';
  outcome = 'UnderTreatment';

  // Observation State
  observations = signal<ConsultationObservation[]>([]);
  isAddingObservation = signal<boolean>(false);
  newReactionType = 'InitialEncounter';
  newKentsObservation = 'Obs 3: Aggravation quick, short, strong with rapid improvement';
  newSecondPrescriptionAction = 'Continue';
  newObservationNotes = '';

  // Rx Builder State
  selectedMethod = 'Repertorial';
  activeFamilyFilter = 'All';
  selectedRemedy: any = null;
  selectedPolarity = 'Red';
  selectedDilution = '30C';
  selectedVehicle = 'Sugar Globules';
  dosage = '5 globules';
  frequency = 'TDS';
  duration = '14 days';
  itemInstructions = 'Take 30 mins before food';

  dietAndRegimenInstructions = 'Avoid coffee, raw onions, garlic, and camphor during remedy action.';
  dietPresets = [
    'Avoid coffee & strong tea',
    'Avoid raw onions & garlic',
    'Avoid camphor & perfumes',
    'Take 30 mins before meals on empty stomach'
  ];

  currentItems: PrescriptionItem[] = [];
  finalizedResult: any = null;

  mmSearchQuery = '';
  materiaMedicaEntries = signal<MateriaMedicaEntry[]>([]);

  remedyFamilies = [
    { key: 'All', label: 'All Families' },
    { key: 'Scrofoloso', label: 'Scrofoloso (S)' },
    { key: 'Angiotico', label: 'Angiotico (A)' },
    { key: 'Canceroso', label: 'Canceroso (C)' },
    { key: 'Febbrifugo', label: 'Febbrifugo (F)' },
    { key: 'Pettorale', label: 'Pettorale (P)' },
    { key: 'Linfatico', label: 'Linfatico (L)' },
    { key: 'Vermifugo', label: 'Vermifugo (Ver)' },
    { key: 'Venereo', label: 'Venereo (Ven)' },
    { key: 'Special', label: 'Special (Sy)' }
  ];

  allRemedies = [
    // Scrofoloso
    { code: 'S1', name: 'Scrofoloso 1', family: 'Scrofoloso', affinity: 'Lymph & Glands' },
    { code: 'S2', name: 'Scrofoloso 2', family: 'Scrofoloso', affinity: 'Glandular Induration' },
    { code: 'S3', name: 'Scrofoloso 3', family: 'Scrofoloso', affinity: 'Skin & Eczema' },
    { code: 'S5', name: 'Scrofoloso 5', family: 'Scrofoloso', affinity: 'Liver & Digestion' },
    { code: 'S6', name: 'Scrofoloso 6', family: 'Scrofoloso', affinity: 'Kidney Drainage' },
    { code: 'S10', name: 'Scrofoloso 10', family: 'Scrofoloso', affinity: 'Nerve Alterative' },
    { code: 'S11', name: 'Scrofoloso 11', family: 'Scrofoloso', affinity: 'Intestinal Sluggishness' },
    { code: 'S12', name: 'Scrofoloso 12', family: 'Scrofoloso', affinity: 'Ophthalmic Lymphatics' },
    { code: 'S-Pass', name: 'Scrofoloso Pass', family: 'Scrofoloso', affinity: 'Spasmodic Lymph' },
    // Angiotico
    { code: 'A1', name: 'Angiotico 1', family: 'Angiotico', affinity: 'Arterial Tonic' },
    { code: 'A2', name: 'Angiotico 2', family: 'Angiotico', affinity: 'Venous Sluggishness' },
    { code: 'A3', name: 'Angiotico 3', family: 'Angiotico', affinity: 'Capillary & Heart' },
    // Canceroso
    { code: 'C1', name: 'Canceroso 1', family: 'Canceroso', affinity: 'Cellular Tonic' },
    { code: 'C2', name: 'Canceroso 2', family: 'Canceroso', affinity: 'Bladder & Glands' },
    { code: 'C3', name: 'Canceroso 3', family: 'Canceroso', affinity: 'Skin Ulcerations' },
    { code: 'C4', name: 'Canceroso 4', family: 'Canceroso', affinity: 'Bone & Periosteum' },
    { code: 'C5', name: 'Canceroso 5', family: 'Canceroso', affinity: 'Liver & Gallbladder' },
    { code: 'C6', name: 'Canceroso 6', family: 'Canceroso', affinity: 'Kidney & Bladder' },
    { code: 'C10', name: 'Canceroso 10', family: 'Canceroso', affinity: 'Laryngeal Tissues' },
    // Febbrifugo
    { code: 'F1', name: 'Febbrifugo 1', family: 'Febbrifugo', affinity: 'Nerve & Thermal' },
    { code: 'F2', name: 'Febbrifugo 2', family: 'Febbrifugo', affinity: 'Neuralgia & Spasms' },
    // Pettorale
    { code: 'P1', name: 'Pettorale 1', family: 'Pettorale', affinity: 'Bronchial Mucosa' },
    { code: 'P2', name: 'Pettorale 2', family: 'Pettorale', affinity: 'Deep Pulmonary' },
    { code: 'P3', name: 'Pettorale 3', family: 'Pettorale', affinity: 'Pleural Membrane' },
    { code: 'P4', name: 'Pettorale 4', family: 'Pettorale', affinity: 'Suffocative Cough' },
    { code: 'P9', name: 'Pettorale 9', family: 'Pettorale', affinity: 'Chronic Catarrh' },
    // Linfatico
    { code: 'L1', name: 'Linfatico 1', family: 'Linfatico', affinity: 'Spleen & Lymph System' },
    { code: 'L2', name: 'Linfatico 2', family: 'Linfatico', affinity: 'Fluid Stagnation' },
    // Vermifugo
    { code: 'Ver1', name: 'Vermifugo 1', family: 'Vermifugo', affinity: 'Intestinal Parasites' },
    { code: 'Ver2', name: 'Vermifugo 2', family: 'Vermifugo', affinity: 'Tapeworm & Colic' },
    // Venereo
    { code: 'Ven1', name: 'Venereo 1', family: 'Venereo', affinity: 'Genito-Urinary Mucosa' },
    { code: 'Ven2', name: 'Venereo 2', family: 'Venereo', affinity: 'Pelvic Inflammation' },
    { code: 'Ven5', name: 'Venereo 5', family: 'Venereo', affinity: 'Syphilitic Diathesis' },
    // Special
    { code: 'Sy', name: 'Sinassio', family: 'Special', affinity: 'Synergistic Balancer' },
    { code: 'Domfin', name: 'Domfin', family: 'Special', affinity: 'Nervous Calmer' }
  ];

  polarityOptions = [
    { name: 'Red', symbol: '(+)', desc: 'Stimulating / Dynamic / Arterial', cssClass: 'red' },
    { name: 'Blue', symbol: '(-)', desc: 'Sedating / Calming / Venous', cssClass: 'blue' },
    { name: 'Green', symbol: '(~)', desc: 'Neutral / Balancer / Granulator', cssClass: 'green' },
    { name: 'Yellow', symbol: '(⚡)', desc: 'Vital / Thermal / Nervous', cssClass: 'yellow' },
    { name: 'White', symbol: '(⚪)', desc: 'Universal / Unpolarized', cssClass: 'white' }
  ];

  get filteredRemedies() {
    if (this.activeFamilyFilter === 'All') return this.allRemedies;
    return this.allRemedies.filter(r => r.family === this.activeFamilyFilter);
  }

  ngOnInit() {
    this.searchMateriaMedica();
    this.loadPatients();
  }

  loadPatients() {
    this.api.getPatients().subscribe(patients => {
      this.allPatients.set(patients || []);
      if (patients && patients.length > 0) {
        // Check query param for patientId
        this.route.queryParams.subscribe(params => {
          const targetId = params['patientId'];
          const matched = targetId ? patients.find(p => p.id === targetId) : null;
          const chosen = matched || patients[0];
          this.setActivePatient(chosen);
        });
      }
    });
  }

  onPatientChange(patientId: string) {
    const p = this.allPatients().find(x => x.id === patientId);
    if (p) {
      this.setActivePatient(p);
    }
  }

  setActivePatient(patient: Patient) {
    this.activePatient.set(patient);

    // Fetch Detailed Case Record Dossier
    this.api.getDetailedProfile(patient.id).subscribe(profile => {
      this.caseRecord.set(profile.caseRecord || null);
    });

    // Fetch Consultation
    this.api.getPatientConsultation(patient.id).subscribe(c => {
      this.activeConsultation.set(c);
      if (c) {
        this.caseType = c.caseType || 'Chronic';
        this.provisionalDiagnosis = c.provisionalDiagnosis || '';
        this.finalDiagnosis = c.finalDiagnosis || '';
        this.outcome = c.outcome || 'UnderTreatment';
        this.observations.set(c.observations || []);
      }
    });
  }

  openCaseDossier(patientId: string) {
    this.router.navigate(['/patients', patientId, 'case-record']);
  }

  saveClinicalDetails() {
    const c = this.activeConsultation();
    if (!c) return;

    this.api.updateConsultationDetails(c.id, {
      caseType: this.caseType,
      provisionalDiagnosis: this.provisionalDiagnosis,
      finalDiagnosis: this.finalDiagnosis,
      outcome: this.outcome
    }).subscribe(updated => {
      this.activeConsultation.set(updated);
      this.metaSavedNotice.set(true);
      setTimeout(() => this.metaSavedNotice.set(false), 2500);
    });
  }

  submitObservation() {
    const c = this.activeConsultation();
    if (!c || !this.newObservationNotes.trim()) return;

    this.api.addConsultationObservation(c.id, {
      reactionType: this.newReactionType,
      kentsObservation: this.newKentsObservation,
      secondPrescriptionAction: this.newSecondPrescriptionAction,
      notes: this.newObservationNotes
    }).subscribe(obs => {
      this.observations.update(list => [obs, ...list]);
      this.newObservationNotes = '';
      this.isAddingObservation.set(false);
    });
  }

  getReactionClass(reaction: string): string {
    switch (reaction) {
      case 'Amelioration': return 'amelioration';
      case 'HomoeopathicAggravation':
      case 'DiseaseAggravation': return 'aggravation';
      case 'InitialEncounter': return 'initial';
      default: return 'neutral';
    }
  }

  selectRemedy(remedy: any) {
    this.selectedRemedy = remedy;
  }

  selectFromMateriaMedica(entry: MateriaMedicaEntry) {
    const found = this.allRemedies.find(r => r.code === entry.code);
    this.selectedRemedy = found || { code: entry.code, name: entry.name, family: entry.family, affinity: entry.indications };
    // Auto switch family tab to the selected remedy's family
    if (this.selectedRemedy.family) {
      this.activeFamilyFilter = this.selectedRemedy.family;
    }
  }

  searchMateriaMedica() {
    this.api.getMateriaMedica(this.mmSearchQuery).subscribe(entries => {
      this.materiaMedicaEntries.set(entries);
    });
  }

  appendDietPreset(preset: string) {
    if (!this.dietAndRegimenInstructions.includes(preset)) {
      this.dietAndRegimenInstructions = this.dietAndRegimenInstructions
        ? `${this.dietAndRegimenInstructions} ${preset}.`
        : `${preset}.`;
    }
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
      duration: this.duration,
      selectionMethod: this.selectedMethod,
      frequency: this.frequency,
      instructions: this.itemInstructions
    });
  }

  removeItem(index: number) {
    this.currentItems.splice(index, 1);
  }

  finalizePrescription() {
    if (this.currentItems.length === 0) return;

    const pat = this.activePatient();
    const c = this.activeConsultation();

    const payload = {
      patientId: pat?.id || 'p1000000-0000-0000-0000-000000000001',
      consultationId: c?.id || `c-${Date.now()}`,
      items: this.currentItems,
      dietAndRegimenInstructions: this.dietAndRegimenInstructions,
      clinicalNotes: `Case Type: ${this.caseType}. Prov Dx: ${this.provisionalDiagnosis}. Final Dx: ${this.finalDiagnosis}. Outcome: ${this.outcome}`
    };

    this.api.finalizePrescription(payload).subscribe(res => {
      this.finalizedResult = res;
    });
  }
}
