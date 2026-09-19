import { Injectable, signal } from '@angular/core';
import { User, AuthResponse } from './models';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly TOKEN_KEY = 'mattei_grid_token';
  private readonly USER_KEY = 'mattei_grid_user';

  currentUser = signal<User | null>(this.getStoredUser());
  currentClinicId = signal<string>('a0000000-0000-0000-0000-000000000001');

  constructor() {
    if (!this.currentUser()) {
      // Default initial mock user (Doctor) for convenient local development and inspection
      this.setUser({
        id: 'd1000000-0000-0000-0000-000000000001',
        email: 'dr.mattei@clinic.com',
        fullName: 'Dr. Cesare Mattei',
        role: 'Doctor',
        permissions: ['Prescription.*', 'ClinicalNote.*', 'Patient.*', 'Consultation.*', 'MateriaMedica.*']
      }, 'mock-jwt-token');
    }
  }

  setUser(user: User, token: string) {
    localStorage.setItem(this.TOKEN_KEY, token);
    localStorage.setItem(this.USER_KEY, JSON.stringify(user));
    this.currentUser.set(user);
  }

  switchRole(roleName: 'Doctor' | 'Receptionist' | 'Lab Technician' | 'Administrator') {
    const userMap = {
      'Doctor': {
        id: 'd1000000-0000-0000-0000-000000000001',
        email: 'doctor@clinic.com',
        fullName: 'Dr. Cesare Mattei',
        role: 'Doctor',
        permissions: ['Prescription.*', 'ClinicalNote.*', 'Patient.*', 'Consultation.*', 'MateriaMedica.*']
      },
      'Receptionist': {
        id: 'd1000000-0000-0000-0000-000000000002',
        email: 'reception@clinic.com',
        fullName: 'Anita Sharma (Receptionist)',
        role: 'Receptionist',
        permissions: ['Patient.*', 'Appointment.*', 'Billing.*']
      },
      'Lab Technician': {
        id: 'd1000000-0000-0000-0000-000000000003',
        email: 'lab@clinic.com',
        fullName: 'Rahul Verma (Lab Tech)',
        role: 'Lab Technician',
        permissions: ['Laboratory.*']
      },
      'Administrator': {
        id: 'd1000000-0000-0000-0000-000000000004',
        email: 'admin@clinic.com',
        fullName: 'Admin User',
        role: 'Administrator',
        permissions: ['Admin.*', 'Inventory.*', 'Billing.*', 'Reports.*']
      }
    };

    const selected = userMap[roleName];
    this.setUser(selected, `token-${roleName.toLowerCase()}`);
  }

  logout() {
    localStorage.removeItem(this.TOKEN_KEY);
    localStorage.removeItem(this.USER_KEY);
    this.currentUser.set(null);
  }

  getToken(): string | null {
    return localStorage.getItem(this.TOKEN_KEY);
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
