using System.Text;
using MatteiGrid.Api.Authorization;
using MatteiGrid.Api.Endpoints;
using MatteiGrid.Api.Hubs;
using MatteiGrid.Api.Middleware;
using MatteiGrid.Modules.Appointments.Contracts;
using MatteiGrid.Modules.Appointments.Services;
using MatteiGrid.Modules.Billing.Contracts;
using MatteiGrid.Modules.Billing.Services;
using MatteiGrid.Modules.Clinical.Contracts;
using MatteiGrid.Modules.Clinical.Services;
using MatteiGrid.Modules.Identity.Contracts;
using MatteiGrid.Modules.Identity.Domain;
using MatteiGrid.Modules.Identity.Services;
using MatteiGrid.Modules.Inventory.Contracts;
using MatteiGrid.Modules.Inventory.Services;
using MatteiGrid.Modules.Laboratory.Contracts;
using MatteiGrid.Modules.Laboratory.Services;
using MatteiGrid.Modules.MateriaMedica.Contracts;
using MatteiGrid.Modules.MateriaMedica.Services;
using MatteiGrid.Modules.Patients.Contracts;
using MatteiGrid.Modules.Patients.Services;
using MatteiGrid.Modules.Prescriptions.Contracts;
using MatteiGrid.Modules.Prescriptions.Services;
using MatteiGrid.Shared.Infrastructure.Audit;
using MatteiGrid.Shared.Infrastructure.Logging;
using MatteiGrid.Shared.Infrastructure.Persistence;
using MatteiGrid.Shared.Infrastructure.Tenancy;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Events;

namespace MatteiGrid.Api;

public class Program
{
    public static void Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.Console(outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u4}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.SynchronizedDualFile(
                logDirectory: "logs",
                maxLinesPerFile: 10_000,
                retentionDays: 14)
            .CreateLogger();

        try
        {
            Log.Information("[LIFECYCLE] Starting Mattei Grid API application host...");
            var builder = WebApplication.CreateBuilder(args);
            builder.Host.UseSerilog();

        // --- DATABASE & TENANCY ---
        builder.Services.AddScoped<ICurrentTenantContext, CurrentTenantContext>();
        builder.Services.AddScoped<TenantSaveChangesInterceptor>();
        builder.Services.AddScoped<IAuditService, AuditService>();

        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
        builder.Services.AddDbContext<AppDbContext>((sp, options) =>
        {
            var interceptor = sp.GetRequiredService<TenantSaveChangesInterceptor>();
            options.AddInterceptors(interceptor);

            var useInMemory = builder.Configuration.GetValue<bool>("UseInMemoryDatabase", false);
            if (!string.IsNullOrWhiteSpace(connectionString) && !useInMemory)
            {
                options.UseNpgsql(connectionString);
            }
            else
            {
                // In-memory / development fallback if configured or no connection string
                options.UseInMemoryDatabase("MatteiGridDevDb");
            }
        });

        // --- IDENTITY & JWT ---
        builder.Services.AddIdentityCore<ApplicationUser>(options =>
        {
            // Security hardening: strong password policy & lockout
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequiredLength = 8;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.AllowedForNewUsers = true;
        })
        .AddRoles<IdentityRole<Guid>>()
        .AddEntityFrameworkStores<AppDbContext>();

        builder.Services.AddSingleton<JwtTokenService>();
        builder.Services.AddLoggedScoped<IIdentityModule, IdentityModule>();

        var jwtKey = builder.Configuration["Jwt:Key"] ?? "MatteiGridDefaultSecretKey123456789012345!";
        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "MatteiGridApi",
                    ValidateAudience = true,
                    ValidAudience = builder.Configuration["Jwt:Audience"] ?? "MatteiGridClients",
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                    ClockSkew = TimeSpan.Zero
                };

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;
                        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                        {
                            context.Token = accessToken;
                        }
                        return Task.CompletedTask;
                    }
                };
            });

        // AUTH-03: Deny-by-default fallback policy - any unannotated endpoint requires an authenticated user
        builder.Services.AddAuthorization(options =>
        {
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
        });

        builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

        // --- MODULE SERVICES (AOP Method Entry/Exit & Performance Logging) ---
        builder.Services.AddLoggedScoped<IPatientService, PatientService>();
        builder.Services.AddLoggedScoped<IAppointmentService, AppointmentService>();
        builder.Services.AddLoggedScoped<IClinicalService, ClinicalService>();
        builder.Services.AddLoggedScoped<IMateriaMedicaService, MateriaMedicaService>();
        builder.Services.AddLoggedScoped<IPrescriptionService, PrescriptionService>();
        builder.Services.AddLoggedScoped<ILaboratoryService, LaboratoryService>();
        builder.Services.AddLoggedScoped<IInventoryService, InventoryService>();
        builder.Services.AddLoggedScoped<IBillingService, BillingService>();
        builder.Services.AddScoped<MatteiGrid.Shared.Kernel.IPaymentLockChecker, MatteiGrid.Modules.Billing.Services.PaymentLockChecker>();

        // --- SIGNALR & REAL-TIME ---
        builder.Services.AddSignalR();

        // --- CORS ---
        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.WithOrigins("http://localhost:4200", "http://127.0.0.1:4200")
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });

        // --- SWAGGER ---
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        var app = builder.Build();

        // --- MIDDLEWARE PIPELINE ---
        // 1. Global Exception Handler: catches any unhandled fatal crash and returns RFC 7807 ProblemDetails
        app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

        // 2. Security headers
        app.Use(async (context, next) =>
        {
            context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
            context.Response.Headers.Append("X-Frame-Options", "DENY");
            context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
            await next();
        });

        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Mattei Grid API v2.4");
            c.RoutePrefix = "swagger";
        });

        app.UseCors();
        app.UseAuthentication();
        app.UseMiddleware<TenantResolutionMiddleware>();

        // 3. Request & Response Logging: logs HTTP method, path, latency, status code with resolved tenant context
        app.UseMiddleware<RequestResponseLoggingMiddleware>();

        app.UseAuthorization();

        // Map Endpoints & Hubs
        app.MapMatteiGridEndpoints();
        app.MapHub<LabQueueHub>("/hubs/lab-queue");
        app.MapHub<DashboardHub>("/hubs/dashboard");

        app.MapGet("/", () => Results.Ok(new
        {
            app = "Mattei Grid API",
            status = "Operational",
            version = "2.4",
            documentation = "/swagger"
        })).AllowAnonymous();

        // AUTH-08: Gate database seeding strictly to Development environment
        if (app.Environment.IsDevelopment())
        {
            using var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
            MatteiGrid.Api.Data.DatabaseSeeder.SeedAsync(db, userManager, roleManager).GetAwaiter().GetResult();
        }

        // Automatically launch default browser to Swagger on startup in Development
        app.Lifetime.ApplicationStarted.Register(() =>
        {
            if (app.Environment.IsDevelopment())
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await Task.Delay(600);
                        var swaggerUrl = "http://localhost:5000/swagger";
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = swaggerUrl,
                            UseShellExecute = true
                        });
                    }
                    catch
                    {
                        // Fallback silently if running in headless/container environment
                    }
                });
            }
        });

        app.Run();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "[FATAL] [LIFECYCLE] Host terminated unexpectedly: {Message}", ex.Message);
            throw;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}
