import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../core/api.service';
import { InventoryItem } from '../../core/models';

@Component({
  selector: 'app-inventory',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="inventory-container">
      <div class="inventory-header">
        <div>
          <h1 class="page-title">Inventory & Stock Ledger</h1>
          <p class="page-subtitle">Raw medicines, vehicles, bottles, and automated laboratory deduction tracking</p>
        </div>
        <button class="btn btn-primary" (click)="showRestockModal = true">+ Receive Stock</button>
      </div>

      <!-- Filters & Stats -->
      <div class="stats-bar">
        <div class="stat-pill">Total Items: <strong>{{ items().length }}</strong></div>
        <div class="stat-pill warning" (click)="toggleLowStockOnly()" [class.active]="lowStockOnly">
          ⚠️ Low Stock Alerts: <strong>{{ lowStockItemsCount() }}</strong> (Click to Filter)
        </div>
      </div>

      <!-- Inventory Table -->
      <div class="table-container">
        <table>
          <thead>
            <tr>
              <th>Medicine / Item</th>
              <th>Quantity On Hand</th>
              <th>Reorder Threshold</th>
              <th>Status</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            <tr *ngFor="let item of filteredItems()">
              <td>
                <div class="item-name">
                  {{ getMedicineName(item.medicineId) }}
                  <span class="item-id">ID: #{{ item.id.slice(0, 6) }}</span>
                </div>
              </td>
              <td>
                <span class="qty-badge" [class.low]="item.isLowStock">{{ item.quantityOnHand }} units</span>
              </td>
              <td>{{ item.reorderLevel }} units</td>
              <td>
                <span class="status-pill" [class.alert]="item.isLowStock">
                  {{ item.isLowStock ? 'Reorder Required' : 'Adequate' }}
                </span>
              </td>
              <td>
                <button class="btn-sm btn-outline" (click)="quickRestock(item)">+ Restock</button>
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <!-- Restock Modal -->
      <div class="modal-backdrop" *ngIf="showRestockModal">
        <div class="modal-dialog">
          <div class="modal-header">
            <h3>Receive Inventory Stock</h3>
            <button class="modal-close" (click)="showRestockModal = false">×</button>
          </div>
          <div class="modal-body">
            <div class="form-group">
              <label>Medicine / Item *</label>
              <select [(ngModel)]="restockForm.medicineId" class="form-control">
                <option value="m1000000-0000-0000-0000-000000000001">Scrofoloso 1 (S1)</option>
                <option value="m1000000-0000-0000-0000-000000000002">Angiotico 1 (A1)</option>
                <option value="m1000000-0000-0000-0000-000000000003">Canceroso 1 (C1)</option>
                <option value="m1000000-0000-0000-0000-000000000004">Febbrifugo 1 (F1)</option>
              </select>
            </div>
            <div class="form-group">
              <label>Quantity to Add *</label>
              <input type="number" [(ngModel)]="restockForm.quantity" class="form-control" min="1" />
            </div>
            <div class="form-group">
              <label>Supplier / Batch Receipt Note</label>
              <input type="text" [(ngModel)]="restockForm.reason" class="form-control" placeholder="e.g. Batch #EH-2026-09 Supplier Delivery" />
            </div>
          </div>
          <div class="modal-footer">
            <button class="btn btn-outline" (click)="showRestockModal = false">Cancel</button>
            <button class="btn btn-primary" (click)="submitRestock()" [disabled]="restockForm.quantity <= 0">
              Confirm Stock Receipt
            </button>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .inventory-container { padding: 1.5rem 2rem; max-width: 1200px; margin: 0 auto; }
    .inventory-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 1.5rem; }
    .page-title { font-size: 1.75rem; font-weight: 700; color: #111827; margin: 0; }
    .page-subtitle { color: #6b7280; margin: 0.25rem 0 0; font-size: 0.9rem; }
    .stats-bar { display: flex; gap: 1rem; margin-bottom: 1.5rem; }
    .stat-pill { background: white; border: 1px solid #d1d5db; padding: 0.4rem 0.8rem; border-radius: 6px; font-size: 0.85rem; }
    .stat-pill.warning { border-color: #f59e0b; color: #b45309; cursor: pointer; }
    .stat-pill.warning.active { background: #fef3c7; font-weight: bold; }
    .table-container { background: white; border: 1px solid #e5e7eb; border-radius: 8px; overflow-x: auto; box-shadow: 0 1px 3px rgba(0,0,0,0.05); }
    table { width: 100%; border-collapse: collapse; font-size: 0.875rem; }
    th { text-align: left; padding: 0.85rem 1rem; background: #f9fafb; color: #4b5563; border-bottom: 1px solid #e5e7eb; }
    td { padding: 0.85rem 1rem; border-bottom: 1px solid #f3f4f6; }
    .item-name { font-weight: 600; color: #111827; }
    .item-id { font-size: 0.75rem; color: #9ca3af; margin-left: 0.5rem; font-weight: normal; }
    .qty-badge { font-weight: 600; }
    .qty-badge.low { color: #dc2626; }
    .status-pill { font-size: 0.75rem; padding: 0.2rem 0.5rem; border-radius: 9999px; background: #d1fae5; color: #065f46; }
    .status-pill.alert { background: #fee2e2; color: #991b1b; font-weight: bold; }
    .btn { padding: 0.5rem 1rem; border-radius: 6px; font-weight: 500; font-size: 0.875rem; cursor: pointer; border: 1px solid transparent; }
    .btn-primary { background: #10b981; color: white; }
    .btn-outline { background: white; border-color: #d1d5db; color: #374151; }
    .btn-sm { padding: 0.35rem 0.65rem; font-size: 0.8rem; }
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
  `]
})
export class InventoryComponent implements OnInit {
  api = inject(ApiService);

  items = signal<InventoryItem[]>([]);
  lowStockOnly = false;
  showRestockModal = false;

  restockForm = {
    medicineId: 'm1000000-0000-0000-0000-000000000001',
    quantity: 20,
    reason: ''
  };

  private medicineNames: Record<string, string> = {
    'm1000000-0000-0000-0000-000000000001': 'Scrofoloso 1 (S1)',
    'm1000000-0000-0000-0000-000000000002': 'Angiotico 1 (A1)',
    'm1000000-0000-0000-0000-000000000003': 'Canceroso 1 (C1)',
    'm1000000-0000-0000-0000-000000000004': 'Febbrifugo 1 (F1)'
  };

  ngOnInit() {
    this.loadInventory();
  }

  loadInventory() {
    this.api.getInventory().subscribe(res => this.items.set(res));
  }

  toggleLowStockOnly() {
    this.lowStockOnly = !this.lowStockOnly;
  }

  filteredItems(): InventoryItem[] {
    if (this.lowStockOnly) {
      return this.items().filter(i => i.isLowStock);
    }
    return this.items();
  }

  lowStockItemsCount(): number {
    return this.items().filter(i => i.isLowStock).length;
  }

  getMedicineName(id: string): string {
    return this.medicineNames[id] || `Raw Remedy #${id.slice(0, 6)}`;
  }

  quickRestock(item: InventoryItem) {
    this.restockForm.medicineId = item.medicineId;
    this.showRestockModal = true;
  }

  submitRestock() {
    this.api.receiveStock(this.restockForm.medicineId, this.restockForm.quantity, this.restockForm.reason)
      .subscribe(() => {
        this.showRestockModal = false;
        this.loadInventory();
      });
  }
}
