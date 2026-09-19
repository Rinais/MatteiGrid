import { Routes } from '@angular/router';
import { DashboardComponent } from './features/dashboard/dashboard.component';
import { PatientListComponent } from './features/patients/patient-list.component';
import { ConsultationWorkspaceComponent } from './features/consultation/consultation-workspace.component';
import { LabQueueComponent } from './features/laboratory/lab-queue.component';
import { InventoryComponent } from './features/inventory/inventory.component';
import { BillingComponent } from './features/billing/billing.component';

export const routes: Routes = [
  { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
  { path: 'dashboard', component: DashboardComponent },
  { path: 'patients', component: PatientListComponent },
  { path: 'consultation', component: ConsultationWorkspaceComponent },
  { path: 'lab', component: LabQueueComponent },
  { path: 'inventory', component: InventoryComponent },
  { path: 'billing', component: BillingComponent },
  { path: '**', redirectTo: 'dashboard' }
];
