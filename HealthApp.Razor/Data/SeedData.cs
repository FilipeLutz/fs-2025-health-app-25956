using Bogus;
using HealthApp.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace HealthApp.Razor.Data;

public static class SeedData
{
    public static async Task Initialize(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILogger<Program>>();

        try
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

            // Apply pending migrations first
            await context.Database.MigrateAsync();
            logger.LogInformation("Database migrations applied");

            // 1. Seed roles first
            await SeedRoles(roleManager);

            // 2. Seed admin user
            var adminUser = await SeedAdminUser(userManager, context);

            // 3. Seed doctors (with their roles)
            await SeedDoctors(userManager, context);

            // 4. Seed patients (with their roles)
            await SeedPatients(userManager, context, adminUser.Id, logger);

            // 5. Seed appointments last
            await SeedAppointments(context, logger);

            logger.LogInformation("Database seeding completed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding the database");
            throw;
        }
    }

    private static async Task SeedRoles(RoleManager<IdentityRole> roleManager)
    {
        string[] roleNames = { "Admin", "Doctor", "Patient" };
        foreach (var roleName in roleNames)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }
    }

    private static async Task<IdentityUser> SeedAdminUser(UserManager<IdentityUser> userManager, ApplicationDbContext context)
    {
        const string adminEmail = "admin@healthapp.com";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);

        if (adminUser == null)
        {
            adminUser = new IdentityUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true
            };

            await userManager.CreateAsync(adminUser, "Admin123!");
            await userManager.AddToRoleAsync(adminUser, "Admin");

            context.Patients.Add(new Patient
            {
                UserId = adminUser.Id,
                Name = "Admin User",
                DateOfBirth = new DateTime(1980, 1, 1),
                PhoneNumber = "1234567890",
                Email = adminEmail // Added email to match earlier requirements
            });

            await context.SaveChangesAsync();
        }

        return adminUser;
    }

    private static async Task SeedDoctors(UserManager<IdentityUser> userManager, ApplicationDbContext context)
    {
        if (await context.Doctors.AnyAsync()) return;

        var specialties = new[] { "Cardiology", "Neurology", "Pediatrics", "Orthopedics", "Dermatology" };

        var doctorFaker = new Faker<Doctor>()
            .RuleFor(d => d.Name, f => f.Name.FullName())
            .RuleFor(d => d.Specialization, f => f.PickRandom(specialties))
            .RuleFor(d => d.Bio, f => f.Lorem.Paragraph())
            .RuleFor(d => d.LicenseNumber, f => f.Random.AlphaNumeric(10))
            .RuleFor(d => d.YearsOfExperience, f => f.Random.Number(1, 30))
            .RuleFor(d => d.HospitalAffiliation, f => f.Company.CompanyName())
            .RuleFor(d => d.Email, (f, d) => $"{d.Name.Replace(" ", "").ToLower()}@healthapp.com");

        var doctors = doctorFaker.Generate(10);

        foreach (var doctor in doctors)
        {
            var doctorUser = new IdentityUser
            {
                UserName = doctor.Email,
                Email = doctor.Email,
                EmailConfirmed = true
            };

            await userManager.CreateAsync(doctorUser, "Doctor123!");
            await userManager.AddToRoleAsync(doctorUser, "Doctor");

            doctor.UserId = doctorUser.Id;
            context.Doctors.Add(doctor);
        }

        // Save doctors first to generate IDs
        await context.SaveChangesAsync();

        // Now add schedules with valid doctor IDs
        foreach (var doctor in context.Doctors)
        {
            for (int i = 0; i < 5; i++) // 5 days per week
            {
                context.Schedules.Add(new Schedule
                {
                    DoctorId = doctor.Id,
                    DayOfWeek = (DayOfWeek)((i + 1) % 7), // Mon-Fri
                    StartTime = TimeSpan.FromHours(9),
                    EndTime = TimeSpan.FromHours(17),
                    MaxAppointments = 8
                });
            }
        }

        await context.SaveChangesAsync();
    }

    private static async Task SeedPatients(UserManager<IdentityUser> userManager,
    ApplicationDbContext context, string adminUserId, ILogger<Program> logger)
    {
        if (await context.Patients.AnyAsync(p => p.UserId != adminUserId))
        {
            logger.LogInformation("Patients already seeded");
            return;
        }

        var patientFaker = new Faker<Patient>()
            .RuleFor(p => p.Name, f => {
                var name = f.Name.FullName();
                while (name.Length > 100) // Ensure name fits in database column
                    name = f.Name.FullName();
                return name;
            })
            .RuleFor(p => p.DateOfBirth, f => f.Date.Between(DateTime.Now.AddYears(-80), DateTime.Now.AddYears(-18)))
            .RuleFor(p => p.PhoneNumber, f => {
                var phone = f.Phone.PhoneNumber();
                return phone.Length > 20 ? phone[..20] : phone; // Truncate if too long
            })
            .RuleFor(p => p.Email, (f, p) => {
                var email = $"{p.Name.Replace(" ", "").ToLower()}@patient.com";
                return email.Length > 256 ? email[..256] : email; // Ensure email fits
            });

        var patients = patientFaker.Generate(10); // Start with small number for testing

        foreach (var patient in patients)
        {
            try
            {
                // Ensure email is unique
                if (await userManager.FindByEmailAsync(patient.Email) != null)
                {
                    patient.Email = $"{patient.Name.Replace(" ", "")}{Guid.NewGuid().ToString()[..4]}@patient.com";
                }

                var patientUser = new IdentityUser
                {
                    UserName = patient.Email,
                    Email = patient.Email,
                    EmailConfirmed = true
                };

                // Create user with stronger password
                var result = await userManager.CreateAsync(patientUser, "StrongPatient123!");
                if (!result.Succeeded)
                {
                    logger.LogError("Failed to create user {Email}: {Errors}",
                        patient.Email, string.Join(", ", result.Errors.Select(e => e.Description)));
                    continue; // Skip to next patient instead of failing
                }

                // Add to Patient role
                result = await userManager.AddToRoleAsync(patientUser, "Patient");
                if (!result.Succeeded)
                {
                    logger.LogWarning("Failed to add role to {Email}: {Errors}",
                        patient.Email, string.Join(", ", result.Errors.Select(e => e.Description)));
                }

                patient.UserId = patientUser.Id;
                context.Patients.Add(patient);

                // Save after each patient to isolate errors
                await context.SaveChangesAsync();
                logger.LogInformation("Created patient {Email}", patient.Email);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating patient {Email}", patient.Email);
            }
        }
    }

    private static async Task SeedAppointments(ApplicationDbContext context, ILogger<Program> logger)
    {
        if (await context.Appointments.AnyAsync())
        {
            logger.LogInformation("Appointments already seeded");
            return;
        }

        // Verify we have doctors and patients
        var doctors = await context.Doctors.ToListAsync();
        var patients = await context.Patients.ToListAsync();

        if (doctors.Count == 0 || patients.Count == 0)
        {
            logger.LogWarning("Cannot seed appointments - need at least 1 doctor and 1 patient");
            return;
        }

        var appointmentFaker = new Faker<Appointment>()
            .RuleFor(a => a.PatientId, f => f.PickRandom(patients).Id)
            .RuleFor(a => a.DoctorId, f => f.PickRandom(doctors).Id)
            .RuleFor(a => a.AppointmentDateTime, f => f.Date.Between(DateTime.Now.AddDays(-30), DateTime.Now.AddDays(30)))
            .RuleFor(a => a.EndDateTime, (f, a) => a.AppointmentDateTime.AddMinutes(30))
            .RuleFor(a => a.Status, f => f.PickRandom("Pending", "Approved", "Completed"))
            .RuleFor(a => a.Reason, f => f.Lorem.Sentence())
            .RuleFor(a => a.Notes, f => f.Lorem.Paragraph());

        // Generate in smaller batches
        for (int i = 0; i < 5; i++)
        {
            var appointments = appointmentFaker.Generate(20); // 20 appointments per batch
            await context.Appointments.AddRangeAsync(appointments);
            await context.SaveChangesAsync();
            logger.LogInformation("Seeded batch of {Count} appointments", appointments.Count);
        }
    }
}