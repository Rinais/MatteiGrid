using System.Text;
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
using MatteiGrid.Shared.Infrastructure.Persistence;
using MatteiGrid.Shared.Infrastructure.Tenancy;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace MatteiGrid.Api;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // --- DATABASE & TENANCY ---
        builder.Services.AddScoped<ICurrentTenantContext, CurrentTenantContext>();
        builder.Services.AddScoped<TenantSaveChangesInterceptor>();

        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
        builder.Services.AddDbContext<AppDbContext>((sp, options) =>
        {
            var interceptor = sp.GetRequiredService<TenantSaveChangesInterceptor>();
            options.AddInterceptors(interceptor);

            if (!string.IsNullOrWhiteSpace(connectionString) && !connectionString.Contains("localhost"))
            {
                options.UseNpgsql(connectionString);
            }
            else
            {
                // In-memory / development fallback if Postgres isn't directly up
                options.UseInMemoryDatabase("MatteiGridDevDb");
            }
        });

        // --- IDENTITY & JWT ---
        builder.Services.AddIdentityCore<ApplicationUser>(options =>
        {
            options.Password.RequireDigit = false;
            options.Password.RequireLowercase = false;
            options.Password.RequireUppercase = false;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequiredLength = 6;
        })
        .AddRoles<IdentityRole<Guid>>()
        .AddEntityFrameworkStores<AppDbContext>();

        builder.Services.AddSingleton<JwtTokenService>();
        builder.Services.AddScoped<IIdentityModule, IdentityModule>();

        var jwtKey = builder.Configuration["Jwt:Key"] ?? "MatteiGridDefaultSecretKey123456789012345!";
        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
                };
            });

        builder.Services.AddAuthorization();

        // --- MODULE SERVICES ---
        builder.Services.AddScoped<IPatientService, PatientService>();
        builder.Services.AddScoped<IAppointmentService, AppointmentService>();
        builder.Services.AddScoped<IClinicalService, ClinicalService>();
        builder.Services.AddScoped<IMateriaMedicaService, MateriaMedicaService>();
        builder.Services.AddScoped<IPrescriptionService, PrescriptionService>();
        builder.Services.AddScoped<ILaboratoryService, LaboratoryService>();
        builder.Services.AddScoped<IInventoryService, InventoryService>();
        builder.Services.AddScoped<IBillingService, BillingService>();
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

        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Mattei Grid API v2.4");
            c.RoutePrefix = "swagger";
        });

        app.UseCors();
        app.UseAuthentication();
        app.UseMiddleware<TenantResolutionMiddleware>();
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
        }));

        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            MatteiGrid.Api.Data.DatabaseSeeder.SeedAsync(db).GetAwaiter().GetResult();
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
}
