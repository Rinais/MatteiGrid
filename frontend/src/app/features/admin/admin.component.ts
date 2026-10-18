import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { AuthService } from '../../core/auth.service';
import { ApiService } from '../../core/api.service';
import { User, AuditLog } from '../../core/models';

@Component({
  selector: 'app-admin',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  template: `
    <div class="admin-container">
      <!-- Admin Header -->
      <div class="admin-header">
        <div>
          <div class="header-badges">
            <span class="role-pill" [ngClass]="auth.currentUser()?.role?.toLowerCase()?.replace(' ', '-')">
              {{ auth.currentUser()?.role }} Scope
            </span>
            <span class="compliance-badge">DPDP Act 2023 Compliant</span>
          </div>
          <h1 class="admin-title">Administration & Security Management</h1>
          <p class="admin-subtitle">
            Manage user accounts, enforce security policies, reset credentials, and inspect immutable audit logs.
          </p>
        </div>

        <div class="header-actions">
          <button class="btn btn-outline" (click)="loadData()" [disabled]="isLoading()">
            <span aria-hidden="true">🔄</span> Refresh
          </button>
        </div>
      </div>

      <!-- Navigation Tabs -->
      <div class="tabs-nav">
        <button
          type="button"
          class="tab-btn"
          [class.active]="activeTab() === 'users'"
          (click)="activeTab.set('users')"
        >
          👥 User Directory & Passwords ({{ users().length }})
        </button>
        <button
          type="button"
          class="tab-btn"
          [class.active]="activeTab() === 'audit'"
          (click)="activeTab.set('audit')"
        >
          📜 Security Audit Logs ({{ auditLogs().length }})
        </button>
        <button
          type="button"
          class="tab-btn"
          *ngIf="auth.currentUser()?.role === 'SuperAdmin'"
          [class.active]="activeTab() === 'platform'"
          (click)="activeTab.set('platform')"
        >
          🌐 Platform Tenants & Break-Glass
        </button>
      </div>

      <!-- TAB 1: User Directory & Password Reset Utility -->
      <div class="tab-content" *ngIf="activeTab() === 'users'">
        <!-- Search & Filter Bar -->
        <div class="filter-bar">
          <div class="search-box">
            <span class="search-icon" aria-hidden="true">🔍</span>
            <input
              type="text"
              placeholder="Search user by name, email or role..."
              [(ngModel)]="searchQuery"
            />
          </div>
          <div class="role-filter">
            <select [(ngModel)]="roleFilter">
              <option value="All">All Roles</option>
              <option value="Doctor">Doctor</option>
              <option value="Receptionist">Receptionist</option>
              <option value="Lab Technician">Lab Technician</option>
              <option value="Administrator">Administrator</option>
              <option value="SuperAdmin">SuperAdmin</option>
            </select>
          </div>
        </div>

        <!-- Feedback Alert -->
        @if (feedbackMessage()) {
          <div class="feedback-banner" [class.success]="feedbackType() === 'success'" [class.error]="feedbackType() === 'error'">
            <span>{{ feedbackType() === 'success' ? '✅' : '⚠️' }} {{ feedbackMessage() }}</span>
            <button type="button" class="dismiss-btn" (click)="feedbackMessage.set(null)">×</button>
          </div>
        }

        <!-- Users Table -->
        <div class="card-table">
          <table>
            <thead>
              <tr>
                <th>User Details</th>
                <th>Role</th>
                <th>Clinic / Tenant</th>
                <th>Status</th>
                <th>Security Actions</th>
              </tr>
            </thead>
            <tbody>
              <tr *ngFor="let user of filteredUsers()">
                <td>
                  <div class="user-meta">
                    <span class="user-avatar-circle">{{ user.fullName.charAt(0) }}</span>
                    <div>
                      <div class="user-fullname">{{ user.fullName }}</div>
                      <div class="user-email-text">{{ user.email }}</div>
                    </div>
                  </div>
                </td>
                <td>
                  <span class="badge" [ngClass]="user.role.toLowerCase().replace(' ', '-')">
                    {{ user.role }}
                  </span>
                </td>
                <td>
                  <code class="clinic-id-pill" [title]="user.clinicId || 'Platform-wide'">
                    {{ user.clinicId ? (user.clinicId.slice(0, 8) + '...') : 'Platform Multi-Tenant' }}
                  </code>
                </td>
                <td>
                  <span class="status-active">
                    <span class="status-dot"></span> Active
                  </span>
                </td>
                <td>
                  <button
                    type="button"
                    class="btn btn-sm btn-primary"
                    (click)="openResetPasswordModal(user)"
                  >
                    🔑 Reset Password
                  </button>
                </td>
              </tr>
              <tr *ngIf="filteredUsers().length === 0">
                <td colspan="5" class="empty-state">No users match your search criteria.</td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>

      <!-- TAB 2: Security Audit Logs -->
      <div class="tab-content" *ngIf="activeTab() === 'audit'">
        <div class="card-table">
          <table>
            <thead>
              <tr>
                <th>Timestamp</th>
                <th>Event Type</th>
                <th>Action</th>
                <th>Entity</th>
                <th>Actor</th>
                <th>Details</th>
              </tr>
            </thead>
            <tbody>
              <tr *ngFor="let log of auditLogs()">
                <td class="timestamp-cell">{{ log.createdAt | date:'medium' }}</td>
                <td>
                  <span class="log-badge">{{ log.eventType }}</span>
                </td>
                <td><strong>{{ log.action }}</strong></td>
                <td>{{ log.entityName || '-' }}</td>
                <td class="actor-cell">{{ log.userEmail || log.userId || 'System' }}</td>
                <td class="details-cell" [title]="log.details || ''">{{ log.details || '-' }}</td>
              </tr>
              <tr *ngIf="auditLogs().length === 0">
                <td colspan="6" class="empty-state">No audit logs recorded yet.</td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>

      <!-- TAB 3: Platform Tenants & Break-Glass (SuperAdmin only) -->
      <div class="tab-content" *ngIf="activeTab() === 'platform' && auth.currentUser()?.role === 'SuperAdmin'">
        <div class="platform-grid">
          <!-- Tenants Card -->
          <div class="info-card">
            <h3>Registered Practice Tenants</h3>
            <p class="card-subtext">Tenant Model A: Strict Per-Clinic Physical & Logical Isolation</p>
            <div class="tenant-list">
              <div class="tenant-item">
                <div class="tenant-info">
                  <strong>Clinic Alpha (Primary Central Practice)</strong>
                  <code>00000000-0000-0000-0000-000000000001</code>
                </div>
                <span class="status-badge active">Active Tenant</span>
              </div>
              <div class="tenant-item">
                <div class="tenant-info">
                  <strong>Clinic Beta (Branch Clinic)</strong>
                  <code>00000000-0000-0000-0000-000000000002</code>
                </div>
                <span class="status-badge active">Active Tenant</span>
              </div>
            </div>
          </div>

          <!-- Break Glass Emergency Session Card -->
          <div class="info-card" [class.alert-card]="auth.isBreakGlass()">
            <div class="card-header-row">
              <h3>DPDP Act 2023 Break-Glass Protocol</h3>
              <span class="break-glass-status" [class.armed]="auth.isBreakGlass()">
                {{ auth.isBreakGlass() ? '🔴 BREAK-GLASS ACTIVE' : '🟢 NORMAL (ZERO PHI ACCESS)' }}
              </span>
            </div>
            <p class="card-subtext">
              SuperAdmins have zero access to clinical PHI by default. In life-critical emergencies or technical lockouts, an audited break-glass session may be declared.
            </p>

            @if (auth.isBreakGlass()) {
              <div class="break-glass-active-banner">
                <p><strong>Warning:</strong> Emergency break-glass session is currently active. Every view and export of patient data is logged to the immutable audit trail.</p>
                <button type="button" class="btn btn-outline" (click)="revokeBreakGlass()">
                  Revoke Break-Glass Session
                </button>
              </div>
            } @else {
              <form class="break-glass-form" (ngSubmit)="submitBreakGlass()">
                <div class="form-group">
                  <label for="bg-reason">Incident Justification (min. 20 chars)</label>
                  <input
                    id="bg-reason"
                    type="text"
                    [(ngModel)]="breakGlassReason"
                    name="bgReason"
                    placeholder="e.g. Incident INC-4921: Prescription system deadlock during consultation"
                    required
                  />
                </div>
                <div class="form-group">
                  <label for="bg-duration">Session Duration</label>
                  <select id="bg-duration" [(ngModel)]="breakGlassDuration" name="bgDuration">
                    <option [value]="15">15 Minutes</option>
                    <option [value]="30">30 Minutes (Recommended)</option>
                    <option [value]="60">60 Minutes</option>
                    <option [value]="120">120 Minutes (Max)</option>
                  </select>
                </div>
                <button
                  type="submit"
                  class="btn btn-danger"
                  [disabled]="isSubmitting() || breakGlassReason.length < 20"
                >
                  ⚡ Declare Break-Glass Session
                </button>
              </form>
            }
          </div>
        </div>
      </div>

      <!-- PASSWORD RESET MODAL -->
      <div class="modal-backdrop" *ngIf="selectedUserForReset()" (click)="closeResetPasswordModal()">
        <div class="modal-dialog" (click)="$event.stopPropagation()">
          <div class="modal-header">
            <div>
              <h2 class="modal-title">Admin Password Reset</h2>
              <p class="modal-subtitle">Direct credential reset with instant revocation of old sessions</p>
            </div>
            <button type="button" class="close-modal-btn" (click)="closeResetPasswordModal()">×</button>
          </div>

          <div class="modal-body">
            <!-- Target User Summary Card -->
            <div class="target-user-card">
              <span class="user-avatar-circle lg">{{ selectedUserForReset()?.fullName?.charAt(0) }}</span>
              <div>
                <div class="target-name">{{ selectedUserForReset()?.fullName }}</div>
                <div class="target-email">{{ selectedUserForReset()?.email }}</div>
                <div class="target-role">
                  Role: <span class="badge" [ngClass]="selectedUserForReset()?.role?.toLowerCase()?.replace(' ', '-')">{{ selectedUserForReset()?.role }}</span>
                </div>
              </div>
            </div>

            <!-- Reset Password Form -->
            <div class="form-group">
              <div class="label-row">
                <label for="new-password-input">New Password</label>
                <button type="button" class="btn-text-action" (click)="generateStrongPassword()">
                  🎲 Generate Strong Password
                </button>
              </div>
              <div class="password-input-group">
                <input
                  id="new-password-input"
                  [type]="showPassword() ? 'text' : 'password'"
                  [(ngModel)]="newPassword"
                  placeholder="Enter strong password (min 8 chars)"
                />
                <button type="button" class="toggle-password-btn" (click)="showPassword.set(!showPassword())">
                  {{ showPassword() ? '👁️ Hide' : '👁️ Show' }}
                </button>
              </div>
            </div>

            <!-- Password Policy Checklist -->
            <div class="policy-checklist">
              <div class="policy-item" [class.valid]="newPassword.length >= 8">
                <span class="check-icon">{{ newPassword.length >= 8 ? '✓' : '○' }}</span> At least 8 characters
              </div>
              <div class="policy-item" [class.valid]="hasUpperCase(newPassword)">
                <span class="check-icon">{{ hasUpperCase(newPassword) ? '✓' : '○' }}</span> At least 1 uppercase letter (A-Z)
              </div>
              <div class="policy-item" [class.valid]="hasLowerCase(newPassword)">
                <span class="check-icon">{{ hasLowerCase(newPassword) ? '✓' : '○' }}</span> At least 1 lowercase letter (a-z)
              </div>
              <div class="policy-item" [class.valid]="hasDigit(newPassword)">
                <span class="check-icon">{{ hasDigit(newPassword) ? '✓' : '○' }}</span> At least 1 number (0-9)
              </div>
              <div class="policy-item" [class.valid]="hasSpecialChar(newPassword)">
                <span class="check-icon">{{ hasSpecialChar(newPassword) ? '✓' : '○' }}</span> At least 1 special symbol (!&#64;#$%^&*)
              </div>
            </div>

            <div class="security-note">
              ℹ️ Resetting this password will immediately invalidate existing refresh tokens, clear lockout counters, and stamp a new security token.
            </div>
          </div>

          <div class="modal-footer">
            <button type="button" class="btn btn-outline" (click)="closeResetPasswordModal()" [disabled]="isSubmitting()">
              Cancel
            </button>
            <button
              type="button"
              class="btn btn-primary"
              (click)="executePasswordReset()"
              [disabled]="isSubmitting() || !isPasswordValid(newPassword)"
            >
              {{ isSubmitting() ? 'Resetting Password...' : 'Save & Enforce New Password' }}
            </button>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .admin-container {
      padding: 1.5rem 2rem;
      max-width: 1200px;
      margin: 0 auto;
    }
    .admin-header {
      display: flex;
      justify-content: space-between;
      align-items: flex-start;
      margin-bottom: 1.5rem;
    }
    .header-badges {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      margin-bottom: 0.5rem;
    }
    .role-pill {
      font-size: 0.75rem;
      font-weight: 700;
      padding: 0.2rem 0.6rem;
      border-radius: 9999px;
      text-transform: uppercase;
      letter-spacing: 0.05em;
    }
    .role-pill.administrator {
      background: #e0e7ff;
      color: #3730a3;
    }
    .role-pill.superadmin {
      background: #fef3c7;
      color: #92400e;
    }
    .compliance-badge {
      font-size: 0.75rem;
      background: #dcfce7;
      color: #166534;
      padding: 0.2rem 0.6rem;
      border-radius: 9999px;
      font-weight: 600;
    }
    .admin-title {
      font-size: 1.75rem;
      font-weight: 700;
      color: #111827;
      margin: 0;
    }
    .admin-subtitle {
      color: #6b7280;
      margin: 0.35rem 0 0;
      font-size: 0.95rem;
    }
    .tabs-nav {
      display: flex;
      gap: 0.5rem;
      border-bottom: 2px solid #e5e7eb;
      margin-bottom: 1.5rem;
    }
    .tab-btn {
      padding: 0.75rem 1.25rem;
      background: none;
      border: none;
      border-bottom: 3px solid transparent;
      margin-bottom: -2px;
      font-size: 0.95rem;
      font-weight: 600;
      color: #6b7280;
      cursor: pointer;
      transition: all 0.15s;
    }
    .tab-btn:hover {
      color: #111827;
    }
    .tab-btn.active {
      color: #10b981;
      border-bottom-color: #10b981;
    }
    .filter-bar {
      display: flex;
      justify-content: space-between;
      gap: 1rem;
      margin-bottom: 1rem;
    }
    .search-box {
      flex: 1;
      display: flex;
      align-items: center;
      background: white;
      border: 1px solid #d1d5db;
      border-radius: 6px;
      padding: 0.4rem 0.75rem;
      gap: 0.5rem;
    }
    .search-box input {
      border: none;
      outline: none;
      width: 100%;
      font-size: 0.9rem;
    }
    .role-filter select {
      padding: 0.45rem 0.75rem;
      border: 1px solid #d1d5db;
      border-radius: 6px;
      background: white;
      font-size: 0.9rem;
      color: #374151;
    }
    .feedback-banner {
      padding: 0.75rem 1rem;
      border-radius: 6px;
      margin-bottom: 1rem;
      display: flex;
      justify-content: space-between;
      align-items: center;
      font-size: 0.9rem;
      font-weight: 500;
    }
    .feedback-banner.success {
      background: #ecfdf5;
      color: #065f46;
      border: 1px solid #a7f3d0;
    }
    .feedback-banner.error {
      background: #fef2f2;
      color: #991b1b;
      border: 1px solid #fecaca;
    }
    .dismiss-btn {
      background: none;
      border: none;
      font-size: 1.25rem;
      cursor: pointer;
      color: inherit;
    }
    .card-table {
      background: white;
      border: 1px solid #e5e7eb;
      border-radius: 8px;
      overflow: hidden;
      box-shadow: 0 1px 3px rgba(0,0,0,0.05);
    }
    table {
      width: 100%;
      border-collapse: collapse;
      text-align: left;
    }
    th, td {
      padding: 0.85rem 1.25rem;
      border-bottom: 1px solid #f3f4f6;
      font-size: 0.875rem;
    }
    th {
      background: #f9fafb;
      font-weight: 600;
      color: #374151;
    }
    .user-meta {
      display: flex;
      align-items: center;
      gap: 0.75rem;
    }
    .user-avatar-circle {
      width: 34px;
      height: 34px;
      border-radius: 50%;
      background: #10b981;
      color: white;
      display: flex;
      align-items: center;
      justify-content: center;
      font-weight: 700;
      font-size: 0.85rem;
    }
    .user-avatar-circle.lg {
      width: 48px;
      height: 48px;
      font-size: 1.25rem;
    }
    .user-fullname {
      font-weight: 600;
      color: #111827;
    }
    .user-email-text {
      color: #6b7280;
      font-size: 0.8rem;
    }
    .badge {
      display: inline-block;
      padding: 0.2rem 0.55rem;
      border-radius: 4px;
      font-size: 0.75rem;
      font-weight: 600;
    }
    .badge.doctor { background: #e0f2fe; color: #0369a1; }
    .badge.receptionist { background: #fef3c7; color: #92400e; }
    .badge.lab-technician { background: #f3e8ff; color: #6b21a8; }
    .badge.administrator { background: #e0e7ff; color: #3730a3; }
    .badge.superadmin { background: #fee2e2; color: #991b1b; }
    .clinic-id-pill {
      font-size: 0.75rem;
      background: #f3f4f6;
      padding: 0.15rem 0.4rem;
      border-radius: 4px;
      color: #4b5563;
    }
    .status-active {
      display: inline-flex;
      align-items: center;
      gap: 0.35rem;
      font-size: 0.8rem;
      color: #059669;
      font-weight: 500;
    }
    .status-dot {
      width: 8px;
      height: 8px;
      border-radius: 50%;
      background: #10b981;
    }
    .empty-state {
      text-align: center;
      color: #9ca3af;
      padding: 2rem;
    }
    .timestamp-cell {
      white-space: nowrap;
      font-size: 0.8rem;
      color: #6b7280;
    }
    .log-badge {
      background: #e5e7eb;
      color: #374151;
      padding: 0.15rem 0.45rem;
      border-radius: 4px;
      font-size: 0.75rem;
      font-family: monospace;
    }
    .actor-cell {
      font-weight: 500;
      color: #111827;
    }
    .details-cell {
      max-width: 320px;
      overflow: hidden;
      text-overflow: ellipsis;
      white-space: nowrap;
      color: #4b5563;
    }
    .btn {
      padding: 0.5rem 1rem;
      border-radius: 6px;
      font-weight: 600;
      font-size: 0.875rem;
      cursor: pointer;
      display: inline-flex;
      align-items: center;
      gap: 0.4rem;
      border: 1px solid transparent;
      transition: all 0.15s;
    }
    .btn:disabled {
      opacity: 0.5;
      cursor: not-allowed;
    }
    .btn-sm {
      padding: 0.3rem 0.65rem;
      font-size: 0.8rem;
    }
    .btn-primary {
      background: #10b981;
      color: white;
    }
    .btn-primary:hover:not(:disabled) {
      background: #059669;
    }
    .btn-outline {
      background: white;
      border-color: #d1d5db;
      color: #374151;
    }
    .btn-outline:hover:not(:disabled) {
      background: #f3f4f6;
    }
    .btn-danger {
      background: #dc2626;
      color: white;
    }
    .btn-danger:hover:not(:disabled) {
      background: #b91c1c;
    }
    .btn-text-action {
      background: none;
      border: none;
      color: #2563eb;
      font-size: 0.8rem;
      font-weight: 600;
      cursor: pointer;
      padding: 0;
    }
    .btn-text-action:hover {
      text-decoration: underline;
    }

    /* Platform Grid */
    .platform-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(360px, 1fr));
      gap: 1.5rem;
    }
    .info-card {
      background: white;
      border: 1px solid #e5e7eb;
      border-radius: 8px;
      padding: 1.5rem;
      box-shadow: 0 1px 3px rgba(0,0,0,0.05);
    }
    .info-card.alert-card {
      border: 2px solid #ef4444;
      background: #fff5f5;
    }
    .card-header-row {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 0.25rem;
    }
    .card-subtext {
      color: #6b7280;
      font-size: 0.85rem;
      margin: 0.25rem 0 1rem;
    }
    .tenant-list {
      display: flex;
      flex-direction: column;
      gap: 0.75rem;
    }
    .tenant-item {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: 0.75rem;
      background: #f9fafb;
      border-radius: 6px;
      border: 1px solid #e5e7eb;
    }
    .tenant-info {
      display: flex;
      flex-direction: column;
      gap: 0.15rem;
    }
    .tenant-info code {
      font-size: 0.75rem;
      color: #6b7280;
    }
    .status-badge.active {
      font-size: 0.75rem;
      background: #dcfce7;
      color: #166534;
      padding: 0.2rem 0.5rem;
      border-radius: 4px;
      font-weight: 600;
    }
    .break-glass-status {
      font-size: 0.75rem;
      font-weight: 700;
      padding: 0.25rem 0.5rem;
      border-radius: 4px;
      background: #f3f4f6;
      color: #374151;
    }
    .break-glass-status.armed {
      background: #fee2e2;
      color: #991b1b;
      animation: pulse 2s infinite;
    }
    @keyframes pulse {
      0%, 100% { opacity: 1; }
      50% { opacity: 0.6; }
    }
    .break-glass-active-banner {
      background: #fee2e2;
      color: #7f1d1d;
      padding: 1rem;
      border-radius: 6px;
      border: 1px solid #fca5a5;
      margin-top: 1rem;
      font-size: 0.9rem;
      display: flex;
      flex-direction: column;
      gap: 0.75rem;
    }
    .break-glass-form {
      display: flex;
      flex-direction: column;
      gap: 1rem;
    }
    .form-group {
      display: flex;
      flex-direction: column;
      gap: 0.35rem;
    }
    .form-group label {
      font-size: 0.85rem;
      font-weight: 600;
      color: #374151;
    }
    .form-group input, .form-group select {
      padding: 0.5rem 0.75rem;
      border: 1px solid #d1d5db;
      border-radius: 6px;
      font-size: 0.9rem;
    }
    .label-row {
      display: flex;
      justify-content: space-between;
      align-items: center;
    }

    /* Modal Backdrop */
    .modal-backdrop {
      position: fixed;
      top: 0;
      left: 0;
      width: 100vw;
      height: 100vh;
      background: rgba(17, 24, 39, 0.65);
      backdrop-filter: blur(4px);
      display: flex;
      align-items: center;
      justify-content: center;
      z-index: 999;
      padding: 1rem;
    }
    .modal-dialog {
      background: white;
      width: 100%;
      max-width: 520px;
      border-radius: 12px;
      box-shadow: 0 20px 25px -5px rgba(0, 0, 0, 0.1), 0 10px 10px -5px rgba(0, 0, 0, 0.04);
      overflow: hidden;
      animation: modalSlideIn 0.2s ease-out;
    }
    @keyframes modalSlideIn {
      from { transform: translateY(-12px); opacity: 0; }
      to { transform: translateY(0); opacity: 1; }
    }
    .modal-header {
      padding: 1.25rem 1.5rem;
      border-bottom: 1px solid #e5e7eb;
      display: flex;
      justify-content: space-between;
      align-items: flex-start;
      background: #f9fafb;
    }
    .modal-title {
      font-size: 1.25rem;
      font-weight: 700;
      color: #111827;
      margin: 0;
    }
    .modal-subtitle {
      font-size: 0.8rem;
      color: #6b7280;
      margin: 0.25rem 0 0;
    }
    .close-modal-btn {
      background: none;
      border: none;
      font-size: 1.5rem;
      line-height: 1;
      color: #9ca3af;
      cursor: pointer;
    }
    .close-modal-btn:hover {
      color: #111827;
    }
    .modal-body {
      padding: 1.5rem;
      display: flex;
      flex-direction: column;
      gap: 1.25rem;
    }
    .target-user-card {
      display: flex;
      align-items: center;
      gap: 1rem;
      background: #f8fafc;
      padding: 1rem;
      border-radius: 8px;
      border: 1px solid #e2e8f0;
    }
    .target-name {
      font-weight: 700;
      color: #0f172a;
      font-size: 1rem;
    }
    .target-email {
      font-size: 0.85rem;
      color: #64748b;
    }
    .target-role {
      font-size: 0.8rem;
      color: #334155;
      margin-top: 0.2rem;
    }
    .password-input-group {
      display: flex;
      gap: 0.5rem;
    }
    .password-input-group input {
      flex: 1;
      padding: 0.5rem 0.75rem;
      border: 1px solid #d1d5db;
      border-radius: 6px;
      font-size: 0.95rem;
      font-family: monospace;
    }
    .toggle-password-btn {
      background: #f3f4f6;
      border: 1px solid #d1d5db;
      border-radius: 6px;
      padding: 0.4rem 0.75rem;
      font-size: 0.8rem;
      font-weight: 500;
      cursor: pointer;
      white-space: nowrap;
    }
    .policy-checklist {
      background: #f9fafb;
      padding: 0.75rem 1rem;
      border-radius: 6px;
      border: 1px solid #e5e7eb;
      display: flex;
      flex-direction: column;
      gap: 0.35rem;
    }
    .policy-item {
      font-size: 0.8rem;
      color: #6b7280;
      display: flex;
      align-items: center;
      gap: 0.4rem;
    }
    .policy-item.valid {
      color: #059669;
      font-weight: 600;
    }
    .check-icon {
      font-size: 0.85rem;
      width: 14px;
    }
    .security-note {
      font-size: 0.75rem;
      color: #475569;
      background: #eff6ff;
      padding: 0.5rem 0.75rem;
      border-radius: 6px;
      border-left: 3px solid #3b82f6;
    }
    .modal-footer {
      padding: 1rem 1.5rem;
      background: #f9fafb;
      border-top: 1px solid #e5e7eb;
      display: flex;
      justify-content: flex-end;
      gap: 0.75rem;
    }
  `]
})
export class AdminComponent implements OnInit {
  auth = inject(AuthService);
  private api = inject(ApiService);

