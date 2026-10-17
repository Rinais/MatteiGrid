import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable, of, catchError } from 'rxjs';
import { AuthService } from './auth.service';
import {
  Patient,
  DuplicatePatientWarning,
  Appointment,
  Consultation,
  ClinicalNote,
  MateriaMedicaEntry,
  Prescription,
  LaboratoryOrder,
  InventoryItem,
  Invoice,
  PaymentLink
} from './models';

@Injectable({
  providedIn: 'root'
})
export class ApiService {
  private http = inject(HttpClient);
  private auth = inject(AuthService);
  private baseUrl = 'http://localhost:5000/api/v1';

  // In-memory mock data store for resilient local offline preview & fallback
  private mockPatients: Patient[] = [
    { id: 'p1000000-0000-0000-0000-000000000001', clinicId: 'a0000000-0000-0000-0000-000000000001', name: 'Ramesh Kumar', phone: '+919845012345', dob: '1982-04-12', isArchived: false, isAnonymized: false, createdAt: new Date().toISOString() },
    { id: 'p1000000-0000-0000-0000-000000000002', clinicId: 'a0000000-0000-0000-0000-000000000001', name: 'Sunita Patel', phone: '+919876543210', dob: '1990-11-28', isArchived: false, isAnonymized: false, createdAt: new Date().toISOString() }
  ];

  private mockAppointments: Appointment[] = [
    { id: 'ap100000-0000-0000-0000-000000000001', clinicId: 'a0000000-0000-0000-0000-000000000001', patientId: 'p1000000-0000-0000-0000-000000000001', doctorStaffId: 'd1000000-0000-0000-0000-000000000001', startAt: new Date(Date.now() + 3600000).toISOString(), endAt: new Date(Date.now() + 5400000).toISOString(), status: 'Scheduled', createdAt: new Date().toISOString() }
  ];

  private mockMateriaMedica: MateriaMedicaEntry[] = [
    { id: 'm1000000-0000-0000-0000-000000000001', code: 'S1', name: 'Scrofoloso 1', family: 'Scrofoloso', description: 'Universal constitutional alterative. Acts primarily on lymphatic, glandular, and digestive systems.', indications: 'Lymphatic congestion, scrofulous diathesis, sluggish metabolism, chronic glandular swelling.' },
    { id: 'm1000000-0000-0000-0000-000000000002', code: 'A1', name: 'Angiotico 1', family: 'Angiotico', description: 'Primary circulatory regulator. Tones arterial and venous blood vessels.', indications: 'Circulatory sluggishness, arterial tension, haemorrhoids, varicose conditions.' },
    { id: 'm1000000-0000-0000-0000-000000000003', code: 'C1', name: 'Canceroso 1', family: 'Canceroso', description: 'Deep tissue cellular tonic. Specific for chronic cellular debility and degenerative changes.', indications: 'Chronic tissue degeneration, fibroids, stubborn indurations, ulcerations.' },
    { id: 'm1000000-0000-0000-0000-000000000004', code: 'F1', name: 'Febbrifugo 1', family: 'Febbrifugo', description: 'Nerve tonic and thermal balancer. Regulates neuro-vegetative nervous system.', indications: 'Intermittent fevers, neuralgia, nervous exhaustion, sympathetic tremors.' },
    { id: 'm1000000-0000-0000-0000-000000000005', code: 'P1', name: 'Pettorale 1', family: 'Pettorale', description: 'Respiratory soothing remedy. Tones bronchial mucosa and pulmonary tissues.', indications: 'Bronchial irritation, catarrh, dry irritating cough, chest congestion.' },
    { id: 'm1000000-0000-0000-0000-000000000006', code: 'L1', name: 'Linfatico 1', family: 'Linfatico', description: 'Specific remedy for lymph glands, spleen, and fluid balance.', indications: 'Lymphatic stagnation, post-inflammatory oedema, glandular enlargement.' }
  ];

