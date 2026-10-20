import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { ApiService } from '../../core/api.service';
import { Patient, HomeopathicCaseRecord, UpdateHomeopathicCaseRecordRequest } from '../../core/models';

@Component({
  selector: 'app-case-dossier',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="dossier-container" *ngIf="patient()">
      <!-- Sticky Navigation & Status Header -->
      <header class="dossier-header">
        <div class="header-top">
          <div class="header-left">
            <button class="btn-back" (click)="goBack()">← Back to Registry</button>
            <div class="patient-title-group">
              <h1 class="patient-name">{{ patient()?.name }}</h1>
              <span class="patient-sub">
                {{ patient()?.gender }} • DOB: {{ patient()?.dob }} • Tel: {{ patient()?.phone }}
              </span>
            </div>
          </div>

          <div class="header-right">
            <div class="completion-pill-box">
              <span
                class="status-badge"
                [class.complete]="patient()?.completionPercentage === 100"
                [class.incomplete]="patient()?.completionPercentage !== 100"
              >
                {{ patient()?.completionPercentage === 100 ? '✅ Dossier Complete' : '⚠️ Case Record Incomplete' }}
                ({{ patient()?.completionPercentage }}%)
              </span>
            </div>
            <button class="btn btn-save" (click)="saveDossier()" [disabled]="saving()">
              {{ saving() ? 'Saving...' : '💾 Save Clinical Dossier' }}
            </button>
          </div>
        </div>

        <!-- Progress Bar -->
        <div class="progress-container">
          <div class="progress-bar-track">
            <div
              class="progress-bar-fill"
              [style.width.%]="patient()?.completionPercentage || 0"
              [class.complete]="patient()?.completionPercentage === 100"
            ></div>
          </div>
          <div class="progress-labels">
            <span>Quick Intake Baseline (40%)</span>
            <span>Comprehensive Organon Totality (100%)</span>
          </div>
        </div>

        <!-- Persistent Non-Blocking Warning Banner -->
        <div class="warning-banner" *ngIf="missingFields().length > 0">
          <div class="warning-icon">⚠️</div>
          <div class="warning-content">
            <strong>Case Record Incomplete: Missing required clinical intake fields:</strong>
            <div class="missing-tags">
              <span class="missing-tag" *ngFor="let field of missingFields()">
                {{ getFriendlyFieldName(field) }}
              </span>
            </div>
          </div>
        </div>

        <div class="success-banner" *ngIf="saveSuccessMessage()">
          ✓ {{ saveSuccessMessage() }}
        </div>
      </header>

      <!-- Patient Chief Complaint Card -->
      <div class="complaint-summary-card">
        <div class="complaint-label">Primary Chief Complaint (from Quick Intake):</div>
        <div class="complaint-value">{{ patient()?.primaryChiefComplaint || 'No chief complaint recorded at intake.' }}</div>
      </div>

      <!-- Tab Navigation -->
      <nav class="dossier-tabs" role="tablist">
        <button
          class="tab-btn"
          [class.active]="activeTab === 'complaints'"
          (click)="activeTab = 'complaints'"
        >
          1. Presenting Complaints & Modalities
          <span class="tab-warn" *ngIf="isTabMissing('complaints')">•</span>
        </button>
        <button
          class="tab-btn"
          [class.active]="activeTab === 'physical'"
          (click)="activeTab = 'physical'"
        >
          2. Physical Generals (Thermals, Functional, Elimination)
          <span class="tab-warn" *ngIf="isTabMissing('physical')">•</span>
        </button>
        <button
          class="tab-btn"
          [class.active]="activeTab === 'mental'"
          (click)="activeTab = 'mental'"
        >
          3. Mental Generals & Mind
        </button>
        <button
          class="tab-btn"
          [class.active]="activeTab === 'history'"
          (click)="activeTab = 'history'"
        >
          4. Personal & Medical History
        </button>
        <button
          class="tab-btn"
          [class.active]="activeTab === 'vitals'"
          (click)="activeTab = 'vitals'"
        >
          5. General Exam, Vitals & Allergies
          <span class="tab-warn" *ngIf="isTabMissing('vitals')">•</span>
        </button>
      </nav>

      <!-- Tab Content Area -->
      <main class="dossier-body">
        <!-- TAB 1: Presenting Complaints & Modalities -->
        <div *ngIf="activeTab === 'complaints'" class="tab-panel">
          <h2 class="panel-heading">Presenting Complaints, Onset & Modalities</h2>
          <p class="panel-desc">Organon Case Record (Page 3): Detailed narration, location, sensation & modifying modalities ($&lt;, &gt;$).</p>

          <div class="form-group mandatory-field">
            <label>
              Chief Complaint Timeline / Onset *
              <span class="mandatory-badge">Required</span>
            </label>
            <textarea
              [(ngModel)]="form.chiefComplaintTimeline"
              rows="3"
              class="form-control"
              placeholder="e.g. Origin, duration, and chronological progression along with mode of onset, probable causes, and treatment outcome"
            ></textarea>
            <small class="field-hint">Specify chronological development and causative circumstances (Aetiology).</small>
          </div>

          <div class="form-row">
            <div class="form-group half">
              <label>Location</label>
              <input
                type="text"
                [(ngModel)]="form.location"
                class="form-control"
                placeholder="e.g. Tissues, Organs, Systems, Extension & Duration, Direction & frequency"
              />
            </div>

            <div class="form-group half">
              <label>Sensation & Pathology</label>
              <input
                type="text"
                [(ngModel)]="form.sensationAndPathology"
                class="form-control"
                placeholder="e.g. Bursting, throbbing, stitching, burning, dull ache, induration"
              />
            </div>
          </div>

          <div class="form-row">
            <div class="form-group half">
              <label>Aggravating Modalities (&lt;)</label>
              <textarea
                [(ngModel)]="form.aggravatingModalities"
                rows="3"
                class="form-control"
                placeholder="e.g. Cold air, cold drafts, morning, after eating, motion, damp weather, 4 PM to 8 PM"
              ></textarea>
              <small class="field-hint">Factors, times, and circumstances that worsen symptoms.</small>
            </div>

            <div class="form-group half">
              <label>Ameliorating Modalities (&gt;)</label>
              <textarea
                [(ngModel)]="form.amelioratingModalities"
                rows="3"
                class="form-control"
                placeholder="e.g. Warm applications, gentle motion, resting in dark room, open fresh air"
              ></textarea>
              <small class="field-hint">Circumstances or applications that relieve symptoms.</small>
            </div>
          </div>

          <div class="form-row">
            <div class="form-group half">
              <label>Ailments From (A/F - Causation)</label>
              <input
                type="text"
                [(ngModel)]="form.ailmentsFrom"
                class="form-control"
                placeholder="e.g. Grief, anger, wet weather exposure, financial loss, suppression of skin eruptions"
              />
            </div>

            <div class="form-group half">
              <label>Concomitants / Accompaniments</label>
              <input
                type="text"
                [(ngModel)]="form.concomitantSymptoms"
                class="form-control"
                placeholder="e.g. Nausea during headache; cold sweat with colic; vertigo with cervical pain"
              />
            </div>
          </div>
        </div>

        <!-- TAB 2: Physical Generals (Organon Page 5: Thermals, Functional, Elimination) -->
        <div *ngIf="activeTab === 'physical'" class="tab-panel">
          <h2 class="panel-heading">Physical Generals (Organon Case Record Page 5)</h2>
          <p class="panel-desc">Physical Totality: Thermals, Functional Vitality (Appetite, Thirst, Sex, Sleep, Dreams) and Elimination (Stool, Urine, Sweat, Breath, Discharges).</p>

          <!-- 1. Thermals -->
          <div class="section-card mandatory-field">
            <div class="section-card-header">
              <label class="section-title">
                Thermals *
                <span class="mandatory-badge">Required</span>
              </label>
              <span class="section-hint">General constitutional reaction to heat, cold, seasons & drafts</span>
            </div>

            <div class="form-row" style="margin-top: 0.5rem;">
              <div class="form-group half">
                <label>Thermal State Selection</label>
                <select [(ngModel)]="form.thermals" class="form-control">
                  <option value="">-- Select Thermal State --</option>
                  <option value="Chilly (Sensitive to cold air & drafts)">Chilly (Sensitive to cold air & drafts)</option>
                  <option value="Hot (Intolerant of warm rooms & summer sun)">Hot (Intolerant of warm rooms & summer sun)</option>
                  <option value="Ambithermal (Equally affected by both extremes)">Ambithermal (Equally affected by both extremes)</option>
                  <option value="Sensitive to damp cold weather">Sensitive to damp cold weather</option>
                  <option value="Sensitive to drafts / open air">Sensitive to drafts / open air</option>
                </select>
              </div>

              <div class="form-group half">
                <label>Thermals Details & Modalities</label>
                <input
                  type="text"
                  [(ngModel)]="form.thermalTolerance"
                  class="form-control"
                  placeholder="e.g. Chilly but desires open air; catches cold easily; intolerable sun headaches"
                />
              </div>
            </div>
          </div>

          <!-- 2. Functional (Page 5 Table Column 1) -->
          <div class="section-card">
            <div class="section-card-header">
              <h3 class="section-title">Functional</h3>
              <span class="section-hint">Appetite, Thirst, Sex, Sleep & Dreams (Organon Page 5)</span>
            </div>

            <div class="form-grid-2">
              <div class="form-group mandatory-field">
                <label>
                  Appetite *
                  <span class="mandatory-badge">Required</span>
                </label>
                <textarea
                  [(ngModel)]="form.appetite"
                  rows="2"
                  class="form-control"
                  placeholder="e.g. Good / Ravenous / Loss of appetite; easily satiated; canine hunger at 11 AM; hunger headaches"
                ></textarea>
              </div>

              <div class="form-group mandatory-field">
                <label>
                  Thirst *
                  <span class="mandatory-badge">Required</span>
                </label>
                <textarea
                  [(ngModel)]="form.thirst"
                  rows="2"
                  class="form-control"
                  placeholder="e.g. Thirstless with dry tongue; thirst for large quantities of cold water at long intervals; warm water sips"
                ></textarea>
              </div>

              <div class="form-group">
                <label>Sex</label>
                <textarea
                  [(ngModel)]="form.sex"
                  rows="2"
                  class="form-control"
                  placeholder="e.g. Sexual desire (normal / increased / diminished); aggravations after coitus; marital history"
                ></textarea>
              </div>

              <div class="form-group mandatory-field">
                <label>
                  Sleep *
                  <span class="mandatory-badge">Required</span>
                </label>
                <textarea
                  [(ngModel)]="form.sleep"
                  rows="2"
                  class="form-control"
                  placeholder="e.g. Sleep latency; waking at 3 AM; unrefreshing sleep; posture (prone, on back, right/left side); starting in sleep"
                ></textarea>
              </div>

              <div class="form-group full-col">
                <label>Dreams</label>
                <textarea
                  [(ngModel)]="form.dreams"
                  rows="2"
                  class="form-control"
                  placeholder="e.g. Recurring dreams of falling, water, snakes, fire, ghosts, dead relatives, uncompleted daily work"
                ></textarea>
              </div>
            </div>
          </div>

          <!-- 3. Elimination (Page 5 Table Column 2) -->
          <div class="section-card">
            <div class="section-card-header">
              <h3 class="section-title">Elimination</h3>
              <span class="section-hint">Stool, Urine, Sweat, Breath & Discharges (Organon Page 5)</span>
            </div>

            <div class="form-grid-2">
              <div class="form-group">
                <label>Stool</label>
                <textarea
                  [(ngModel)]="form.stool"
                  rows="2"
                  class="form-control"
                  placeholder="e.g. Constipated, hard dry balls; ineffectual urging; painless morning diarrhea; straining; after-stool weakness"
                ></textarea>
              </div>

              <div class="form-group">
                <label>Urine</label>
                <textarea
                  [(ngModel)]="form.urine"
                  rows="2"
                  class="form-control"
                  placeholder="e.g. Frequency, burning during/after micturition, nocturnal enuresis, involuntary with cough, red sand sediment"
                ></textarea>
              </div>

              <div class="form-group">
                <label>Sweat</label>
                <textarea
                  [(ngModel)]="form.sweat"
                  rows="2"
                  class="form-control"
                  placeholder="e.g. Profuse on head during sleep; palms and soles; offensive, sour, yellow staining; cold clammy sweat"
                ></textarea>
              </div>

              <div class="form-group">
                <label>Breath</label>
                <textarea
                  [(ngModel)]="form.breath"
                  rows="2"
                  class="form-control"
                  placeholder="e.g. Fetid morning halitosis; sour odor; offensive breath with coated tongue and aphthae"
                ></textarea>
              </div>

              <div class="form-group full-col">
                <label>Discharges</label>
                <textarea
                  [(ngModel)]="form.discharges"
                  rows="2"
                  class="form-control"
                  placeholder="e.g. Nasal catarrh (thick green / thin acrid); leucorrhoea (bland / burning / ropy); ear discharge; mucous discharges"
                ></textarea>
              </div>
            </div>
          </div>

          <!-- 4. Cravings & Aversions -->
          <div class="section-card">
            <div class="section-card-header">
              <h3 class="section-title">Cravings & Aversions (Desires & Intolerances)</h3>
              <span class="section-hint">Specific food cravings, desires, and aversions</span>
            </div>

            <div class="form-row">
              <div class="form-group half">
                <label>Cravings / Desires</label>
                <input
                  type="text"
                  [(ngModel)]="form.cravingsAversions"
                  class="form-control"
                  placeholder="e.g. Desires sweets, salt, spicy pungent, warm milk, cold drinks, sour pickles"
                />
              </div>

              <div class="form-group half">
                <label>Aversions</label>
                <input
                  type="text"
                  [(ngModel)]="form.socialStatus"
                  class="form-control"
                  placeholder="e.g. Aversion to meat, fatty foods, milk, bread, vegetables, warm cooked food"
                />
              </div>
            </div>
          </div>
        </div>

        <!-- TAB 3: Mental Generals -->
        <div *ngIf="activeTab === 'mental'" class="tab-panel">
          <h2 class="panel-heading">Mental Generals (Organon Page 5)</h2>
          <p class="panel-desc">Will & emotions including motivation, Understanding and intellect, Memory (Organon §210–230).</p>

          <div class="form-group">
            <label>Will & Emotions (including Motivation)</label>
            <textarea
              [(ngModel)]="form.willAndEmotions"
              rows="3"
              class="form-control"
              placeholder="e.g. Irritable when contradicted; anxiety regarding health and family; weeping disposition, easily consoled; restlessness; fear of darkness/death"
            ></textarea>
            <small class="field-hint">Emotional reactions, fears, affection, anger, grief, depression, and motivational drive.</small>
          </div>

          <div class="form-row">
            <div class="form-group half">
              <label>Understanding & Intellect</label>
              <textarea
                [(ngModel)]="form.understandingAndIntellect"
                rows="2"
                class="form-control"
                placeholder="e.g. Quick comprehension; dull sluggish thinking; confusion of mind; fastidious and orderly"
              ></textarea>
            </div>

            <div class="form-group half">
              <label>Memory</label>
              <textarea
                [(ngModel)]="form.memory"
                rows="2"
                class="form-control"
                placeholder="e.g. Good memory; forgets recent events; forgets names; weak concentration from mental overwork"
              ></textarea>
            </div>
          </div>

          <div class="form-row">
            <div class="form-group half">
              <label>Mind & Disposition Notes</label>
              <input
                type="text"
                [(ngModel)]="form.mindDisposition"
                class="form-control"
                placeholder="e.g. Mild, quiet, sensitive, conscientious about trifles"
              />
            </div>

            <div class="form-group half">
              <label>Constitutional Temperament</label>
              <select [(ngModel)]="form.temperament" class="form-control">
                <option value="">-- Select Temperament --</option>
                <option value="Lymphatic (Cesare Mattei Primary)">Lymphatic (Cesare Mattei Primary)</option>
                <option value="Sanguine / Plethoric">Sanguine / Plethoric</option>
                <option value="Bilious / Choleric">Bilious / Choleric</option>
                <option value="Nervous / Melancholic">Nervous / Melancholic</option>
              </select>
            </div>
          </div>
        </div>

        <!-- TAB 4: Personal & Medical History -->
        <div *ngIf="activeTab === 'history'" class="tab-panel">
          <h2 class="panel-heading">Personal & Medical History (Organon Page 2 & 4)</h2>
          <p class="panel-desc">Investigation of previous illness, miasmatic background, family history, and social life situation.</p>

          <div class="form-group">
            <label>H/O Previous Illness</label>
            <textarea
              [(ngModel)]="form.pastMedicalSurgicalHistory"
              rows="3"
              class="form-control"
              placeholder="e.g. Age / Year, Illness, Treatment Adopted (Allopathic / Homoeopathic), and Outcome"
            ></textarea>
            <small class="field-hint">Chronological history of major infections, surgeries, and drug suppressions.</small>
          </div>

          <div class="form-group">
            <label>Family Medical History</label>
            <textarea
              [(ngModel)]="form.familyMedicalHistory"
              rows="3"
              class="form-control"
              placeholder="e.g. Maternal diabetes, hypertension; paternal asthma, tuberculosis, arthritis; cancer history"
            ></textarea>
          </div>

          <!-- Personal Data / Life Situation -->
          <div class="section-card">
            <h3 class="section-title">Personal History & Life Situation</h3>

            <div class="form-row">
              <div class="form-group half">
                <label>Diet</label>
                <select [(ngModel)]="form.diet" class="form-control">
                  <option value="">-- Select Diet --</option>
                  <option value="Veg">Vegetarian</option>
                  <option value="Non-Veg">Non-Vegetarian</option>
                  <option value="Mixed">Mixed</option>
                </select>
              </div>

              <div class="form-group half">
                <label>Family Status</label>
                <select [(ngModel)]="form.familyStatus" class="form-control">
                  <option value="">-- Select --</option>
                  <option value="Nuclear">Nuclear Family</option>
                  <option value="Joint">Joint Family</option>
                  <option value="Extended">Extended Family</option>
                </select>
              </div>
            </div>

            <div class="form-row">
              <div class="form-group half">
                <label>Place of Birth</label>
                <input type="text" [(ngModel)]="form.placeOfBirth" class="form-control" placeholder="e.g. Chennai, India" />
              </div>

              <div class="form-group half">
                <label>Occupation</label>
                <input type="text" [(ngModel)]="form.occupation" class="form-control" placeholder="e.g. Teacher, Engineer" />
              </div>
            </div>

            <div class="form-row">
              <div class="form-group half">
                <label>Marital Status</label>
                <select [(ngModel)]="form.maritalStatus" class="form-control">
                  <option value="">-- Select --</option>
                  <option value="Single">Single</option>
                  <option value="Married">Married</option>
                  <option value="Widowed">Widowed</option>
                  <option value="Divorced">Divorced</option>
                  <option value="Live in relation">Live in relation</option>
                </select>
              </div>

              <div class="form-group half">
                <label>Religion / Education</label>
                <input type="text" [(ngModel)]="form.religion" class="form-control" placeholder="e.g. Education & background" />
              </div>
            </div>
          </div>
        </div>

        <!-- TAB 5: General Exam, Vitals & Allergies -->
        <div *ngIf="activeTab === 'vitals'" class="tab-panel">
          <h2 class="panel-heading">Physical Examination, Vitals & Allergies (Organon Page 5)</h2>
          <p class="panel-desc">Objective biometric observation, general examination, and mandatory allergy confirmation.</p>

          <!-- General Examination -->
          <div class="section-card">
            <h3 class="section-title">General Examination</h3>
            <div class="form-row">
              <div class="form-group half">
                <label>Conscious State</label>
                <select [(ngModel)]="form.consciousState" class="form-control">
                  <option value="Conscious">Conscious</option>
                  <option value="Semi-conscious">Semi-conscious</option>
                  <option value="Unconscious">Unconscious</option>
                </select>
              </div>

              <div class="form-group half">
                <label>General Built & Nutrition</label>
                <input
                  type="text"
                  [(ngModel)]="form.builtAndNutrition"
                  class="form-control"
                  placeholder="e.g. Well built and nourished / Moderate / Emaciated"
                />
              </div>
            </div>

            <div class="form-group">
              <label>General Appearance (Expression, Look, Decubitus, Gait)</label>
              <input
                type="text"
                [(ngModel)]="form.generalAppearance"
                class="form-control"
                placeholder="e.g. Anxious expression, pale look, lies on abdomen, erect gait"
              />
            </div>

            <div class="form-group">
              <label>Systemic Signs & Inspection</label>
              <input
                type="text"
                [(ngModel)]="form.systemicSigns"
                class="form-control"
                placeholder="e.g. Anaemia: absent, Jaundice: nil, Cyanosis: nil, Clubbing: nil, Oedema: mild pedal, Skin: pigmentation"
              />
            </div>
          </div>

          <!-- Vitals Grid -->
          <div class="section-card">
            <h3 class="section-title">Vitals</h3>
            <div class="vitals-grid">
              <div class="vital-item">
                <label>Blood Pressure (mm Hg)</label>
                <input type="text" [(ngModel)]="form.bloodPressure" class="form-control" placeholder="e.g. 120/80" />
              </div>
              <div class="vital-item">
                <label>Pulse Rate (/min)</label>
                <input type="text" [(ngModel)]="form.pulseRate" class="form-control" placeholder="e.g. 72" />
              </div>
              <div class="vital-item">
                <label>Resp. Rate (/min)</label>
                <input type="text" [(ngModel)]="form.respiratoryRate" class="form-control" placeholder="e.g. 18" />
              </div>
              <div class="vital-item">
                <label>Height (cm)</label>
                <input
                  type="number"
                  [(ngModel)]="form.heightCm"
                  (ngModelChange)="onHeightWeightChange()"
                  class="form-control"
                  placeholder="e.g. 170"
                />
              </div>
              <div class="vital-item">
                <label>Weight (kg)</label>
                <input
                  type="number"
                  [(ngModel)]="form.weightKg"
                  (ngModelChange)="onHeightWeightChange()"
                  class="form-control"
                  placeholder="e.g. 68"
                />
              </div>
              <div class="vital-item">
                <label>BMI (kg/m²)</label>
                <input type="number" [value]="form.bmi" readonly class="form-control readonly-input" />
              </div>
            </div>
          </div>

          <!-- Mandatory Allergy Safety Assessment -->
          <div class="allergy-section mandatory-field">
            <div class="allergy-header">
              <label class="allergy-title">
                Drug & Environmental Substance Allergy Assessment *
                <span class="mandatory-badge">Required</span>
              </label>
              <p class="field-hint">Mandatory patient safety verification before prescribing or dispensing medicines.</p>
            </div>

            <div class="allergy-radio-group">
              <label class="radio-label">
                <input
                  type="radio"
                  name="hasAllergies"
                  [value]="false"
                  [checked]="form.hasKnownAllergies === false"
                  (change)="onAllergyToggle(false)"
                />
                <strong>No known drug / substance allergies (Verified with patient)</strong>
              </label>

              <label class="radio-label">
                <input
                  type="radio"
                  name="hasAllergies"
                  [value]="true"
                  [checked]="form.hasKnownAllergies === true"
                  (change)="onAllergyToggle(true)"
                />
                <strong>Patient has known drug, food, or chemical allergies</strong>
              </label>
            </div>

            <div class="form-group" *ngIf="form.hasKnownAllergies === true" style="margin-top: 0.75rem;">
              <label>Specify Known Allergies & Severity *</label>
              <textarea
                [(ngModel)]="form.knownAllergies"
                rows="2"
                class="form-control"
                placeholder="e.g. Severe rash with Penicillin; wheezing with NSAIDs (Aspirin); lactose intolerance"
              ></textarea>
            </div>
          </div>
        </div>
      </main>

      <!-- Bottom Action Bar -->
      <footer class="dossier-footer">
        <button class="btn btn-outline" (click)="goBack()">Cancel & Return</button>
        <button class="btn btn-primary" (click)="saveDossier()" [disabled]="saving()">
          {{ saving() ? 'Saving Changes...' : 'Save & Recalculate Completeness' }}
        </button>
      </footer>
    </div>
  `,
  styles: [`
    .dossier-container { max-width: 1160px; margin: 0 auto; padding: 1.5rem 2rem 3rem; }
    .dossier-header {
      background: white;
      border: 1px solid #e5e7eb;
      border-radius: 8px;
      padding: 1.25rem 1.5rem;
      margin-bottom: 1.25rem;
      box-shadow: 0 1px 3px rgba(0,0,0,0.05);
      position: sticky;
      top: 0;
      z-index: 100;
    }
    .header-top { display: flex; justify-content: space-between; align-items: center; margin-bottom: 1rem; }
    .header-left { display: flex; align-items: center; gap: 1rem; }
    .btn-back { background: none; border: 1px solid #d1d5db; border-radius: 6px; padding: 0.4rem 0.75rem; font-size: 0.825rem; cursor: pointer; color: #374151; font-weight: 500; }
    .btn-back:hover { background: #f3f4f6; }
    .patient-title-group { display: flex; flex-direction: column; }
    .patient-name { font-size: 1.4rem; font-weight: 700; color: #111827; margin: 0; }
    .patient-sub { font-size: 0.8rem; color: #6b7280; margin-top: 0.15rem; }
    .header-right { display: flex; align-items: center; gap: 1rem; }
    .status-badge {
      font-size: 0.8rem;
      font-weight: 600;
      padding: 0.35rem 0.75rem;
      border-radius: 9999px;
      display: inline-flex;
      align-items: center;
      gap: 0.35rem;
    }
    .status-badge.complete { background: #d1fae5; color: #065f46; border: 1px solid #a7f3d0; }
    .status-badge.incomplete { background: #fef3c7; color: #92400e; border: 1px solid #fde68a; }
    .btn { padding: 0.5rem 1.25rem; border-radius: 6px; font-weight: 600; font-size: 0.875rem; cursor: pointer; border: 1px solid transparent; }
    .btn-save { background: #059669; color: white; }
    .btn-save:hover { background: #047857; }
    .btn-primary { background: #10b981; color: white; }
    .btn-primary:hover { background: #059669; }
    .btn-outline { background: white; border-color: #d1d5db; color: #374151; }
    .btn:disabled { opacity: 0.6; cursor: not-allowed; }

    .progress-container { margin-bottom: 0.75rem; }
    .progress-bar-track { width: 100%; height: 8px; background: #e5e7eb; border-radius: 9999px; overflow: hidden; }
    .progress-bar-fill { height: 100%; background: #f59e0b; transition: width 0.3s ease; }
    .progress-bar-fill.complete { background: #10b981; }
    .progress-labels { display: flex; justify-content: space-between; font-size: 0.7rem; color: #9ca3af; margin-top: 0.25rem; }

    .warning-banner {
      background: #fef3c7;
      border: 1px solid #f59e0b;
      border-radius: 6px;
      padding: 0.75rem 1rem;
      display: flex;
      gap: 0.75rem;
      align-items: flex-start;
      margin-top: 0.75rem;
    }
    .warning-icon { font-size: 1.15rem; }
    .warning-content { font-size: 0.825rem; color: #92400e; }
    .missing-tags { display: flex; flex-wrap: wrap; gap: 0.4rem; margin-top: 0.35rem; }
    .missing-tag { background: #fde68a; color: #78350f; padding: 0.15rem 0.5rem; border-radius: 4px; font-weight: 600; font-size: 0.75rem; }

    .success-banner {
      background: #ecfdf5;
      border: 1px solid #10b981;
      color: #065f46;
      padding: 0.6rem 1rem;
      border-radius: 6px;
      font-size: 0.85rem;
      margin-top: 0.75rem;
      font-weight: 500;
    }

    .complaint-summary-card {
      background: #eff6ff;
      border: 1px solid #bfdbfe;
      border-radius: 6px;
      padding: 0.85rem 1.25rem;
      margin-bottom: 1.25rem;
    }
    .complaint-label { font-size: 0.75rem; font-weight: 600; color: #1e40af; text-transform: uppercase; letter-spacing: 0.05em; }
    .complaint-value { font-size: 0.95rem; color: #1e3a8a; font-weight: 500; margin-top: 0.25rem; }

    .dossier-tabs {
      display: flex;
      border-bottom: 2px solid #e5e7eb;
      gap: 0.5rem;
      margin-bottom: 1.5rem;
      overflow-x: auto;
    }
    .tab-btn {
      background: none;
      border: none;
      padding: 0.75rem 1rem;
      font-size: 0.875rem;
      font-weight: 500;
      color: #6b7280;
      cursor: pointer;
      position: relative;
      border-bottom: 2px solid transparent;
      margin-bottom: -2px;
      white-space: nowrap;
      display: flex;
      align-items: center;
      gap: 0.35rem;
    }
    .tab-btn:hover { color: #111827; }
    .tab-btn.active { color: #059669; border-bottom-color: #059669; font-weight: 600; }
    .tab-warn { color: #d97706; font-size: 1.2rem; font-weight: bold; line-height: 0; }

    .dossier-body {
      background: white;
      border: 1px solid #e5e7eb;
      border-radius: 8px;
      padding: 1.75rem;
      box-shadow: 0 1px 3px rgba(0,0,0,0.05);
      margin-bottom: 1.5rem;
    }
    .panel-heading { font-size: 1.2rem; font-weight: 700; color: #111827; margin: 0 0 0.25rem; }
    .panel-desc { font-size: 0.825rem; color: #6b7280; margin: 0 0 1.5rem; }

    .section-card {
      background: #fafafa;
      border: 1px solid #e5e7eb;
      border-radius: 6px;
      padding: 1.25rem;
      margin-bottom: 1.25rem;
    }
    .section-card-header { margin-bottom: 0.75rem; }
    .section-title { font-size: 1rem; font-weight: 700; color: #1f2937; margin: 0; }
    .section-hint { font-size: 0.75rem; color: #6b7280; display: block; margin-top: 0.15rem; }

    .form-group { margin-bottom: 1.25rem; }
    .form-group label { display: block; font-size: 0.85rem; font-weight: 600; margin-bottom: 0.35rem; color: #374151; }
    .form-control {
      width: 100%;
      padding: 0.55rem 0.8rem;
      border: 1px solid #d1d5db;
      border-radius: 6px;
      box-sizing: border-box;
      font-size: 0.9rem;
      font-family: inherit;
      background: white;
    }
    .form-control:focus { outline: none; border-color: #059669; box-shadow: 0 0 0 2px rgba(5,150,105,0.1); }
    .readonly-input { background: #f3f4f6; color: #4b5563; }
    .form-row { display: flex; gap: 1.25rem; }
    .form-group.half { flex: 1; margin-bottom: 0.75rem; }
    .form-grid-2 { display: grid; grid-template-columns: 1fr 1fr; gap: 1rem; }
    .full-col { grid-column: span 2; }
    .field-hint { display: block; font-size: 0.75rem; color: #6b7280; margin-top: 0.3rem; }

    .mandatory-badge {
      font-size: 0.65rem;
      color: #92400e;
      background: #fef3c7;
      padding: 0.1rem 0.35rem;
      border-radius: 4px;
      margin-left: 0.35rem;
      font-weight: 700;
      text-transform: uppercase;
    }

    .vitals-grid { display: grid; grid-template-columns: repeat(6, 1fr); gap: 0.75rem; }
    .vital-item label { display: block; font-size: 0.75rem; font-weight: 600; color: #4b5563; margin-bottom: 0.3rem; }

    .allergy-section {
      background: #fffbeb;
      border: 1px solid #fef3c7;
      border-left: 4px solid #f59e0b;
      padding: 1.25rem;
      border-radius: 6px;
      margin-top: 1.5rem;
    }
    .allergy-title { font-size: 0.95rem; font-weight: 700; color: #92400e; margin-bottom: 0.25rem; }
    .allergy-radio-group { display: flex; flex-direction: column; gap: 0.6rem; margin-top: 0.75rem; }
    .radio-label { display: flex; align-items: center; gap: 0.5rem; font-size: 0.85rem; color: #374151; cursor: pointer; }

    .dossier-footer {
      display: flex;
      justify-content: flex-end;
      gap: 1rem;
      padding-top: 0.5rem;
    }
  `]
})
export class CaseDossierComponent implements OnInit {
  api = inject(ApiService);
  route = inject(ActivatedRoute);
  router = inject(Router);

  patientId = '';
  patient = signal<Patient | null>(null);
  missingFields = signal<string[]>([]);
  saving = signal(false);
  saveSuccessMessage = signal<string | null>(null);

  activeTab: 'complaints' | 'physical' | 'mental' | 'history' | 'vitals' = 'complaints';

  form: UpdateHomeopathicCaseRecordRequest = {
    chiefComplaintTimeline: '',
    location: '',
    sensationAndPathology: '',
    aggravatingModalities: '',
    amelioratingModalities: '',
    ailmentsFrom: '',
    concomitantSymptoms: '',
    // Thermals
    thermals: '',
    thermalTolerance: '',
    // Functional
    appetite: '',
    thirst: '',
    sex: '',
    sleep: '',
    dreams: '',
    cravingsAversions: '',
    // Elimination
    stool: '',
    urine: '',
    sweat: '',
    breath: '',
    discharges: '',
    // Mental Generals
    willAndEmotions: '',
    understandingAndIntellect: '',
    memory: '',
    mindDisposition: '',
    temperament: '',
    // History
    pastMedicalSurgicalHistory: '',
    familyMedicalHistory: '',
    childhoodDiseasesVaccination: '',
    diet: '',
    familyStatus: '',
    placeOfBirth: '',
    religion: '',
    occupation: '',
    maritalStatus: '',
    address: '',
    // General Exam & Vitals
    consciousState: 'Conscious',
    generalAppearance: '',
    builtAndNutrition: '',
    systemicSigns: '',
    bloodPressure: '',
    pulseRate: '',
    respiratoryRate: '',
    weightKg: undefined,
    heightCm: undefined,
    bmi: undefined,
    systemicExamNotes: '',
    // Allergies
    knownAllergies: '',
    hasKnownAllergies: false,
    allergyAssessmentConfirmed: false
  };

  ngOnInit() {
    this.patientId = this.route.snapshot.paramMap.get('id') || '';
    if (this.patientId) {
      this.loadDossier();
    }
  }

  loadDossier() {
    this.api.getDetailedProfile(this.patientId).subscribe(res => {
      this.patient.set(res.patient);
      this.missingFields.set(res.missingRequiredFields || []);
      if (res.caseRecord) {
        this.populateForm(res.caseRecord);
      }
    });
  }

  populateForm(record: HomeopathicCaseRecord) {
    this.form = {
      chiefComplaintTimeline: record.chiefComplaintTimeline || '',
      location: record.location || '',
      sensationAndPathology: record.sensationAndPathology || '',
      aggravatingModalities: record.aggravatingModalities || '',
      amelioratingModalities: record.amelioratingModalities || '',
      ailmentsFrom: record.ailmentsFrom || '',
      concomitantSymptoms: record.concomitantSymptoms || '',
      // Thermals
      thermals: record.thermals || record.thermalTolerance || '',
      thermalTolerance: record.thermalTolerance || record.thermals || '',
      // Functional
      appetite: record.appetite || '',
      thirst: record.thirst || '',
      sex: record.sex || '',
      sleep: record.sleep || record.sleepPattern || '',
      dreams: record.dreams || '',
      cravingsAversions: record.cravingsAversions || '',
      // Elimination
      stool: record.stool || '',
      urine: record.urine || '',
      sweat: record.sweat || '',
      breath: record.breath || '',
      discharges: record.discharges || '',
      // Mental Generals
      willAndEmotions: record.willAndEmotions || '',
      understandingAndIntellect: record.understandingAndIntellect || record.intellectAndMemory || '',
      memory: record.memory || '',
      mindDisposition: record.mindDisposition || '',
      temperament: record.temperament || '',
      // History
      pastMedicalSurgicalHistory: record.pastMedicalSurgicalHistory || '',
      familyMedicalHistory: record.familyMedicalHistory || '',
      childhoodDiseasesVaccination: record.childhoodDiseasesVaccination || '',
      diet: record.diet || '',
      familyStatus: record.familyStatus || '',
      placeOfBirth: record.placeOfBirth || '',
      religion: record.religion || '',
      occupation: record.occupation || '',
      maritalStatus: record.maritalStatus || '',
      address: record.address || '',
      // General Exam & Vitals
      consciousState: record.consciousState || 'Conscious',
      generalAppearance: record.generalAppearance || '',
      builtAndNutrition: record.builtAndNutrition || record.buildAndAppearance || '',
      systemicSigns: record.systemicSigns || record.skinAndNails || '',
      bloodPressure: record.bloodPressure || '',
      pulseRate: record.pulseRate || '',
      respiratoryRate: record.respiratoryRate || '',
      weightKg: record.weightKg,
      heightCm: record.heightCm,
      bmi: record.bmi,
      systemicExamNotes: record.systemicExamNotes || '',
      // Allergies
      knownAllergies: record.knownAllergies || '',
      hasKnownAllergies: record.hasKnownAllergies ?? false,
      allergyAssessmentConfirmed: record.allergyAssessmentConfirmed ?? true
    };
    this.calculateBmi();
  }

  onHeightWeightChange() {
    this.calculateBmi();
  }

  calculateBmi() {
    if (this.form.heightCm && this.form.weightKg && this.form.heightCm > 0) {
      const heightInMeters = this.form.heightCm / 100;
      const computed = this.form.weightKg / (heightInMeters * heightInMeters);
      this.form.bmi = parseFloat(computed.toFixed(1));
    }
  }

  onAllergyToggle(hasAllergies: boolean) {
    this.form.hasKnownAllergies = hasAllergies;
    this.form.allergyAssessmentConfirmed = true;
    if (!hasAllergies) {
      this.form.knownAllergies = 'None (Verified)';
    } else if (this.form.knownAllergies === 'None (Verified)') {
      this.form.knownAllergies = '';
    }
  }

  saveDossier() {
    this.saving.set(true);
    this.saveSuccessMessage.set(null);

    // Sync thermals
    if (this.form.thermals) {
      this.form.thermalTolerance = this.form.thermals;
    }
    this.form.allergyAssessmentConfirmed = true;

    this.api.updateDetailedProfile(this.patientId, this.form).subscribe({
      next: (res) => {
        this.patient.set(res.patient);
        this.missingFields.set(res.missingRequiredFields || []);
        if (res.caseRecord) {
          this.populateForm(res.caseRecord);
        }
        this.saving.set(false);
        this.saveSuccessMessage.set(
          res.patient.completionPercentage === 100
            ? 'Case Record dossier successfully completed (100%)!'
            : `Case record updated. Profile completion is now ${res.patient.completionPercentage}%.`
        );
        setTimeout(() => this.saveSuccessMessage.set(null), 5000);
      },
      error: () => {
        this.saving.set(false);
      }
    });
  }

  goBack() {
    this.router.navigate(['/patients']);
  }

  getFriendlyFieldName(fieldName: string): string {
    const map: Record<string, string> = {
      ChiefComplaintTimeline: 'Chief Complaint Timeline / Onset',
      Thermals: 'Thermals',
      PhysicalGenerals_Thermal: 'Thermals',
      Functional_AppetiteThirst: 'Functional: Appetite & Thirst',
      PhysicalGenerals_AppetiteThirst: 'Functional: Appetite & Thirst',
      Functional_Sleep: 'Functional: Sleep & Rest',
      PhysicalGenerals_Sleep: 'Functional: Sleep & Rest',
      KnownAllergies: 'Allergy Assessment'
    };
    return map[fieldName] || fieldName;
  }

  isTabMissing(tab: string): boolean {
    const missing = this.missingFields();
    if (tab === 'complaints' && missing.includes('ChiefComplaintTimeline')) return true;
    if (
      tab === 'physical' &&
      (missing.includes('Thermals') ||
        missing.includes('PhysicalGenerals_Thermal') ||
        missing.includes('Functional_AppetiteThirst') ||
        missing.includes('PhysicalGenerals_AppetiteThirst') ||
        missing.includes('Functional_Sleep') ||
        missing.includes('PhysicalGenerals_Sleep'))
    ) {
      return true;
    }
    if (tab === 'vitals' && missing.includes('KnownAllergies')) return true;
    return false;
  }
}