  activeTab = signal<'users' | 'audit' | 'platform'>('users');
  users = signal<User[]>([]);
  auditLogs = signal<AuditLog[]>([]);
  isLoading = signal<boolean>(false);
  isSubmitting = signal<boolean>(false);

  searchQuery = '';
  roleFilter = 'All';

  // Modal State
  selectedUserForReset = signal<User | null>(null);
  newPassword = '';
  showPassword = signal<boolean>(false);
  feedbackMessage = signal<string | null>(null);
  feedbackType = signal<'success' | 'error'>('success');

  // Break Glass Form State
  breakGlassReason = '';
  breakGlassDuration = 30;

  filteredUsers = computed(() => {
    let list = this.users();
    if (this.roleFilter !== 'All') {
      list = list.filter(u => u.role === this.roleFilter);
    }
    if (this.searchQuery.trim()) {
      const q = this.searchQuery.toLowerCase();
      list = list.filter(u =>
        u.fullName.toLowerCase().includes(q) ||
        u.email.toLowerCase().includes(q) ||
        u.role.toLowerCase().includes(q)
      );
    }
    return list;
  });

  ngOnInit() {
    this.loadData();
  }

  loadData() {
    this.isLoading.set(true);
    this.api.getUsers().subscribe({
      next: (data) => {
        this.users.set(data);
        this.isLoading.set(false);
      },
      error: () => this.isLoading.set(false)
    });

    this.api.getAuditLogs().subscribe({
      next: (logs) => this.auditLogs.set(logs),
      error: () => {}
    });
  }

