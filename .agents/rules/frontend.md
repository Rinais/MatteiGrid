# Frontend Development Rules (Angular 19 SPA)

## 1. Component Architecture
- Use **Standalone Components** exclusively (`standalone: true`).
- Use modern Angular control flow syntax (`@if`, `@for`, `@switch`) instead of legacy `*ngIf`, `*ngFor`.
- Prefer Angular **Signals** (`signal()`, `computed()`, `effect()`) for reactive component state instead of excessive RxJS BehaviorSubjects where appropriate.
- Use `inject()` function for dependency injection rather than constructor injection boilerplate.

## 2. API Communication & Authentication
- All backend HTTP requests must go through `src/app/core/api.service.ts` or dedicated feature services.
- Never manually assemble Authorization headers in individual components; rely on `auth.interceptor.ts` (or equivalent HTTP interceptor) to attach bearer tokens automatically.
- Handle 401 Unauthorized globally by redirecting to `/login` or attempting silent refresh.

## 3. Real-Time SignalR Hubs
- Manage SignalR connections (such as `LabQueueHub` and `ConsultationHub`) through singletons in `core/`.
- Ensure connection lifecycles are cleanly terminated or unsubscribed in `ngOnDestroy()` to avoid memory leaks and ghost subscriptions.

## 4. UI & Form Validation
- Use Angular Reactive Forms (`FormBuilder`, `FormGroup`, `FormControl`) with explicit validators for user input.
- Show clear, user-friendly validation messages on blur or submit.
- Maintain consistent UI styling using Tailwind CSS utility classes.
- Ensure all clinical input forms (prescriptions, remedy selection, lab orders) are strictly typed.
