import { Injectable, inject, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, of, tap, catchError, map } from 'rxjs';
import { User, AuthResponse, ClinicMembership } from './models';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private http = inject(HttpClient);
  private router = inject(Router);

  private readonly USER_KEY = 'mattei_grid_user';
  private readonly API_URL = 'http://localhost:5000/api/v1';

  // In-memory access token (AUTH-04: Token held in memory, refresh token stored via HttpOnly cookie)
  private inMemoryAccessToken = signal<string | null>(null);

  currentUser = signal<User | null>(this.getStoredUser());
  currentClinicId = computed(() => this.currentUser()?.clinicId || '00000000-0000-0000-0000-000000000001');
  isAuthenticated = computed(() => !!this.currentUser());

  // Multi-tenant selection state (Decision 1)
  tenantSelectionPending = signal<boolean>(false);
  tenantSelectionToken = signal<string | null>(null);
  availableClinics = signal<ClinicMembership[]>([]);

  // Demo user lookup directory for seamless preview & fallback
  readonly DEMO_USERS: Record<string, { user: User; token: string; label: string; roleDesc: string; defaultEmail: string }> = {
    'Doctor': {
      label: 'Doctor (Clinical + Rx)',
      roleDesc: 'Prescriptions, Consultations & Materia Medica',
      defaultEmail: 'doctor@clinic.com',
      token: 'demo-jwt-token-doctor',
      user: {
        id: '11111111-1111-1111-1111-111111111111',
        email: 'doctor@clinic.com',
        fullName: 'Dr. Cesare Mattei',
        role: 'Doctor',
        clinicId: '00000000-0000-0000-0000-000000000001',
        permissions: [
          'Patients.Read', 'Patients.Create', 'Patients.Edit',
          'Appointments.Read',
          'ClinicalNotes.Read', 'ClinicalNotes.Create', 'ClinicalNotes.Edit',
          'Consultations.Read', 'Consultations.Create', 'Consultations.Edit',
          'Prescriptions.Read', 'Prescriptions.Create', 'Prescriptions.Edit', 'Prescriptions.Finalize',
          'MateriaMedica.Read',
          'Laboratory.Read',
          'Inventory.Read',
          'Billing.Read', 'Billing.Refund', 'Billing.Cancel',
          'Reports.Read'
        ]
      }
    },
    'Receptionist': {
      label: 'Receptionist (Front Desk)',
      roleDesc: 'Patient Registration, Appointments & Billing',
      defaultEmail: 'reception@clinic.com',
      token: 'demo-jwt-token-receptionist',
      user: {
        id: '22222222-2222-2222-2222-222222222222',
        email: 'reception@clinic.com',
        fullName: 'Anita Sharma (Receptionist)',
        role: 'Receptionist',
        clinicId: '00000000-0000-0000-0000-000000000001',
        permissions: [
          'Patients.Read', 'Patients.Create', 'Patients.Edit', 'Patients.Archive',
          'Appointments.Read', 'Appointments.Create', 'Appointments.Update', 'Appointments.Cancel',
          'Billing.Read', 'Billing.ManualPayment',
          'Reports.Read'
        ]
      }
    },
    'Lab Technician': {
      label: 'Lab Technician (Compounding)',
      roleDesc: 'Laboratory Queue, Preparation & Deductions',
      defaultEmail: 'lab@clinic.com',
      token: 'demo-jwt-token-lab',
      user: {
        id: '33333333-3333-3333-3333-333333333333',
        email: 'lab@clinic.com',
        fullName: 'Rahul Verma (Lab Tech)',
        role: 'Lab Technician',
        clinicId: '00000000-0000-0000-0000-000000000001',
        permissions: [
          'Laboratory.Read', 'Laboratory.UpdateStatus', 'Laboratory.Reject',
          'Reports.Read'
        ]
      }
    },
    'Administrator': {
      label: 'Clinic Administrator',
      roleDesc: 'Inventory, Financial Ledgers & Audits',
      defaultEmail: 'admin@clinic.com',
      token: 'demo-jwt-token-admin',
      user: {
        id: '44444444-4444-4444-4444-444444444444',
        email: 'admin@clinic.com',
        fullName: 'Admin User',
        role: 'Administrator',
        clinicId: '00000000-0000-0000-0000-000000000001',
        permissions: [
          'Patients.Read', 'Patients.Create', 'Patients.Edit', 'Patients.Archive', 'Patients.ErasePii',
          'Appointments.Read', 'Appointments.Create', 'Appointments.Update', 'Appointments.Cancel',
          'MateriaMedica.Read', 'MateriaMedica.Edit',
          'Inventory.Read', 'Inventory.Manage',
          'Billing.Read', 'Billing.Create', 'Billing.ManualPayment', 'Billing.Refund', 'Billing.Cancel',
          'Administration.ManageUsers', 'Administration.AssignRoles', 'Administration.Settings', 'Administration.MasterData', 'Administration.AuditLogs', 'Administration.Reports',
          'Reports.Read'
        ]
      }
    },
    'SuperAdmin': {
      label: 'Platform SuperAdmin',
      roleDesc: 'Multi-Clinic Oversight & Platform Operations (Zero Clinical Data)',
      defaultEmail: 'superadmin@matteigrid.com',
      token: 'demo-jwt-token-superadmin',
      user: {
        id: '55555555-5555-5555-5555-555555555555',
        email: 'superadmin@matteigrid.com',
        fullName: 'Platform SuperAdmin',
        role: 'SuperAdmin',
        clinicId: '00000000-0000-0000-0000-000000000001',
        permissions: [
          'SuperAdmin.PlatformOps', 'SuperAdmin.ManageTenants', 'SuperAdmin.BreakGlass',
          'Administration.ManageUsers', 'Administration.AssignRoles', 'Administration.Settings', 'Administration.MasterData', 'Administration.AuditLogs', 'Administration.Reports'
        ]
      }
    }
  };

  isBreakGlass = signal<boolean>(false);

  login(email: string, password: string, clinicId?: string): Observable<boolean> {
    return this.http.post<AuthResponse>(
      `${this.API_URL}/auth/login`,
      { email, password, clinicId },
      { withCredentials: true }
    ).pipe(
      map(res => {
        if (res.requiresTenantSelection && res.tenantSelectionToken && res.availableClinics) {
          this.tenantSelectionPending.set(true);
          this.tenantSelectionToken.set(res.tenantSelectionToken);
          this.availableClinics.set(res.availableClinics);
          return true;
        }

        if (res.user && res.accessToken) {
          this.setUser(res.user, res.accessToken);
          this.tenantSelectionPending.set(false);
          return true;
        }
        return false;
      }),
      catchError((err) => {
        console.error('Authentication request failed:', err);
        return of(false);
      })
    );
  }

  selectTenant(clinicId: string): Observable<boolean> {
    const token = this.tenantSelectionToken();
    if (!token) return of(false);

    return this.http.post<AuthResponse>(
      `${this.API_URL}/auth/select-tenant`,
      { tenantSelectionToken: token, clinicId },
      { withCredentials: true }
    ).pipe(
      map(res => {
        if (res.user && res.accessToken) {
          this.setUser(res.user, res.accessToken);
          this.tenantSelectionPending.set(false);
          this.tenantSelectionToken.set(null);
          this.availableClinics.set([]);
          return true;
        }
        return false;
      }),
      catchError(() => of(false))
    );
  }

  loginWithDemoRole(roleKey: string): boolean {
    const demo = this.DEMO_USERS[roleKey];
    if (demo) {
      this.setUser(demo.user, demo.token);
      return true;
    }
    return false;
  }

  hasPermission(permission: string): boolean {
    const user = this.currentUser();
    if (!user) return false;
    if (user.role === 'SuperAdmin') {
      if (this.isBreakGlass()) return true;
      return permission.startsWith('SuperAdmin.') ||
             permission.startsWith('Administration.') ||
             (user.permissions?.includes(permission) ?? false);
    }
    return user.permissions?.includes(permission) ?? false;
  }

  setUser(user: User, token: string) {
    this.inMemoryAccessToken.set(token);
    localStorage.setItem(this.USER_KEY, JSON.stringify(user));
    this.currentUser.set(user);
  }

  switchRole(roleName: 'Doctor' | 'Receptionist' | 'Lab Technician' | 'Administrator' | 'SuperAdmin') {
    const demo = this.DEMO_USERS[roleName];
    if (demo) {
      this.setUser(demo.user, demo.token);
    }
  }

  logout() {
    const token = this.getToken();
    this.http.post(`${this.API_URL}/auth/logout`, { refreshToken: token }, { withCredentials: true })
      .pipe(catchError(() => of(null)))
      .subscribe();

    this.inMemoryAccessToken.set(null);
    localStorage.removeItem(this.USER_KEY);
    this.currentUser.set(null);
    this.router.navigate(['/login']);
  }

  getToken(): string | null {
    return this.inMemoryAccessToken();
  }

  private getStoredUser(): User | null {
    const raw = localStorage.getItem(this.USER_KEY);
    if (!raw) return null;
    try {
      return JSON.parse(raw);
    } catch {
      return null;
    }
  }
}