  openResetPasswordModal(user: User) {
    this.selectedUserForReset.set(user);
    this.newPassword = '';
    this.showPassword.set(false);
    this.feedbackMessage.set(null);
  }

  closeResetPasswordModal() {
    this.selectedUserForReset.set(null);
    this.newPassword = '';
  }

  generateStrongPassword() {
    const chars = 'ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789!@#$%^&*';
    let pwd = 'MG' + Math.floor(100 + Math.random() * 900);
    const specials = '!@#$%^&*';
    pwd += specials[Math.floor(Math.random() * specials.length)];
    const uppers = 'ABCDEFGHJKLMNPQRSTUVWXYZ';
    pwd += uppers[Math.floor(Math.random() * uppers.length)];
    const lowers = 'abcdefghijkmnopqrstuvwxyz';
    pwd += lowers[Math.floor(Math.random() * lowers.length)] + '2026!';
    this.newPassword = pwd;
    this.showPassword.set(true);
  }

  executePasswordReset() {
    const user = this.selectedUserForReset();
    if (!user || !this.isPasswordValid(this.newPassword)) return;

    this.isSubmitting.set(true);
    this.api.adminResetPassword(user.id, this.newPassword).subscribe({
      next: (res) => {
        this.isSubmitting.set(false);
        this.feedbackType.set('success');
        this.feedbackMessage.set(res.message || `Password for ${user.email} reset successfully.`);
        this.closeResetPasswordModal();
        this.loadData();
      },
      error: (err) => {
        this.isSubmitting.set(false);
        this.feedbackType.set('error');
        this.feedbackMessage.set(err?.message || 'Password reset request failed.');
      }
    });
  }

