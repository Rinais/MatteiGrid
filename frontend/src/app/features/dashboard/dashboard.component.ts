import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { AuthService } from '../../core/auth.service';
import { ApiService } from '../../core/api.service';
import { Appointment, LaboratoryOrder, InventoryItem, Invoice } from '../../core/models';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule],
  template: `
    <div class="dashboard-container">
      <div class="dashboard-header">
        <div>
          <h1 class="dashboard-title">{{ auth.currentUser()?.role }} Dashboard</h1>
          <p class="dashboard-subtitle">
            {{ auth.currentUser()?.role === 'SuperAdmin' ? 'Platform-wide governance & tenant operations' : 'Operational summary for Mattei Grid clinical practice' }}
          </p>
        </div>
        <div class="action-buttons">
          <a routerLink="/consultation" class="btn btn-primary" *ngIf="auth.currentUser()?.role === 'Doctor'">
            + New Consultation
          </a>
          <a routerLink="/patients" class="btn btn-primary" *ngIf="auth.currentUser()?.role === 'Receptionist'">
            + Register Patient
          </a>
          <a routerLink="/lab" class="btn btn-primary" *ngIf="auth.currentUser()?.role === 'Lab Technician'">
            View Lab Queue
          </a>
          <a routerLink="/admin" class="btn btn-primary" *ngIf="auth.currentUser()?.role === 'Administrator' || auth.currentUser()?.role === 'SuperAdmin'">
            🔑 Manage Users & Passwords
          </a>
        </div>
      </div>

      <!-- Quick Metrics Grid: Scoped to Role Permissions -->
      <div class="metrics-grid">
        <!-- Clinical / Practice Metrics (Doctor, Receptionist, Administrator) -->
        <div class="metric-card" *ngIf="auth.hasPermission('Appointments.Read') && auth.currentUser()?.role !== 'SuperAdmin'">
          <div class="metric-label">Today's Appointments</div>
          <div class="metric-value">{{ appointments().length }}</div>
          <div class="metric-subtext">Confirmed schedule</div>
        </div>

        <div class="metric-card" *ngIf="auth.hasPermission('Laboratory.Read') && auth.currentUser()?.role !== 'SuperAdmin'">
          <div class="metric-label">Active Lab Orders</div>
          <div class="metric-value">{{ activeLabOrdersCount() }}</div>
          <div class="metric-subtext">In prep queue</div>
        </div>

        <div class="metric-card" *ngIf="auth.hasPermission('Billing.Read') && auth.currentUser()?.role !== 'SuperAdmin'">
          <div class="metric-label">Pending Invoices</div>
          <div class="metric-value">{{ pendingInvoicesCount() }}</div>
          <div class="metric-subtext">Unsettled accounts</div>
        </div>

        <div class="metric-card" [class.alert]="lowStockCount() > 0" *ngIf="auth.hasPermission('Inventory.Read') && auth.currentUser()?.role !== 'SuperAdmin'">
          <div class="metric-label">Low Stock Alerts</div>
          <div class="metric-value">{{ lowStockCount() }}</div>
          <div class="metric-subtext">{{ lowStockCount() > 0 ? 'Requires restock' : 'Adequate levels' }}</div>
        </div>

        <!-- Administrator Metric -->
        <div class="metric-card" *ngIf="auth.currentUser()?.role === 'Administrator'">
          <div class="metric-label">Clinic Staff & Users</div>
          <div class="metric-value">{{ totalUsersCount() }}</div>
          <div class="metric-subtext">Active accounts</div>
        </div>

        <!-- SuperAdmin Platform Governance Metrics (Zero Clinical PHI per DPDP Act 2023) -->
        <div class="metric-card" *ngIf="auth.currentUser()?.role === 'SuperAdmin'">
          <div class="metric-label">Connected Tenants</div>
          <div class="metric-value">2</div>
          <div class="metric-subtext">Tenant Model A isolation</div>
        </div>

        <div class="metric-card" *ngIf="auth.currentUser()?.role === 'SuperAdmin'">
          <div class="metric-label">Platform Accounts</div>
          <div class="metric-value">{{ totalUsersCount() }}</div>
          <div class="metric-subtext">Across all tenants</div>
        </div>

        <div class="metric-card" *ngIf="auth.currentUser()?.role === 'SuperAdmin'">
          <div class="metric-label">Security Audit Logs</div>
          <div class="metric-value">{{ auditLogCount() }}</div>
          <div class="metric-subtext">Recorded security events</div>
        </div>

        <div class="metric-card" [class.alert]="auth.isBreakGlass()" *ngIf="auth.currentUser()?.role === 'SuperAdmin'">
          <div class="metric-label">Break-Glass State</div>
          <div class="metric-value">{{ auth.isBreakGlass() ? 'ARMED' : 'NORMAL' }}</div>
          <div class="metric-subtext">{{ auth.isBreakGlass() ? 'Emergency clinical access' : 'Zero clinical PHI' }}</div>
        </div>
      </div>

      <!-- Section: Doctor View -->
      <div class="role-section" *ngIf="auth.currentUser()?.role === 'Doctor'">
        <div class="section-title">Today's Schedule & Consultations</div>
        <div class="card-table">
          <table>
            <thead>
              <tr>
                <th>Time</th>
                <th>Patient</th>
                <th>Status</th>
                <th>Action</th>
              </tr>
            </thead>
            <tbody>
              <tr *ngFor="let a of appointments()">
                <td>{{ a.startAt | date:'shortTime' }} - {{ a.endAt | date:'shortTime' }}</td>
                <td>Ramesh Kumar (ID: #{{ a.patientId.slice(0, 8) }})</td>
                <td><span class="status-badge scheduled">{{ a.status }}</span></td>
                <td>
                  <a routerLink="/consultation" class="table-action-link">Open Workspace →</a>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>

      <!-- Section: Lab Technician View -->
      <div class="role-section" *ngIf="auth.currentUser()?.role === 'Lab Technician'">
        <div class="section-title">Preparation Queue Live Status</div>
        <div class="queue-grid">
          <div class="queue-card" *ngFor="let order of labOrders()">
            <div class="queue-header">
              <span class="order-id">Order #{{ order.id.slice(0, 8) }}</span>
              <span class="status-badge" [ngClass]="order.status.toLowerCase()">{{ order.status }}</span>
            </div>
            <div class="order-details">
              <div>Prescription: #{{ order.prescriptionId.slice(0, 8) }}</div>
              <div class="text-muted">Received: {{ order.createdAt | date:'shortTime' }}</div>
            </div>
            <a routerLink="/lab" class="btn btn-sm btn-outline">Manage Order →</a>
          </div>
        </div>
      </div>

      <!-- Section: Receptionist View -->
      <div class="role-section" *ngIf="auth.currentUser()?.role === 'Receptionist'">
        <div class="section-title">Billing & Collections Follow-up</div>
        <div class="card-table">
          <table>
            <thead>
              <tr>
                <th>Invoice</th>
                <th>Amount</th>
                <th>Status</th>
                <th>Link Status</th>
                <th>Action</th>
              </tr>
            </thead>
            <tbody>
              <tr *ngFor="let inv of invoices()">
                <td>#{{ inv.id.slice(0, 8) }}</td>
                <td>₹{{ inv.totalAmount }}</td>
                <td><span class="status-badge" [ngClass]="inv.status.toLowerCase()">{{ inv.status }}</span></td>
                <td>{{ inv.activePaymentLink ? 'UPI Link Active (48h)' : 'None' }}</td>
                <td>
                  <a routerLink="/billing" class="table-action-link">Collect Payment →</a>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>

      <!-- Section: Administrator View -->
      <div class="role-section" *ngIf="auth.currentUser()?.role === 'Administrator'">
        <div class="section-title">Clinic Inventory & Operations Overview</div>
        <div class="admin-grid">
          <div class="admin-box">
            <h4>Inventory Status</h4>
            <p>Total items tracked: {{ inventory().length }}</p>
            <p class="text-warning">Items at or below reorder level: {{ lowStockCount() }}</p>
            <a routerLink="/inventory" class="btn btn-sm btn-outline">Restock Inventory</a>
          </div>
          <div class="admin-box">
            <h4>User Administration & Compliance</h4>
            <p>Total Clinic Staff: {{ totalUsersCount() }}</p>
            <p>DPDP Act 2023 Compliance: Active</p>
            <div style="display: flex; gap: 0.5rem; margin-top: 0.75rem;">
              <a routerLink="/patients" class="btn btn-sm btn-outline">Audit Records</a>
              <a routerLink="/admin" class="btn btn-sm btn-primary">Manage Users & Passwords</a>
            </div>
          </div>
        </div>
      </div>

      <!-- Section: SuperAdmin View (Platform & Governance) -->
      <div class="role-section" *ngIf="auth.currentUser()?.role === 'SuperAdmin'">
        <div class="section-title">Platform Operations & Security Oversight</div>
        <div class="admin-grid">
          <div class="admin-box">
            <h4>Multi-Tenant Infrastructure</h4>
            <p>Active Tenants: <strong>2 Connected Practices</strong></p>
            <p>Isolation Mode: <strong>Tenancy Model A (Query Filter & SaveInterceptor)</strong></p>
            <a routerLink="/admin" class="btn btn-sm btn-outline">Inspect Tenants & Break-Glass →</a>
          </div>
          <div class="admin-box">
            <h4>Credential & Access Governance</h4>
            <p>Total Managed Accounts: {{ totalUsersCount() }}</p>
            <p>Security Audit Records: {{ auditLogCount() }} recorded events</p>
            <a routerLink="/admin" class="btn btn-sm btn-primary">🔑 Open Password Reset Utility →</a>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .dashboard-container {
      padding: 1.5rem 2rem;
      max-width: 1200px;
      margin: 0 auto;
    }
    .dashboard-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 2rem;
    }
    .dashboard-title {
      font-size: 1.75rem;
      font-weight: 700;
      color: #111827;
      margin: 0;
    }
    .dashboard-subtitle {
      color: #6b7280;
      margin: 0.25rem 0 0;
      font-size: 0.9rem;
    }
    .btn {
      padding: 0.5rem 1rem;
      border-radius: 6px;
      font-weight: 500;
      font-size: 0.875rem;
      text-decoration: none;
      display: inline-flex;
      align-items: center;
      cursor: pointer;
      border: 1px solid transparent;
    }
    .btn-primary {
      background: #10b981;
      color: white;
    }
    .btn-primary:hover {
      background: #059669;
    }
    .btn-outline {
      background: white;
      border-color: #d1d5db;
      color: #374151;
    }
    .btn-outline:hover {
      background: #f3f4f6;
    }
    .btn-sm {
      padding: 0.35rem 0.75rem;
      font-size: 0.8rem;
    }
    .metrics-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
      gap: 1.25rem;
      margin-bottom: 2rem;
    }
    .metric-card {
      background: white;
      border: 1px solid #e5e7eb;
      border-radius: 8px;
      padding: 1.25rem;
      box-shadow: 0 1px 3px rgba(0,0,0,0.05);
    }
    .metric-card.alert {
      border-left: 4px solid #f59e0b;
    }
    .metric-label {
      font-size: 0.85rem;
      color: #6b7280;
      font-weight: 500;
    }
    .metric-value {
      font-size: 2rem;
      font-weight: 700;
      color: #111827;
      margin: 0.25rem 0;
    }
    .metric-subtext {
      font-size: 0.75rem;
      color: #9ca3af;
    }
    .role-section {
      background: white;
      border: 1px solid #e5e7eb;
      border-radius: 8px;
      padding: 1.5rem;
      box-shadow: 0 1px 3px rgba(0,0,0,0.05);
      margin-bottom: 1.5rem;
    }
    .section-title {
      font-size: 1.1rem;
      font-weight: 600;
      color: #111827;
      margin-bottom: 1rem;
    }
    .card-table table {
      width: 100%;
      border-collapse: collapse;
      font-size: 0.875rem;
    }
    .card-table th {
      text-align: left;
      padding: 0.75rem 1rem;
      background: #f9fafb;
      color: #4b5563;
      border-bottom: 1px solid #e5e7eb;
    }
    .card-table td {
      padding: 0.75rem 1rem;
      border-bottom: 1px solid #f3f4f6;
    }
    .status-badge {
      display: inline-block;
      padding: 0.2rem 0.5rem;
      border-radius: 9999px;
      font-size: 0.75rem;
      font-weight: 500;
    }
    .status-badge.scheduled { background: #dbeafe; color: #1e40af; }
    .status-badge.received { background: #e0e7ff; color: #3730a3; }
    .status-badge.preparing { background: #fef3c7; color: #92400e; }
    .status-badge.pending { background: #fef3c7; color: #92400e; }
    .status-badge.paid { background: #d1fae5; color: #065f46; }
    .table-action-link {
      color: #10b981;
      text-decoration: none;
      font-weight: 500;
    }
    .queue-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(280px, 1fr));
      gap: 1rem;
    }
    .queue-card {
      border: 1px solid #e5e7eb;
      border-radius: 6px;
      padding: 1rem;
      background: #fafafa;
    }
    .queue-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 0.5rem;
    }
    .order-id {
      font-weight: 600;
    }
    .order-details {
      font-size: 0.8rem;
      margin-bottom: 0.75rem;
    }
    .text-muted { color: #6b7280; }
    .text-warning { color: #d97706; font-weight: 600; }
    .admin-grid {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 1.5rem;
    }
    .admin-box {
      border: 1px solid #e5e7eb;
      border-radius: 6px;
      padding: 1.25rem;
      background: #f9fafb;
    }
  `]
})
export class DashboardComponent implements OnInit {
  auth = inject(AuthService);
  api = inject(ApiService);

  appointments = signal<Appointment[]>([]);
  labOrders = signal<LaboratoryOrder[]>([]);
  inventory = signal<InventoryItem[]>([]);
  invoices = signal<Invoice[]>([]);
  totalUsersCount = signal<number>(5);
  auditLogCount = signal<number>(3);

  ngOnInit() {
    this.api.getAppointments().subscribe(appts => this.appointments.set(appts));
    this.api.getLabQueue().subscribe(orders => this.labOrders.set(orders));
    this.api.getInventory().subscribe(inv => this.inventory.set(inv));
    this.api.getInvoices().subscribe(invs => this.invoices.set(invs));

    if (this.auth.hasPermission('Administration.ManageUsers')) {
      this.api.getUsers().subscribe(users => this.totalUsersCount.set(users.length));
      this.api.getAuditLogs().subscribe(logs => this.auditLogCount.set(logs.length));
    }
  }

  activeLabOrdersCount(): number {
    return this.labOrders().filter(o => o.status !== 'Collected').length;
  }

  pendingInvoicesCount(): number {
    return this.invoices().filter(i => i.status === 'Pending').length;
  }

  lowStockCount(): number {
    return this.inventory().filter(i => i.isLowStock).length;
  }
}
