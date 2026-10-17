import { Routes } from '@angular/router';
import { LoginComponent } from './features/auth/login.component';
import { DashboardComponent } from './features/dashboard/dashboard.component';
import { PatientListComponent } from './features/patients/patient-list.component';
import { ConsultationWorkspaceComponent } from './features/consultation/consultation-workspace.component';
import { LabQueueComponent } from './features/laboratory/lab-queue.component';
import { InventoryComponent } from './features/inventory/inventory.component';
import { BillingComponent } from './features/billing/billing.component';
import { authGuard, guestGuard, permissionGuard } from './core/auth.guard';

export const routes: Routes = [
  { path: 'login', component: LoginComponent, canActivate: [guestGuard] },
  { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
  { path: 'dashboard', component: DashboardComponent, canActivate: [authGuard] },
  { path: 'patients', component: PatientListComponent, canActivate: [authGuard, permissionGuard('Patients.Read')] },
  { path: 'consultation', component: ConsultationWorkspaceComponent, canActivate: [authGuard, permissionGuard('Consultations.Read')] },
  { path: 'lab', component: LabQueueComponent, canActivate: [authGuard, permissionGuard('Laboratory.Read')] },
  { path: 'inventory', component: InventoryComponent, canActivate: [authGuard, permissionGuard('Inventory.Read')] },
  { path: 'billing', component: BillingComponent, canActivate: [authGuard, permissionGuard('Billing.Read')] },
  { path: '**', redirectTo: 'dashboard' }
];