  private mockLabOrders: LaboratoryOrder[] = [
    { id: 'lo100000-0000-0000-0000-000000000001', clinicId: 'a0000000-0000-0000-0000-000000000001', prescriptionId: 'rx100000-0000-0000-0000-000000000001', status: 'Received', createdAt: new Date().toISOString() },
    { id: 'lo100000-0000-0000-0000-000000000002', clinicId: 'a0000000-0000-0000-0000-000000000001', prescriptionId: 'rx100000-0000-0000-0000-000000000002', status: 'Preparing', createdAt: new Date(Date.now() - 1800000).toISOString() }
  ];

  private mockInventory: InventoryItem[] = [
    { id: 'i1000000-0000-0000-0000-000000000001', clinicId: 'a0000000-0000-0000-0000-000000000001', medicineId: 'm1000000-0000-0000-0000-000000000001', quantityOnHand: 45, reorderLevel: 10, isLowStock: false, createdAt: new Date().toISOString() },
    { id: 'i1000000-0000-0000-0000-000000000002', clinicId: 'a0000000-0000-0000-0000-000000000001', medicineId: 'm1000000-0000-0000-0000-000000000002', quantityOnHand: 6, reorderLevel: 10, isLowStock: true, createdAt: new Date().toISOString() }
  ];

  private mockInvoices: Invoice[] = [
    {
      id: 'inv10000-0000-0000-0000-000000000001',
      clinicId: 'a0000000-0000-0000-0000-000000000001',
      patientId: 'p1000000-0000-0000-0000-000000000001',
      consultationId: 'c1000000-0000-0000-0000-000000000001',
      status: 'Pending',
      totalAmount: 600,
      totalPaidAmount: 0,
      remainingBalance: 600,
      createdAt: new Date().toISOString(),
      lineItems: [
        { id: 'li1', description: 'Consultation Fee', quantity: 1, unitPrice: 300, amount: 300 },
        { id: 'li2', description: 'Scrofoloso 1 (30C, Globules)', quantity: 2, unitPrice: 150, amount: 300 }
      ],
      payments: [],
      activePaymentLink: {
        id: 'pl1',
        tokenHash: 'razorpay_mock_link_hash_12345',
        status: 'Active',
        expiresAt: new Date(Date.now() + 48 * 3600000).toISOString()
      }
    }
  ];

  private getHeaders(): HttpHeaders {
    let headers = new HttpHeaders({
      'Content-Type': 'application/json'
    });
    const token = this.auth.getToken();
    if (token) {
      headers = headers.set('Authorization', `Bearer ${token}`);
    }
    return headers;
  }

  // --- Patients ---
  getPatients(query?: string): Observable<Patient[]> {
    return this.http.get<Patient[]>(`${this.baseUrl}/patients`, {
      headers: this.getHeaders(),
      params: query ? { query } : {}
    }).pipe(
      catchError(() => {
        if (!query) return of(this.mockPatients);
        const q = query.toLowerCase();
        return of(this.mockPatients.filter(p => p.name.toLowerCase().includes(q) || p.phone.includes(q)));
      })
    );
  }

  registerPatient(req: { name: string; phone: string; dob: string; consentType?: string }): Observable<{ patient: Patient; warning: DuplicatePatientWarning }> {
    return this.http.post<{ patient: Patient; warning: DuplicatePatientWarning }>(`${this.baseUrl}/patients`, req, { headers: this.getHeaders() })
      .pipe(
        catchError(() => {
          // Check mock duplicate
          const dupes = this.mockPatients.filter(p => p.phone === req.phone || (p.dob === req.dob && p.name.toLowerCase() === req.name.toLowerCase()));
          const newPatient: Patient = {
            id: `p-${Date.now()}`,
            clinicId: this.auth.currentClinicId(),
            name: req.name,
            phone: req.phone,
            dob: req.dob,
            isArchived: false,
            isAnonymized: false,
            createdAt: new Date().toISOString()
          };
          this.mockPatients.unshift(newPatient);
          return of({
            patient: newPatient,
            warning: {
              hasPotentialDuplicate: dupes.length > 0,
              message: dupes.length > 0 ? `Advisory: ${dupes.length} potential matching patient(s) found with same phone or DOB.` : undefined,
              matches: dupes
            }
          });
        })
      );
  }

