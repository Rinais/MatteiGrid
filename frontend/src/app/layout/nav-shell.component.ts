import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { AuthService } from '../core/auth.service';
import { SignalRService } from '../core/signalr.service';

@Component({
  selector: 'app-nav-shell',
  standalone: true,
  imports: [CommonModule, RouterModule],
  template: `
    <header class="app-header" role="banner">
      <div class="header-left">
        <div class="brand">
          <span class="brand-icon" aria-hidden="true">🌿</span>
          <span class="brand-title">Mattei Grid</span>
          <span class="version-badge">v2.4</span>
        </div>
        <div class="tenant-badge" title="Tenant Model A: Strict Per-Clinic Isolation">
          <span class="tenant-dot" aria-hidden="true"></span>
          <span>Clinic Alpha (B2B Tenant)</span>
        </div>
      </div>

      <div class="header-right">
        <div class="signalr-status" [class.connected]="signalR.isConnected()" title="Real-time WebSocket Sync">
          <span class="pulse-indicator" aria-hidden="true"></span>
          <span>{{ signalR.isConnected() ? 'Real-Time Sync Active' : 'Offline / Standalone' }}</span>
        </div>

        <div class="role-selector">
          <label for="role-select" class="sr-only">Switch User Role</label>
          <span class="role-label" aria-hidden="true">Simulate Role:</span>
          <select id="role-select" [value]="auth.currentUser()?.role" (change)="onRoleChange($event)">
            <option value="Doctor">Doctor (Clinical + Rx)</option>
            <option value="Receptionist">Receptionist (Bookings + Billing)</option>
            <option value="Lab Technician">Lab Technician (Queue)</option>
            <option value="Administrator">Administrator (Audit + Admin)</option>
          </select>
        </div>

        <div class="user-pill">
          <span class="user-avatar" aria-hidden="true">{{ auth.currentUser()?.fullName?.charAt(0) || 'U' }}</span>
          <span class="user-name">{{ auth.currentUser()?.fullName }}</span>
        </div>
      </div>
    </header>

    @if (signalR.latestLabEvent()) {
      <div class="event-banner" role="status" aria-live="polite">
        <span class="event-icon" aria-hidden="true">🔔</span>
        <span>{{ signalR.latestLabEvent() }}</span>
        <button class="event-dismiss" (click)="signalR.latestLabEvent.set(null)" aria-label="Dismiss notification">×</button>
      </div>
    }

    <nav class="app-nav" role="navigation" aria-label="Main Navigation">
      <a routerLink="/dashboard" routerLinkActive="active" class="nav-link">
        <span class="nav-icon" aria-hidden="true">📊</span> Dashboard
      </a>
      <a routerLink="/patients" routerLinkActive="active" class="nav-link">
        <span class="nav-icon" aria-hidden="true">👥</span> Patient Registry
      </a>
      <a routerLink="/consultation" routerLinkActive="active" class="nav-link">
        <span class="nav-icon" aria-hidden="true">🔬</span> Consultation & Rx Grid
      </a>
      <a routerLink="/lab" routerLinkActive="active" class="nav-link">
        <span class="nav-icon" aria-hidden="true">🧪</span> Laboratory Queue
      </a>
      <a routerLink="/inventory" routerLinkActive="active" class="nav-link">
        <span class="nav-icon" aria-hidden="true">📦</span> Inventory Ledger
      </a>
      <a routerLink="/billing" routerLinkActive="active" class="nav-link">
        <span class="nav-icon" aria-hidden="true">💳</span> Invoices & Billing
      </a>
    </nav>
  `,
  styles: [`
    .app-header {
      background: #111827;
      color: #f9fafb;
      padding: 0.75rem 1.5rem;
      display: flex;
      justify-content: space-between;
      align-items: center;
      border-bottom: 2px solid #374151;
    }
    .header-left, .header-right {
      display: flex;
      align-items: center;
      gap: 1rem;
    }
    .brand {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      font-size: 1.25rem;
      font-weight: 700;
      color: #10b981;
    }
    .version-badge {
      font-size: 0.7rem;
      background: #1f2937;
      padding: 0.15rem 0.4rem;
      border-radius: 4px;
      color: #9ca3af;
      border: 1px solid #4b5563;
    }
    .tenant-badge {
      font-size: 0.8rem;
      background: #064e3b;
      color: #a7f3d0;
      padding: 0.25rem 0.6rem;
      border-radius: 9999px;
      display: flex;
      align-items: center;
      gap: 0.4rem;
    }
    .tenant-dot {
      width: 6px;
      height: 6px;
      border-radius: 50%;
      background: #34d399;
    }
    .signalr-status {
      font-size: 0.75rem;
      display: flex;
      align-items: center;
      gap: 0.4rem;
      padding: 0.25rem 0.5rem;
      border-radius: 4px;
      background: #374151;
      color: #d1d5db;
    }
    .signalr-status.connected {
      background: #065f46;
      color: #6ee7b7;
    }
    .pulse-indicator {
      width: 8px;
      height: 8px;
      border-radius: 50%;
      background: #9ca3af;
    }
    .signalr-status.connected .pulse-indicator {
      background: #10b981;
      box-shadow: 0 0 6px #10b981;
    }
    .role-selector {
      display: flex;
      align-items: center;
      gap: 0.4rem;
      font-size: 0.8rem;
    }
    .role-label {
      color: #9ca3af;
    }
    .role-selector select {
      background: #1f2937;
      color: #f3f4f6;
      border: 1px solid #4b5563;
      padding: 0.3rem 0.6rem;
      border-radius: 4px;
      cursor: pointer;
    }
    .user-pill {
      display: flex;
      align-items: center;
      gap: 0.4rem;
      background: #1f2937;
      padding: 0.25rem 0.6rem;
      border-radius: 9999px;
      font-size: 0.8rem;
    }
    .user-avatar {
      width: 22px;
      height: 22px;
      border-radius: 50%;
      background: #10b981;
      color: #fff;
      display: flex;
      align-items: center;
      justify-content: center;
      font-weight: bold;
    }
    .event-banner {
      background: #1e3a8a;
      color: #dbeafe;
      padding: 0.5rem 1.5rem;
      display: flex;
      align-items: center;
      gap: 0.5rem;
      font-size: 0.85rem;
      border-bottom: 1px solid #3b82f6;
    }
    .event-dismiss {
      margin-left: auto;
      background: none;
      border: none;
      color: #93c5fd;
      font-size: 1.2rem;
      cursor: pointer;
    }
    .app-nav {
      background: #1f2937;
      border-bottom: 1px solid #374151;
      display: flex;
      padding: 0 1rem;
      gap: 0.5rem;
      overflow-x: auto;
    }
    .nav-link {
      color: #9ca3af;
      text-decoration: none;
      padding: 0.75rem 1rem;
      font-size: 0.9rem;
      font-weight: 500;
      border-bottom: 2px solid transparent;
      display: flex;
      align-items: center;
      gap: 0.4rem;
      transition: all 0.15s;
    }
    .nav-link:hover {
      color: #f3f4f6;
      background: #374151;
    }
    .nav-link.active {
      color: #10b981;
      border-bottom-color: #10b981;
      background: #111827;
    }
    .sr-only {
      position: absolute;
      width: 1px;
      height: 1px;
      padding: 0;
      margin: -1px;
      overflow: hidden;
      clip: rect(0, 0, 0, 0);
      border: 0;
    }
  `]
})
export class NavShellComponent {
  auth = inject(AuthService);
  signalR = inject(SignalRService);

  onRoleChange(e: Event) {
    const role = (e.target as HTMLSelectElement).value as any;
    this.auth.switchRole(role);
  }
}