  submitBreakGlass() {
    if (this.breakGlassReason.trim().length < 20) return;
    this.isSubmitting.set(true);
    this.api.initiateBreakGlass(this.breakGlassReason, this.breakGlassDuration).subscribe({
      next: () => {
        this.isSubmitting.set(false);
        this.auth.isBreakGlass.set(true);
        this.feedbackType.set('success');
        this.feedbackMessage.set('Break-glass emergency session initiated successfully. Clinical access authorized.');
        this.loadData();
      },
      error: () => {
        this.isSubmitting.set(false);
        this.feedbackType.set('error');
        this.feedbackMessage.set('Failed to initiate break-glass emergency session.');
      }
    });
  }

  revokeBreakGlass() {
    this.auth.isBreakGlass.set(false);
    this.feedbackType.set('success');
    this.feedbackMessage.set('Break-glass session revoked. Returned to zero clinical PHI access mode.');
  }

  // Password Validation Helpers
  hasUpperCase(str: string): boolean { return /[A-Z]/.test(str); }
  hasLowerCase(str: string): boolean { return /[a-z]/.test(str); }
  hasDigit(str: string): boolean { return /[0-9]/.test(str); }
  hasSpecialChar(str: string): boolean { return /[^a-zA-Z0-9]/.test(str); }
  isPasswordValid(str: string): boolean {
    return str.length >= 8 &&
      this.hasUpperCase(str) &&
      this.hasLowerCase(str) &&
      this.hasDigit(str) &&
      this.hasSpecialChar(str);
  }
}
