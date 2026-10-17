import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { AuthService } from '../../core/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  template: `
    <div class="login-wrapper">
      <div class="login-card">
        <!-- Brand Header -->
        <header class="brand-header">
          <div class="brand-logo">
            <span class="logo-leaf" aria-hidden="true">🌿</span>
            <span class="brand-name">Mattei Grid</span>
          </div>
          <p class="brand-subtitle">Electro-Homoeopathy Practice Management Platform</p>
          <div class="tenant-pill">
            <span class="tenant-indicator" aria-hidden="true"></span>
            <span>Clinic Alpha &bull; Tenant Isolation Model A</span>
          </div>
        </header>

        <!-- Error Feedback -->
        @if (errorMessage()) {
          <div class="error-banner" role="alert" aria-live="assertive">
            <span class="error-icon" aria-hidden="true">⚠️</span>
            <span>{{ errorMessage() }}</span>
            <button type="button" class="error-dismiss" (click)="errorMessage.set(null)" aria-label="Dismiss alert">×</button>
          </div>
        }

        <!-- Credentials Form -->
        <form class="login-form" (ngSubmit)="onSubmit()" novalidate>
          <div class="form-group">
            <label for="email-input">Email Address</label>
            <input
              id="email-input"
              type="email"
              name="email"
              [(ngModel)]="email"
              placeholder="e.g. doctor@clinic.com"
              required
              autocomplete="username"
              [disabled]="isLoading()"
            />
          </div>

          <div class="form-group">
            <div class="password-label-row">
              <label for="password-input">Password</label>
              <span class="hint-text">Default demo password: <code>Pass123!</code></span>
            </div>
            <input
              id="password-input"
              type="password"
              name="password"
              [(ngModel)]="password"
              placeholder="••••••••"
              required
              autocomplete="current-password"
              [disabled]="isLoading()"
            />
          </div>

          <button
            type="submit"
            id="login-submit-btn"
            class="submit-btn"
            [disabled]="isLoading() || !email || !password"
          >
            @if (isLoading()) {
              <span class="btn-spinner" aria-hidden="true"></span>
              <span>Authenticating...</span>
            } @else {
              <span>Sign In to Clinic Workspace</span>
              <span class="btn-arrow" aria-hidden="true">→</span>
            }
          </button>
        </form>

        <div class="divider">
          <span>OR CHOOSE DEMO PROFILE</span>
        </div>

        <!-- 1-Click Demo Profiles -->
        <div class="demo-profiles-section">
          <p class="demo-title">Fast Developer & Clinical Role Preview:</p>
          <div class="demo-grid">
            <button
              type="button"
              class="demo-chip chip-doctor"
              (click)="quickLogin('Doctor')"
              [disabled]="isLoading()"
            >
              <div class="chip-icon">🩺</div>
              <div class="chip-text">
                <strong>Doctor</strong>
                <small>Dr. Cesare Mattei (Clinical & Rx)</small>
              </div>
            </button>

            <button
              type="button"
              class="demo-chip chip-reception"
              (click)="quickLogin('Receptionist')"
              [disabled]="isLoading()"
            >
              <div class="chip-icon">📋</div>
              <div class="chip-text">
                <strong>Receptionist</strong>
                <small>Anita Sharma (Registry & Billing)</small>
              </div>
            </button>

            <button
              type="button"
              class="demo-chip chip-lab"
              (click)="quickLogin('Lab Technician')"
              [disabled]="isLoading()"
            >
              <div class="chip-icon">🧪</div>
              <div class="chip-text">
                <strong>Lab Tech</strong>
                <small>Rahul Verma (Order Queue)</small>
              </div>
            </button>

            <button
              type="button"
              class="demo-chip chip-admin"
              (click)="quickLogin('Administrator')"
              [disabled]="isLoading()"
            >
              <div class="chip-icon">⚙️</div>
              <div class="chip-text">
                <strong>Clinic Admin</strong>
                <small>Admin User (Audits & Inventory)</small>
              </div>
            </button>

            <button
              type="button"
              class="demo-chip chip-superadmin"
              (click)="quickLogin('SuperAdmin')"
              [disabled]="isLoading()"
            >
              <div class="chip-icon">🌐</div>
              <div class="chip-text">
                <strong>SuperAdmin</strong>
                <small>Platform Admin (Multi-Tenant)</small>
              </div>
            </button>
          </div>
        </div>

        <footer class="login-footer">
          <span>Mattei Grid Enterprise v2.4 &bull; Modular Monolith Architecture</span>
        </footer>
      </div>
    </div>
  `,
  styles: [`
    .login-wrapper {
      min-height: 100vh;
      display: flex;
      align-items: center;
      justify-content: center;
      padding: 1.5rem;
      background: radial-gradient(circle at 50% 10%, #064e3b 0%, #0f172a 75%, #020617 100%);
      color: #f8fafc;
    }

    .login-card {
      width: 100%;
      max-width: 480px;
      background: rgba(15, 23, 42, 0.85);
      border: 1px solid rgba(52, 211, 153, 0.2);
      border-radius: 16px;
      padding: 2.25rem;
      box-shadow: 0 20px 40px -15px rgba(0, 0, 0, 0.7), 0 0 30px rgba(16, 185, 129, 0.08);
      backdrop-filter: blur(16px);
    }

    .brand-header {
      text-align: center;
      margin-bottom: 1.75rem;
    }

    .brand-logo {
      display: inline-flex;
      align-items: center;
      gap: 0.6rem;
      margin-bottom: 0.4rem;
    }

    .logo-leaf {
      font-size: 1.75rem;
      filter: drop-shadow(0 0 8px rgba(16, 185, 129, 0.6));
    }

    .brand-name {
      font-size: 1.75rem;
      font-weight: 800;
      letter-spacing: -0.025em;
      background: linear-gradient(135deg, #a7f3d0 0%, #10b981 50%, #34d399 100%);
      -webkit-background-clip: text;
      -webkit-text-fill-color: transparent;
    }

    .brand-subtitle {
      font-size: 0.85rem;
      color: #94a3b8;
      margin-bottom: 0.75rem;
    }

    .tenant-pill {
      display: inline-flex;
      align-items: center;
      gap: 0.45rem;
      padding: 0.25rem 0.75rem;
      background: rgba(6, 78, 59, 0.6);
      border: 1px solid rgba(52, 211, 153, 0.3);
      border-radius: 9999px;
      font-size: 0.75rem;
      color: #a7f3d0;
    }

    .tenant-indicator {
      width: 7px;
      height: 7px;
      border-radius: 50%;
      background: #34d399;
      box-shadow: 0 0 6px #34d399;
    }

    .error-banner {
      display: flex;
      align-items: center;
      gap: 0.6rem;
      padding: 0.75rem 1rem;
      margin-bottom: 1.25rem;
      border-radius: 8px;
      background: rgba(185, 28, 28, 0.25);
      border: 1px solid #ef4444;
      color: #fca5a5;
      font-size: 0.85rem;
    }

    .error-dismiss {
      margin-left: auto;
      background: none;
      border: none;
      color: #fca5a5;
      font-size: 1.25rem;
      line-height: 1;
      cursor: pointer;
    }

    .login-form {
      display: flex;
      flex-direction: column;
      gap: 1.1rem;
    }

    .form-group {
      display: flex;
      flex-direction: column;
      gap: 0.4rem;
    }

    .form-group label {
      font-size: 0.825rem;
      font-weight: 600;
      color: #cbd5e1;
    }

    .password-label-row {
      display: flex;
      justify-content: space-between;
      align-items: center;
    }

    .hint-text {
      font-size: 0.72rem;
      color: #64748b;
    }

    .hint-text code {
      color: #34d399;
      background: rgba(51, 65, 85, 0.5);
      padding: 0.1rem 0.3rem;
      border-radius: 3px;
    }

    .form-group input {
      background: #1e293b;
      border: 1px solid #334155;
      border-radius: 8px;
      padding: 0.7rem 0.9rem;
      color: #f8fafc;
      font-size: 0.9rem;
      transition: all 0.15s ease-in-out;
    }

    .form-group input:focus {
      outline: none;
      border-color: #10b981;
      box-shadow: 0 0 0 3px rgba(16, 185, 129, 0.2);
    }

    .submit-btn {
      display: flex;
      align-items: center;
      justify-content: center;
      gap: 0.5rem;
      margin-top: 0.4rem;
      padding: 0.75rem 1.2rem;
      background: #10b981;
      color: #064e3b;
      font-weight: 700;
      font-size: 0.925rem;
      border: none;
      border-radius: 8px;
      cursor: pointer;
      transition: all 0.15s ease-in-out;
    }

    .submit-btn:hover:not(:disabled) {
      background: #34d399;
      box-shadow: 0 0 15px rgba(16, 185, 129, 0.4);
      transform: translateY(-1px);
    }

    .submit-btn:disabled {
      opacity: 0.6;
      cursor: not-allowed;
    }

    .btn-spinner {
      width: 16px;
      height: 16px;
      border: 2px solid #064e3b;
      border-top-color: transparent;
      border-radius: 50%;
      animation: spin 0.8s linear infinite;
    }

    @keyframes spin {
      to { transform: rotate(360deg); }
    }

    .divider {
      position: relative;
      text-align: center;
      margin: 1.5rem 0 1rem;
    }

    .divider::before {
      content: "";
      position: absolute;
      top: 50%;
      left: 0;
      right: 0;
      height: 1px;
      background: #334155;
    }

    .divider span {
      position: relative;
      background: #0f172a;
      padding: 0 0.75rem;
      font-size: 0.68rem;
      font-weight: 700;
      letter-spacing: 0.05em;
      color: #64748b;
    }

    .demo-profiles-section {
      background: rgba(30, 41, 59, 0.5);
      border: 1px solid #334155;
      border-radius: 10px;
      padding: 1rem;
    }

    .demo-title {
      font-size: 0.75rem;
      color: #94a3b8;
      margin-bottom: 0.6rem;
      font-weight: 500;
    }

    .demo-grid {
      display: flex;
      flex-direction: column;
      gap: 0.5rem;
    }

    .demo-chip {
      display: flex;
      align-items: center;
      gap: 0.65rem;
      background: #1e293b;
      border: 1px solid #334155;
      border-radius: 8px;
      padding: 0.5rem 0.75rem;
      color: #f1f5f9;
      text-align: left;
      cursor: pointer;
      transition: all 0.15s ease-in-out;
    }

    .demo-chip:hover:not(:disabled) {
      background: #334155;
      border-color: #10b981;
      transform: translateX(3px);
    }

    .chip-icon {
      font-size: 1.1rem;
    }

    .chip-text {
      display: flex;
      flex-direction: column;
    }

    .chip-text strong {
      font-size: 0.8rem;
      color: #34d399;
    }

    .chip-text small {
      font-size: 0.7rem;
      color: #94a3b8;
    }

    .login-footer {
      text-align: center;
      margin-top: 1.5rem;
      font-size: 0.7rem;
      color: #475569;
    }
  `]
})
export class LoginComponent {
  private auth = inject(AuthService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);

  email = 'doctor@clinic.com';
  password = 'Pass123!';
  isLoading = signal<boolean>(false);
  errorMessage = signal<string | null>(null);

  onSubmit() {
    if (!this.email || !this.password) return;

    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.auth.login(this.email, this.password).subscribe({
      next: (success) => {
        this.isLoading.set(false);
        if (success) {
          const returnUrl = this.route.snapshot.queryParams['returnUrl'] || '/dashboard';
          this.router.navigateByUrl(returnUrl);
        } else {
          this.errorMessage.set('Invalid credentials. Check email or use a Demo Profile below.');
        }
      },
      error: () => {
        this.isLoading.set(false);
        this.errorMessage.set('Unable to complete login request. Please try again.');
      }
    });
  }

  quickLogin(roleKey: 'Doctor' | 'Receptionist' | 'Lab Technician' | 'Administrator' | 'SuperAdmin') {
    const demo = this.auth.DEMO_USERS[roleKey];
    if (demo) {
      this.email = demo.defaultEmail;
      this.password = 'Pass123!';
      this.onSubmit();
    }
  }
}
