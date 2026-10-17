using MatteiGrid.Modules.Clinic.Domain;
using MatteiGrid.Modules.Identity.Domain;
using MatteiGrid.Modules.Inventory.Domain;
using MatteiGrid.Modules.MateriaMedica.Domain;
using MatteiGrid.Modules.Prescriptions.Domain;
using MatteiGrid.Shared.Infrastructure.Persistence;
using MatteiGrid.Shared.Kernel;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MatteiGrid.Api.Data;

public static class DatabaseSeeder
{
    public static readonly Guid DefaultClinicId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    public static readonly Guid DefaultDoctorId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public static async Task SeedAsync(
        AppDbContext db,
        UserManager<ApplicationUser>? userManager = null,
        RoleManager<IdentityRole<Guid>>? roleManager = null)
    {
        // 0. Identity Roles & Demo Users (AUTH-08: Seeded ONLY in Development mode)
        if (roleManager != null && userManager != null)
        {
            var roles = new[] { Roles.Doctor, Roles.Administrator, Roles.Receptionist, Roles.LabTechnician, Roles.SuperAdmin };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole<Guid>(role));
                }
            }

            var demoUsers = new (string Email, string Name, string Role)[]
            {
                ("doctor@clinic.com", "Dr. Cesare Mattei", Roles.Doctor),
                ("admin@clinic.com", "Clinic Administrator", Roles.Administrator),
                ("reception@clinic.com", "Anita Sharma (Receptionist)", Roles.Receptionist),
                ("lab@clinic.com", "Rahul Verma (Lab Tech)", Roles.LabTechnician),
                ("superadmin@matteigrid.com", "Platform SuperAdmin", Roles.SuperAdmin)
            };

            foreach (var (email, name, role) in demoUsers)
            {
                var user = await userManager.FindByEmailAsync(email);
                if (user == null)
                {
                    user = new ApplicationUser
                    {
                        Id = Guid.NewGuid(),
                        UserName = email,
                        Email = email,
                        EmailConfirmed = true,
                        FullName = name,
                        ClinicId = DefaultClinicId,
                        IsActive = true
                    };
                    var createResult = await userManager.CreateAsync(user, "Pass123!");
                    if (createResult.Succeeded)
                    {
                        await userManager.AddToRoleAsync(user, role);
                    }
                }

                // Ensure UserClinicMembership
                if (!await db.Set<UserClinicMembership>().AnyAsync(m => m.UserId == user.Id && m.ClinicId == DefaultClinicId))
                {
                    db.Set<UserClinicMembership>().Add(new UserClinicMembership
                    {
                        UserId = user.Id,
                        ClinicId = DefaultClinicId,
                        ClinicName = "Mattei Grid Central Clinic",
                        Role = role,
                        IsActive = true
                    });
                }
            }
            await db.SaveChangesAsync();
        }

        // 1. Clinic
        if (!await db.Set<Clinic>().AnyAsync(c => c.Id == DefaultClinicId))
        {
            db.Set<Clinic>().Add(new Clinic
            {
                Id = DefaultClinicId,
                Name = "Mattei Grid Central Clinic"
            });
            await db.SaveChangesAsync();
        }

        // 2. Staff
        if (!await db.Set<Staff>().AnyAsync(s => s.Id == DefaultDoctorId))
        {
            db.Set<Staff>().Add(new Staff
            {
                Id = DefaultDoctorId,
                ClinicId = DefaultClinicId,
                Name = "Dr. Cesare Mattei",
                Phone = "+919876543200",
                IsActive = true
            });
            await db.SaveChangesAsync();
        }

        // 3. Materia Medica Entries
        var mmEntries = new List<MateriaMedicaEntry>
        {
            new() { Code = "S1", Name = "Scrofoloso 1", Family = "Scrofoloso", Description = "Constitutional remedy for lymphatic and glandular metabolism.", Indications = "Lymphatic stasis, digestive sluggishness, scrofulous diathesis." },
            new() { Code = "S2", Name = "Scrofoloso 2", Family = "Scrofoloso", Description = "Deep glandular tissue regenerator and metabolic balancer.", Indications = "Chronic glandular swelling, scrofulous eczema." },
            new() { Code = "A1", Name = "Angioitico 1", Family = "Angioitico", Description = "Cardiovascular and circulatory regulator. Affects venous and arterial tone.", Indications = "Hypertension, hemorrhoids, venous stasis, palpitations." },
            new() { Code = "C1", Name = "Canceroso 1", Family = "Canceroso", Description = "Degenerative cell protector and connective tissue restorative.", Indications = "Fibroids, epithelial hyperplasia, cellular degeneration." },
            new() { Code = "P1", Name = "Pettorale 1", Family = "Pettorale", Description = "Bronchopulmonary drainage remedy and lung restorative.", Indications = "Bronchitis, chronic cough, respiratory catarrh." },
            new() { Code = "L1", Name = "Linfatico 1", Family = "Linfatico", Description = "Universal fluid distributor and lymphatic cleanser.", Indications = "Edema, lymphatic engorgement, lymphatic headaches." },
            new() { Code = "Ver1", Name = "Vermifugo 1", Family = "Vermifugo", Description = "Gastrointestinal flora harmonizer and antiparasitic.", Indications = "Intestinal irritation, worm infestation symptoms." },
            new() { Code = "F1", Name = "Febbrifugo 1", Family = "Febbrifugo", Description = "Thermoregulatory balancing agent for intermittent febrile states.", Indications = "Fevers, periodic neuralgia, hepatic inflammation." }
        };

        foreach (var entry in mmEntries)
        {
            if (!await db.Set<MateriaMedicaEntry>().AnyAsync(m => m.Code == entry.Code))
            {
                db.Set<MateriaMedicaEntry>().Add(entry);
            }
        }
        await db.SaveChangesAsync();

        // 4. Prescriptions Master Data: Dilutions
        var dilutionLabels = new[] { "1st Dilution (D1)", "2nd Dilution (D2)", "3rd Dilution (D3)", "6C", "30C", "200C" };
        foreach (var label in dilutionLabels)
        {
            if (!await db.Set<Dilution>().AnyAsync(d => d.ClinicId == DefaultClinicId && d.Label == label))
            {
                db.Set<Dilution>().Add(new Dilution
                {
                    ClinicId = DefaultClinicId,
                    Label = label
                });
            }
        }

        // 5. Prescriptions Master Data: Vehicles
        var vehicleNames = new[] { "Distilled Water (Aqua Destillata)", "Globules (Sugar of Milk)", "Diluted Alcohol (30%)", "Ointment Base" };
        foreach (var vName in vehicleNames)
        {
            if (!await db.Set<Vehicle>().AnyAsync(v => v.ClinicId == DefaultClinicId && v.Name == vName))
            {
                db.Set<Vehicle>().Add(new Vehicle
                {
                    ClinicId = DefaultClinicId,
                    Name = vName
                });
            }
        }
        await db.SaveChangesAsync();

        // 6. Medicines & Initial Inventory
        var existingMM = await db.Set<MateriaMedicaEntry>().ToListAsync();
        foreach (var mm in existingMM)
        {
            var med = await db.Set<Medicine>().FirstOrDefaultAsync(m => m.ClinicId == DefaultClinicId && m.Name == mm.Name);
            if (med == null)
            {
                med = new Medicine
                {
                    ClinicId = DefaultClinicId,
                    MateriaMedicaEntryId = mm.Id,
                    Name = mm.Name,
                    IsActive = true
                };
                db.Set<Medicine>().Add(med);
                await db.SaveChangesAsync();

                // Initial inventory of 100 units
                db.Set<InventoryItem>().Add(new InventoryItem
                {
                    ClinicId = DefaultClinicId,
                    MedicineId = med.Id,
                    QuantityOnHand = 100m,
                    ReorderLevel = 15m
                });
            }
        }
        await db.SaveChangesAsync();
    }
}
