import { Injectable, signal, inject } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { AuthService } from './auth.service';

@Injectable({
  providedIn: 'root'
})
export class SignalRService {
  private auth = inject(AuthService);
  private labHubConnection?: signalR.HubConnection;
  private dashboardHubConnection?: signalR.HubConnection;

  isConnected = signal<boolean>(false);
  latestLabEvent = signal<string | null>(null);

  startConnections() {
    const clinicId = this.auth.currentClinicId();

    this.labHubConnection = new signalR.HubConnectionBuilder()
      .withUrl('http://localhost:5000/hubs/lab-queue', {
        skipNegotiation: true,
        transport: signalR.HttpTransportType.WebSockets
      })
      .withAutomaticReconnect()
      .build();

    this.labHubConnection.on('OrderReceived', (orderId: string, prescriptionId: string) => {
      this.latestLabEvent.set(`New order #${orderId.slice(0, 8)} received for prescription #${prescriptionId.slice(0, 8)}`);
    });

    this.labHubConnection.on('OrderStatusChanged', (orderId: string, newStatus: string) => {
      this.latestLabEvent.set(`Order #${orderId.slice(0, 8)} transitioned to '${newStatus}'`);
    });

    this.labHubConnection.on('OrderRejected', (orderId: string, reason: string) => {
      this.latestLabEvent.set(`Order #${orderId.slice(0, 8)} REJECTED: ${reason}`);
    });

    this.labHubConnection.start()
      .then(() => {
        this.isConnected.set(true);
        this.labHubConnection?.invoke('JoinClinicGroup', clinicId);
      })
      .catch(() => {
        // Safe offline mode
        this.isConnected.set(false);
      });
  }

  stopConnections() {
    this.labHubConnection?.stop();
    this.isConnected.set(false);
  }
}
