# Mattei Grid - B2B Clinical Management Platform

Enterprise multi-tenant clinic management system for Electro-Homoeopathy practices based on PRD v2.4, ERD v4, and the Payment State Machine specification.

---

## Directory Structure

```
MatteiGrid/
├── backend/                  <-- ASP.NET Core (.NET 8) Modular Monolith
│   ├── MatteiGrid.sln        <-- Open in Visual Studio
│   ├── Directory.Build.props
│   ├── src/                  <-- Clean Architecture Domain & Application Modules
│   │   ├── MatteiGrid.Api/   <-- Startup API Project (Port: 5000 / Swagger: /swagger)
│   │   ├── MatteiGrid.Modules.Clinical/
│   │   ├── MatteiGrid.Modules.Prescriptions/
│   │   ├── MatteiGrid.Modules.Laboratory/
│   │   ├── MatteiGrid.Modules.Inventory/
│   │   ├── MatteiGrid.Modules.Billing/
│   │   ├── MatteiGrid.Modules.Patients/
│   │   ├── MatteiGrid.Modules.MateriaMedica/
│   │   ├── MatteiGrid.Modules.Appointments/
│   │   ├── MatteiGrid.Modules.Administration/
│   │   ├── MatteiGrid.Modules.Clinic/
│   │   ├── MatteiGrid.Modules.Reports/
│   │   ├── MatteiGrid.Modules.Notifications/
│   │   ├── MatteiGrid.Modules.Identity/
│   │   ├── MatteiGrid.Shared.Kernel/
│   │   └── MatteiGrid.Shared.Infrastructure/
│   └── tests/                <-- Automated Test Suites (25 passing tests)
│       ├── MatteiGrid.ArchitectureTests/
│       ├── MatteiGrid.UnitTests/
│       └── MatteiGrid.IntegrationTests/
│
└── frontend/                 <-- Angular 19 SPA (Port: 4200)
    ├── angular.json          <-- Open folder in VS Code
    ├── package.json
    ├── tsconfig.json
    └── src/
        └── app/
            ├── core/         <-- API, Auth, Models & SignalR Services
            ├── features/     <-- Consultation, Lab Queue, Inventory, Billing, Patients, Dashboard
            └── layout/       <-- Navigation shell, live tenant badge, role switcher
```

---

## Quick Start

### 1. Backend (.NET 8)
- **In Visual Studio**:
  1. Open `backend/MatteiGrid.sln`.
  2. Set `MatteiGrid.Api` as Startup Project.
  3. Press **F5** or **Ctrl+F5**.
- **In Terminal / CLI**:
  ```powershell
  cd backend
  dotnet run --project src/MatteiGrid.Api/MatteiGrid.Api.csproj
  ```
  API available at `http://localhost:5000` | Swagger at `http://localhost:5000/swagger`.

### 2. Frontend (Angular 19)
- **In VS Code**:
  1. Open folder `frontend/` (or right-click `frontend` and choose "Open with Code").
  2. Open integrated terminal (`Ctrl + \``).
  3. Run:
  ```bash
  npm start
  ```
  App available at `http://localhost:4200`.

### 3. Running All Tests
```powershell
dotnet test backend/MatteiGrid.sln
```
