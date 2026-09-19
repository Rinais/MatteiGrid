import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../core/api.service';
import { Invoice } from '../../core/models';

@Component({
  selector: 'app-billing',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="billing-container">
      <div class="billing-header">
        <div>
          <h1 class="page-title">Invoices & Payment Collection</h1>
          <p class="page-subtitle">Razorpay UPI Route integration, 48h payment link TTL, and cash/POS reconciliation</p>
        </div>
      </div>

      <div class="invoices-layout">
        <!-- Invoices List -->
        <div class="invoices-list-pane">
          <div class="pane-title">All Clinic Invoices</div>
          <div
            *ngFor="let inv of invoices()"
            class="invoice-card"
            [class.active]="selectedInvoice?.id === inv.id"
            (click)="selectInvoice(inv)"
          >
            <div class="inv-card-top">
              <span class="inv-id">Invoice #{{ inv.id.slice(0, 8) }}</span>
              <span class="status-badge" [ngClass]="inv.status.toLowerCase()">{{ inv.status }}</span>
            </div>
            <div class="inv-card-meta">
              <span>Patient: Ramesh Kumar</span>
              <span class="inv-amount">₹{{ inv.totalAmount }}</span>
            </div>
            <div class="inv-card-balance" *ngIf="inv.remainingBalance > 0">
              Remaining: <strong>₹{{ inv.remainingBalance }}</strong>
            </div>
          </div>
        </div>

        <!-- Invoice Detail & Payment Actions -->
        <div class="invoice-detail-pane" *ngIf="selectedInvoice">
          <div class="detail-header">
            <div>
              <h2>Invoice Details (#{{ selectedInvoice.id.slice(0, 8) }})</h2>
              <div class="text-muted">Issued: {{ selectedInvoice.createdAt | date:'medium' }}</div>
            </div>
            <span class="status-badge large" [ngClass]="selectedInvoice.status.toLowerCase()">
              {{ selectedInvoice.status }}
            </span>
          </div>

          <!-- Payment State Machine Stepper -->
          <div class="state-machine-stepper">
            <div class="step" [class.done]="selectedInvoice.status !== 'Pending'">
              <span class="step-num">1</span>
              <span>Pending</span>
            </div>
            <div class="step-line" [class.done]="selectedInvoice.status === 'PartiallyPaid' || selectedInvoice.status === 'Paid'"></div>
            <div class="step" [class.done]="selectedInvoice.status === 'PartiallyPaid' || selectedInvoice.status === 'Paid'">
              <span class="step-num">2</span>
              <span>{{ selectedInvoice.status === 'PartiallyPaid' ? 'Partially Paid' : 'Paid' }}</span>
            </div>
            <div class="step-line" *ngIf="selectedInvoice.status === 'Refunded' || selectedInvoice.status === 'Cancelled'"></div>
            <div class="step terminal" *ngIf="selectedInvoice.status === 'Refunded' || selectedInvoice.status === 'Cancelled'">
              <span class="step-num">3</span>
              <span>{{ selectedInvoice.status }}</span>
            </div>
          </div>

          <!-- Digital UPI Payment Link Box (48h TTL) -->
          <div class="payment-link-box" *ngIf="selectedInvoice.activePaymentLink">
            <div class="link-box-header">
              <span class="upi-icon" aria-hidden="true">📲</span>
              <div>
                <strong>Razorpay Route UPI Payment Link (48h TTL)</strong>
                <div class="link-subtext">Sent via SMS to patient's registered mobile number</div>
              </div>
              <span class="ttl-badge">Valid for 48 Hours</span>
            </div>

            <div class="link-url-display">
              https://api.razorpay.com/v1/checkout/links/{{ selectedInvoice.activePaymentLink.tokenHash.slice(0, 16) }}
            </div>

            <div class="link-actions">
              <button class="btn-sm btn-outline" (click)="simulateDigitalPayment()">
                ⚡ Simulate Patient UPI Payment (Webhook)
              </button>
              <button class="btn-sm btn-outline" (click)="regenerateLink()">
                🔄 Regenerate Expired Link
              </button>
            </div>
          </div>

          <!-- Line Items Table -->
          <div class="section-title">Itemized Services & Remedies</div>
          <table class="items-table">
            <thead>
              <tr>
                <th>Description</th>
                <th>Qty</th>
                <th>Unit Price</th>
                <th>Amount</th>
              </tr>
            </thead>
            <tbody>
              <tr *ngFor="let item of selectedInvoice.lineItems">
                <td>{{ item.description }}</td>
                <td>{{ item.quantity }}</td>
                <td>₹{{ item.unitPrice }}</td>
                <td>₹{{ item.amount }}</td>
              </tr>
            </tbody>
            <tfoot>
              <tr>
                <td colspan="3" class="text-right"><strong>Total Invoice Amount:</strong></td>
                <td><strong>₹{{ selectedInvoice.totalAmount }}</strong></td>
              </tr>
              <tr>
                <td colspan="3" class="text-right">Total Amount Settled:</td>
                <td class="text-success">₹{{ selectedInvoice.totalPaidAmount }}</td>
              </tr>
              <tr *ngIf="selectedInvoice.remainingBalance > 0">
                <td colspan="3" class="text-right"><strong>Remaining Outstanding:</strong></td>
                <td class="text-danger"><strong>₹{{ selectedInvoice.remainingBalance }}</strong></td>
              </tr>
            </tfoot>
          </table>

          <!-- Action Buttons -->
          <div class="action-footer" *ngIf="selectedInvoice.status !== 'Paid' && selectedInvoice.status !== 'Cancelled' && selectedInvoice.status !== 'Refunded'">
            <button class="btn btn-primary" (click)="showManualPayModal = true">
              💵 Record Manual Payment (Cash / POS Card)
            </button>
          </div>
        </div>
      </div>

      <!-- Manual Payment Modal -->
      <div class="modal-backdrop" *ngIf="showManualPayModal && selectedInvoice">
        <div class="modal-dialog">
          <div class="modal-header">
            <h3>Record Manual Payment (Cash / Card POS)</h3>
            <button class="modal-close" (click)="showManualPayModal = false">×</button>
          </div>
          <div class="modal-body">
            <p class="text-muted">
              Per Section 5.6 & 5.9, receptionist can mark payments received directly on-site.
              Requires amount entered + mode selected; marks Paid immediately without webhook dependency.
            </p>

            <div class="form-group">
              <label>Payment Mode *</label>
              <select [(ngModel)]="paymentForm.mode" class="form-control">
                <option value="Cash">Cash (Manual Drawer)</option>
                <option value="Card">Card (Clinic POS Terminal)</option>
                <option value="UPI">UPI (Direct QR Scan at Desk)</option>
              </select>
            </div>

            <div class="form-group">
              <label>Amount (₹) *</label>
              <input
                type="number"
                [(ngModel)]="paymentForm.amount"
                class="form-control"
                [max]="selectedInvoice.remainingBalance"
                min="1"
              />
              <small class="text-muted">Maximum allowed: ₹{{ selectedInvoice.remainingBalance }} (Overpayment strictly rejected)</small>
            </div>
          </div>
          <div class="modal-footer">
            <button class="btn btn-outline" (click)="showManualPayModal = false">Cancel</button>
            <button
              class="btn btn-primary"
              (click)="submitManualPayment()"
              [disabled]="paymentForm.amount <= 0 || paymentForm.amount > selectedInvoice.remainingBalance"
            >
              Record Payment & Issue Receipt
            </button>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .billing-container { padding: 1.5rem 2rem; max-width: 1200px; margin: 0 auto; }
    .billing-header { margin-bottom: 1.5rem; }
    .page-title { font-size: 1.75rem; font-weight: 700; color: #111827; margin: 0; }
    .page-subtitle { color: #6b7280; margin: 0.25rem 0 0; font-size: 0.9rem; }
    .invoices-layout {
      display: grid; grid-template-columns: 340px 1fr; gap: 1.5rem;
    }
    .invoices-list-pane {
      background: white; border: 1px solid #e5e7eb; border-radius: 8px; padding: 1rem;
      height: fit-content;
    }
    .pane-title { font-weight: 700; font-size: 1rem; margin-bottom: 1rem; color: #111827; }
    .invoice-card {
      border: 1px solid #e5e7eb; border-radius: 6px; padding: 0.85rem; margin-bottom: 0.75rem;
      cursor: pointer; transition: all 0.15s; background: #fafafa;
    }
    .invoice-card:hover { border-color: #10b981; }
    .invoice-card.active { border-color: #059669; background: #ecfdf5; }
    .inv-card-top { display: flex; justify-content: space-between; align-items: center; margin-bottom: 0.35rem; }
    .inv-id { font-weight: 600; font-size: 0.9rem; color: #111827; }
    .inv-card-meta { display: flex; justify-content: space-between; font-size: 0.85rem; color: #4b5563; }
    .inv-amount { font-weight: 700; color: #111827; }
    .inv-card-balance { font-size: 0.75rem; color: #dc2626; margin-top: 0.35rem; }
    .invoice-detail-pane {
      background: white; border: 1px solid #e5e7eb; border-radius: 8px; padding: 1.5rem;
      box-shadow: 0 1px 3px rgba(0,0,0,0.05);
    }
    .detail-header { display: flex; justify-content: space-between; align-items: flex-start; margin-bottom: 1.5rem; }
    .detail-header h2 { margin: 0; font-size: 1.3rem; font-weight: 700; }
    .status-badge { font-size: 0.75rem; padding: 0.2rem 0.5rem; border-radius: 9999px; font-weight: 600; }
    .status-badge.large { font-size: 0.85rem; padding: 0.35rem 0.75rem; }
    .status-badge.pending { background: #fef3c7; color: #92400e; }
    .status-badge.partiallypaid { background: #dbeafe; color: #1e40af; }
    .status-badge.paid { background: #d1fae5; color: #065f46; }
    .status-badge.refunded { background: #fee2e2; color: #991b1b; }
    .status-badge.cancelled { background: #f3f4f6; color: #4b5563; }
    .state-machine-stepper {
      display: flex; align-items: center; justify-content: center; gap: 0.5rem; margin-bottom: 1.5rem;
      background: #f9fafb; padding: 1rem; border-radius: 8px; border: 1px solid #e5e7eb;
    }
    .step { display: flex; align-items: center; gap: 0.4rem; font-size: 0.8rem; font-weight: 600; color: #9ca3af; }
    .step.done { color: #059669; }
    .step.terminal { color: #dc2626; }
    .step-num { width: 20px; height: 20px; border-radius: 50%; background: #e5e7eb; display: flex; align-items: center; justify-content: center; font-size: 0.75rem; }
    .step.done .step-num { background: #10b981; color: white; }
    .step.terminal .step-num { background: #ef4444; color: white; }
    .step-line { width: 60px; height: 2px; background: #e5e7eb; }
    .step-line.done { background: #10b981; }
    .payment-link-box {
      background: #eff6ff; border: 1px solid #bfdbfe; border-radius: 8px; padding: 1rem 1.25rem; margin-bottom: 1.5rem;
    }
    .link-box-header { display: flex; align-items: center; gap: 0.75rem; font-size: 0.9rem; margin-bottom: 0.5rem; }
    .upi-icon { font-size: 1.5rem; }
    .ttl-badge { margin-left: auto; font-size: 0.75rem; background: #3b82f6; color: white; padding: 0.2rem 0.5rem; border-radius: 4px; }
    .link-subtext { font-size: 0.75rem; color: #6b7280; }
    .link-url-display {
      background: white; border: 1px dashed #93c5fd; padding: 0.5rem 0.75rem; border-radius: 4px;
      font-size: 0.8rem; font-family: monospace; color: #1e40af; margin-bottom: 0.75rem; word-break: break-all;
    }
    .link-actions { display: flex; gap: 0.5rem; }
    .section-title { font-size: 1rem; font-weight: 600; margin-bottom: 0.75rem; }
    .items-table { width: 100%; border-collapse: collapse; font-size: 0.85rem; margin-bottom: 1.5rem; }
    .items-table th { text-align: left; padding: 0.6rem 0.75rem; background: #f9fafb; border-bottom: 1px solid #e5e7eb; color: #4b5563; }
    .items-table td { padding: 0.6rem 0.75rem; border-bottom: 1px solid #f3f4f6; }
    .items-table tfoot td { padding: 0.6rem 0.75rem; border-top: 1px solid #e5e7eb; }
    .text-right { text-align: right; }
    .text-success { color: #059669; font-weight: 600; }
    .text-danger { color: #dc2626; font-weight: 600; }
    .btn { padding: 0.5rem 1rem; border-radius: 6px; font-weight: 500; font-size: 0.875rem; cursor: pointer; border: 1px solid transparent; }
    .btn-primary { background: #10b981; color: white; }
    .btn-outline { background: white; border-color: #d1d5db; color: #374151; }
    .btn-sm { padding: 0.35rem 0.65rem; font-size: 0.75rem; border-radius: 4px; cursor: pointer; }
    .action-footer { border-top: 1px solid #e5e7eb; padding-top: 1.25rem; display: flex; justify-content: flex-end; }
    .modal-backdrop { position: fixed; top: 0; left: 0; right: 0; bottom: 0; background: rgba(0,0,0,0.5); display: flex; align-items: center; justify-content: center; z-index: 1000; }
    .modal-dialog { background: white; border-radius: 8px; width: 480px; max-width: 90%; }
    .modal-header { padding: 1rem 1.25rem; border-bottom: 1px solid #e5e7eb; display: flex; justify-content: space-between; align-items: center; }
    .modal-header h3 { margin: 0; font-size: 1.15rem; }
    .modal-close { background: none; border: none; font-size: 1.5rem; cursor: pointer; color: #9ca3af; }
    .modal-body { padding: 1.25rem; }
    .modal-footer { padding: 0.75rem 1.25rem; border-top: 1px solid #e5e7eb; display: flex; justify-content: flex-end; gap: 0.75rem; }
    .form-group { margin-bottom: 1rem; }
    .form-group label { display: block; font-size: 0.85rem; font-weight: 500; margin-bottom: 0.35rem; }
    .form-control { width: 100%; box-sizing: border-box; padding: 0.5rem; border: 1px solid #d1d5db; border-radius: 4px; font-size: 0.85rem; }
    .text-muted { color: #6b7280; font-size: 0.85rem; }
  `]
})
export class BillingComponent implements OnInit {
  api = inject(ApiService);

  invoices = signal<Invoice[]>([]);
  selectedInvoice: Invoice | null = null;
  showManualPayModal = false;

  paymentForm = {
    mode: 'Cash',
    amount: 600
  };

  ngOnInit() {
    this.loadInvoices();
  }

  loadInvoices() {
    this.api.getInvoices().subscribe(res => {
      this.invoices.set(res);
      if (res.length > 0 && !this.selectedInvoice) {
        this.selectedInvoice = res[0];
        this.paymentForm.amount = this.selectedInvoice.remainingBalance;
      }
    });
  }

  selectInvoice(inv: Invoice) {
    this.selectedInvoice = inv;
    this.paymentForm.amount = inv.remainingBalance;
  }

  simulateDigitalPayment() {
    if (!this.selectedInvoice) return;
    const razorpayId = `pay_${Date.now()}`;
    this.api.recordManualPayment(this.selectedInvoice.id, this.selectedInvoice.remainingBalance, 'UPI').subscribe(updated => {
      this.selectedInvoice = updated;
      this.loadInvoices();
    });
  }

  regenerateLink() {
    if (!this.selectedInvoice) return;
    this.api.regeneratePaymentLink(this.selectedInvoice.id).subscribe(() => {
      this.loadInvoices();
    });
  }

  submitManualPayment() {
    if (!this.selectedInvoice) return;

    this.api.recordManualPayment(this.selectedInvoice.id, this.paymentForm.amount, this.paymentForm.mode).subscribe(updated => {
      this.selectedInvoice = updated;
      this.showManualPayModal = false;
      this.loadInvoices();
    });
  }
}
