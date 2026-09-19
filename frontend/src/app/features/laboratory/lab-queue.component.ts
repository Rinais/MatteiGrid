import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../core/api.service';
import { SignalRService } from '../../core/signalr.service';
import { LaboratoryOrder } from '../../core/models';

@Component({
  selector: 'app-lab-queue',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="lab-container">
      <div class="lab-header">
        <div>
          <h1 class="page-title">Laboratory Preparation Queue</h1>
          <p class="page-subtitle">Real-time prescription fulfillment and compound dispensing bench</p>
        </div>
        <div class="barcode-sim">
          <span class="barcode-icon" aria-hidden="true">📷</span>
          <span>QR/Barcode Scanner: <strong>Active</strong></span>
        </div>
      </div>

      <!-- Queue Swimlanes / Stage Filter -->
      <div class="queue-status-filters">
        <button
          *ngFor="let s of statuses"
          class="filter-pill"
          [class.active]="selectedStatus === s"
          (click)="selectStatus(s)"
        >
          {{ s }}
        </button>
      </div>

      <!-- Live Order Cards -->
      <div class="orders-grid">
        <div *ngFor="let order of filteredOrders()" class="order-card" [class.rejected]="order.status === 'Rejected'">
          <div class="card-top">
            <div class="order-identity">
              <span class="order-number">Order #{{ order.id.slice(0, 8) }}</span>
              <span class="barcode-tag">QR: {{ order.prescriptionId.slice(0, 6) }}</span>
            </div>
            <span class="status-badge" [ngClass]="order.status.toLowerCase()">{{ order.status }}</span>
          </div>

          <div class="card-middle">
            <div class="presc-info">
              <strong>Prescription:</strong> #{{ order.prescriptionId.slice(0, 8) }}
            </div>
            <div class="timestamp">
              Received: {{ order.createdAt | date:'short' }}
            </div>
            <div *ngIf="order.rejectionComment" class="rejection-box">
              <strong>Rejection Reason:</strong> {{ order.rejectionComment }}
            </div>
          </div>

          <div class="card-bottom">
            <button
              class="btn btn-advance"
              *ngIf="getNextStatus(order.status) && order.status !== 'Rejected'"
              (click)="advanceStatus(order)"
            >
              Advance to {{ getNextStatus(order.status) }} →
            </button>

            <button
              class="btn btn-reject"
              *ngIf="order.status !== 'Ready' && order.status !== 'Collected' && order.status !== 'Rejected'"
              (click)="openRejectModal(order)"
            >
              Reject Order
            </button>
          </div>
        </div>

        <div *ngIf="filteredOrders().length === 0" class="empty-queue">
          No laboratory orders currently in this status.
        </div>
      </div>

      <!-- Rejection Modal with Mandatory Comment -->
      <div class="modal-backdrop" *ngIf="rejectingOrder">
        <div class="modal-dialog">
          <div class="modal-header danger">
            <h3>Reject Laboratory Order #{{ rejectingOrder.id.slice(0, 8) }}</h3>
            <button class="modal-close" (click)="rejectingOrder = null">×</button>
          </div>
          <div class="modal-body">
            <p class="text-muted">
              Per Section 5.7 of PRD, rejected orders return to the doctor's queue with mandatory comments.
              The doctor will issue a revised prescription (v2).
            </p>

            <div class="form-group">
              <label>Rejection Reason / Comment *</label>
              <textarea
                [(ngModel)]="rejectionComment"
                rows="4"
                class="form-control"
                placeholder="e.g. Stock for Scrofoloso 1 30C depleted at lab bench; please revise to 6X dilution."
              ></textarea>
            </div>
          </div>
          <div class="modal-footer">
            <button class="btn btn-outline" (click)="rejectingOrder = null">Cancel</button>
            <button
              class="btn btn-danger"
              [disabled]="!rejectionComment.trim()"
              (click)="confirmRejection()"
            >
              Confirm Rejection & Return to Doctor
            </button>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .lab-container { padding: 1.5rem 2rem; max-width: 1200px; margin: 0 auto; }
    .lab-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 1.5rem; }
    .page-title { font-size: 1.75rem; font-weight: 700; color: #111827; margin: 0; }
    .page-subtitle { color: #6b7280; margin: 0.25rem 0 0; font-size: 0.9rem; }
    .barcode-sim {
      background: #f3f4f6; border: 1px solid #d1d5db; padding: 0.4rem 0.8rem;
      border-radius: 6px; font-size: 0.85rem; display: flex; align-items: center; gap: 0.4rem;
    }
    .queue-status-filters {
      display: flex; gap: 0.5rem; margin-bottom: 1.5rem; overflow-x: auto; padding-bottom: 0.5rem;
    }
    .filter-pill {
      background: white; border: 1px solid #d1d5db; padding: 0.4rem 0.8rem; border-radius: 9999px;
      font-size: 0.8rem; font-weight: 500; cursor: pointer; color: #4b5563;
    }
    .filter-pill.active { background: #10b981; color: white; border-color: #059669; }
    .orders-grid {
      display: grid; grid-template-columns: repeat(auto-fit, minmax(320px, 1fr)); gap: 1.25rem;
    }
    .order-card {
      background: white; border: 1px solid #e5e7eb; border-radius: 8px; padding: 1.25rem;
      box-shadow: 0 1px 3px rgba(0,0,0,0.05); display: flex; flex-direction: column; justify-content: space-between;
    }
    .order-card.rejected { border-left: 4px solid #ef4444; background: #fff5f5; }
    .card-top { display: flex; justify-content: space-between; align-items: center; margin-bottom: 0.75rem; }
    .order-number { font-weight: 700; color: #111827; }
    .barcode-tag { font-size: 0.75rem; background: #f3f4f6; padding: 0.1rem 0.3rem; border-radius: 4px; margin-left: 0.5rem; color: #6b7280; }
    .status-badge { font-size: 0.75rem; padding: 0.2rem 0.5rem; border-radius: 9999px; font-weight: 600; }
    .status-badge.received { background: #e0e7ff; color: #3730a3; }
    .status-badge.accepted { background: #dbeafe; color: #1e40af; }
    .status-badge.preparing { background: #fef3c7; color: #92400e; }
    .status-badge.qualitycheck { background: #fae8ff; color: #86198f; }
    .status-badge.prepared { background: #d1fae5; color: #065f46; }
    .status-badge.ready { background: #10b981; color: white; }
    .status-badge.collected { background: #f3f4f6; color: #6b7280; }
    .status-badge.rejected { background: #fee2e2; color: #991b1b; }
    .card-middle { margin-bottom: 1rem; font-size: 0.85rem; }
    .presc-info { color: #374151; margin-bottom: 0.25rem; }
    .timestamp { font-size: 0.75rem; color: #9ca3af; }
    .rejection-box {
      margin-top: 0.5rem; background: #fee2e2; padding: 0.5rem; border-radius: 4px; color: #991b1b; font-size: 0.8rem;
    }
    .card-bottom { display: flex; gap: 0.5rem; }
    .btn { padding: 0.45rem 0.75rem; border-radius: 6px; font-size: 0.8rem; font-weight: 600; cursor: pointer; border: 1px solid transparent; }
    .btn-advance { background: #10b981; color: white; flex: 1; }
    .btn-advance:hover { background: #059669; }
    .btn-reject { background: white; border-color: #fca5a5; color: #dc2626; }
    .btn-reject:hover { background: #fee2e2; }
    .btn-outline { background: white; border-color: #d1d5db; color: #374151; }
    .btn-danger { background: #ef4444; color: white; }
    .empty-queue { grid-column: 1 / -1; padding: 2.5rem; text-align: center; color: #9ca3af; background: white; border-radius: 8px; border: 1px dashed #d1d5db; }
    .modal-backdrop {
      position: fixed; top: 0; left: 0; right: 0; bottom: 0; background: rgba(0,0,0,0.5);
      display: flex; align-items: center; justify-content: center; z-index: 1000;
    }
    .modal-dialog { background: white; border-radius: 8px; width: 500px; max-width: 90%; }
    .modal-header { padding: 1rem 1.25rem; border-bottom: 1px solid #e5e7eb; display: flex; justify-content: space-between; align-items: center; }
    .modal-header.danger h3 { color: #dc2626; margin: 0; font-size: 1.1rem; }
    .modal-close { background: none; border: none; font-size: 1.5rem; cursor: pointer; color: #9ca3af; }
    .modal-body { padding: 1.25rem; }
    .modal-footer { padding: 0.75rem 1.25rem; border-top: 1px solid #e5e7eb; display: flex; justify-content: flex-end; gap: 0.75rem; }
    .form-group label { display: block; font-size: 0.85rem; font-weight: 500; margin-bottom: 0.35rem; }
    .form-control { width: 100%; box-sizing: border-box; padding: 0.5rem; border: 1px solid #d1d5db; border-radius: 4px; font-family: inherit; font-size: 0.85rem; }
    .text-muted { color: #6b7280; font-size: 0.85rem; }
  `]
})
export class LabQueueComponent implements OnInit {
  api = inject(ApiService);
  signalR = inject(SignalRService);

  orders = signal<LaboratoryOrder[]>([]);
  selectedStatus = 'All Active';
  rejectingOrder: LaboratoryOrder | null = null;
  rejectionComment = '';

  statuses = ['All Active', 'Received', 'Accepted', 'Preparing', 'QualityCheck', 'Prepared', 'Ready', 'Collected', 'Rejected'];

  private sequence = ['Received', 'Accepted', 'Preparing', 'QualityCheck', 'Prepared', 'Ready', 'Collected'];

  ngOnInit() {
    this.loadOrders();
    this.signalR.startConnections();
  }

  loadOrders() {
    this.api.getLabQueue().subscribe(res => this.orders.set(res));
  }

  selectStatus(status: string) {
    this.selectedStatus = status;
  }

  filteredOrders(): LaboratoryOrder[] {
    if (this.selectedStatus === 'All Active') {
      return this.orders().filter(o => o.status !== 'Collected');
    }
    return this.orders().filter(o => o.status === this.selectedStatus);
  }

  getNextStatus(current: string): string | null {
    const idx = this.sequence.indexOf(current);
    if (idx >= 0 && idx < this.sequence.length - 1) {
      return this.sequence[idx + 1];
    }
    return null;
  }

  advanceStatus(order: LaboratoryOrder) {
    const next = this.getNextStatus(order.status);
    if (!next) return;

    this.api.transitionLabStatus(order.id, next).subscribe(() => {
      this.loadOrders();
    });
  }

  openRejectModal(order: LaboratoryOrder) {
    this.rejectingOrder = order;
    this.rejectionComment = '';
  }

  confirmRejection() {
    if (!this.rejectingOrder || !this.rejectionComment.trim()) return;

    this.api.rejectLabOrder(this.rejectingOrder.id, this.rejectionComment.trim()).subscribe(() => {
      this.rejectingOrder = null;
      this.loadOrders();
    });
  }
}