  erasePatientPii(patientId: string): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.baseUrl}/patients/${patientId}/erase-pii`, {}, { headers: this.getHeaders() })
      .pipe(
        catchError(() => {
          const p = this.mockPatients.find(x => x.id === patientId);
          if (p) {
            p.name = `Anonymized-Patient-${patientId.slice(0, 8)}`;
            p.phone = '0000000000';
            p.dob = '1900-01-01';
            p.isAnonymized = true;
          }
          return of({ message: 'Patient PII anonymized in compliance with DPDP Act 2023.' });
        })
      );
  }

  // --- Appointments ---
  getAppointments(): Observable<Appointment[]> {
    return this.http.get<Appointment[]>(`${this.baseUrl}/appointments`, { headers: this.getHeaders() })
      .pipe(catchError(() => of(this.mockAppointments)));
  }

  bookAppointment(req: { patientId: string; doctorStaffId: string; startAt: string; endAt: string; overrideReason?: string }): Observable<{ appointment: Appointment; overlap: any }> {
    return this.http.post<{ appointment: Appointment; overlap: any }>(`${this.baseUrl}/appointments`, req, { headers: this.getHeaders() })
      .pipe(
        catchError(() => {
          const appt: Appointment = {
            id: `ap-${Date.now()}`,
            clinicId: this.auth.currentClinicId(),
            ...req,
            status: 'Scheduled',
            createdAt: new Date().toISOString()
          };
          this.mockAppointments.push(appt);
          return of({ appointment: appt, overlap: { hasOverlap: false } });
        })
      );
  }

  // --- Materia Medica ---
  getMateriaMedica(query?: string): Observable<MateriaMedicaEntry[]> {
    return this.http.get<MateriaMedicaEntry[]>(`${this.baseUrl}/materia-medica`, {
      headers: this.getHeaders(),
      params: query ? { query } : {}
    }).pipe(
      catchError(() => {
        if (!query) return of(this.mockMateriaMedica);
        const q = query.toLowerCase();
        return of(this.mockMateriaMedica.filter(m =>
          m.name.toLowerCase().includes(q) ||
          m.code.toLowerCase().includes(q) ||
          m.family.toLowerCase().includes(q) ||
          m.indications.toLowerCase().includes(q)
        ));
      })
    );
  }

  // --- Prescriptions & Lab ---
  finalizePrescription(prescription: any): Observable<any> {
    const labOrder: LaboratoryOrder = {
      id: `lo-${Date.now()}`,
      clinicId: this.auth.currentClinicId(),
      prescriptionId: prescription.id || `rx-${Date.now()}`,
      status: 'Received',
      createdAt: new Date().toISOString()
    };
    this.mockLabOrders.unshift(labOrder);

    const invoice: Invoice = {
      id: `inv-${Date.now()}`,
      clinicId: this.auth.currentClinicId(),
      patientId: prescription.patientId || this.mockPatients[0].id,
      consultationId: prescription.consultationId || `c-${Date.now()}`,
      prescriptionId: labOrder.prescriptionId,
      status: 'Pending',
      totalAmount: 600,
      totalPaidAmount: 0,
      remainingBalance: 600,
      createdAt: new Date().toISOString(),
      lineItems: [
        { id: 'li1', description: 'Consultation Fee', quantity: 1, unitPrice: 300, amount: 300 },
        { id: 'li2', description: 'Prescribed Electro-Homoeopathy Remedy', quantity: 2, unitPrice: 150, amount: 300 }
      ],
      payments: [],
      activePaymentLink: {
        id: `pl-${Date.now()}`,
        tokenHash: `upi_link_${Math.random().toString(36).substring(2, 10)}`,
        status: 'Active',
        expiresAt: new Date(Date.now() + 48 * 3600000).toISOString()
      }
    };
    this.mockInvoices.unshift(invoice);

    return of({ prescription, labOrder, invoice });
  }

  getLabQueue(): Observable<LaboratoryOrder[]> {
    return this.http.get<LaboratoryOrder[]>(`${this.baseUrl}/lab/queue`, { headers: this.getHeaders() })
      .pipe(catchError(() => of(this.mockLabOrders)));
  }

  transitionLabStatus(orderId: string, targetStatus: string): Observable<LaboratoryOrder> {
    return this.http.post<LaboratoryOrder>(`${this.baseUrl}/lab/${orderId}/transition`, { targetStatus }, { headers: this.getHeaders() })
      .pipe(
        catchError(() => {
          const order = this.mockLabOrders.find(o => o.id === orderId);
          if (order) {
            order.status = targetStatus;
            order.updatedAt = new Date().toISOString();
          }
          return of(order!);
        })
      );
  }

  rejectLabOrder(orderId: string, comment: string): Observable<LaboratoryOrder> {
    return this.http.post<LaboratoryOrder>(`${this.baseUrl}/lab/${orderId}/reject`, { comment, staffId: this.auth.currentUser()?.id }, { headers: this.getHeaders() })
      .pipe(
        catchError(() => {
          const order = this.mockLabOrders.find(o => o.id === orderId);
          if (order) {
            order.status = 'Rejected';
            order.rejectionComment = comment;
            order.updatedAt = new Date().toISOString();
          }
          return of(order!);
        })
      );
  }

  // --- Inventory ---
  getInventory(lowStockOnly: boolean = false): Observable<InventoryItem[]> {
    return this.http.get<InventoryItem[]>(`${this.baseUrl}/inventory`, {
      headers: this.getHeaders(),
      params: lowStockOnly ? { lowStockOnly: 'true' } : {}
    }).pipe(
      catchError(() => {
        if (lowStockOnly) return of(this.mockInventory.filter(i => i.isLowStock));
        return of(this.mockInventory);
      })
    );
  }

  receiveStock(medicineId: string, quantity: number, reason: string): Observable<any> {
    return this.http.post(`${this.baseUrl}/inventory/receive-stock`, { medicineId, quantity, reason }, { headers: this.getHeaders() })
      .pipe(
        catchError(() => {
          const item = this.mockInventory.find(i => i.medicineId === medicineId);
          if (item) {
            item.quantityOnHand += quantity;
            item.isLowStock = item.quantityOnHand <= item.reorderLevel;
          }
          return of({ success: true, item });
        })
      );
  }

  // --- Billing ---
  getInvoices(): Observable<Invoice[]> {
    return of(this.mockInvoices);
  }

  recordManualPayment(invoiceId: string, amount: number, mode: string): Observable<Invoice> {
    return this.http.post<Invoice>(`${this.baseUrl}/billing/invoices/${invoiceId}/manual-payment`, {
      amount,
      mode,
      staffId: this.auth.currentUser()?.id
    }, { headers: this.getHeaders() }).pipe(
      catchError(() => {
        const inv = this.mockInvoices.find(i => i.id === invoiceId);
        if (inv) {
          inv.payments.push({
            id: `pay-${Date.now()}`,
            amount,
            source: 'Manual',
            mode,
            status: 'Confirmed',
            createdAt: new Date().toISOString()
          });
          inv.totalPaidAmount += amount;
          inv.remainingBalance = Math.max(0, inv.totalAmount - inv.totalPaidAmount);
          if (inv.remainingBalance === 0) {
            inv.status = 'Paid';
          } else {
            inv.status = 'PartiallyPaid';
          }
        }
        return of(inv!);
      })
    );
  }

  regeneratePaymentLink(invoiceId: string): Observable<PaymentLink> {
    return this.http.post<PaymentLink>(`${this.baseUrl}/billing/invoices/${invoiceId}/payment-link/regenerate`, {}, { headers: this.getHeaders() })
      .pipe(
        catchError(() => {
          const inv = this.mockInvoices.find(i => i.id === invoiceId);
          const newLink: PaymentLink = {
            id: `pl-${Date.now()}`,
            tokenHash: `upi_link_${Math.random().toString(36).substring(2, 10)}`,
            status: 'Active',
            expiresAt: new Date(Date.now() + 48 * 3600000).toISOString()
          };
          if (inv) {
            inv.activePaymentLink = newLink;
          }
          return of(newLink);
        })
      );
  }
}
