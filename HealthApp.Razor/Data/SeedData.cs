using Bogus;
using HealthApp.Domain.Entities;
using HealthApp.Domain.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace HealthApp.Razor.Data;

public static class SeedData
{
    public static void Initialize(IServiceProvider serviceProvider)
    {
        var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();

        // Seed 1000 patients
        var patientFaker = new Faker<IdentityUser>()
            .RuleFor(u => u.Email, (f, u) => f.Internet.Email(f.Name.FirstName(), f.Name.LastName()))
            .RuleFor(u => u.UserName, (f, u) => u.Email);

        var patients = patientFaker.Generate(1000);

        foreach (var patient in patients)
        {
            var result = userManager.CreateAsync(patient, "Patient@123").Result;
            if (result.Succeeded)
            {
                userManager.AddToRoleAsync(patient, "Patient").Wait();
            }
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

            context.Set<Patient>().Add(new Patient
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
        if (await context.Set<Doctor>().AnyAsync()) return;

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
            context.Set<Doctor>().Add(doctor);
        }

        await context.SaveChangesAsync();

        foreach (var doctor in context.Set<Doctor>())
        {
            for (int i = 0; i < 5; i++) 
            {
                context.Set<Schedule>().Add(new Schedule
                {
                    DoctorId = doctor.Id,
                    DayOfWeek = (DayOfWeek)((i + 1) % 7), 
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
        if (await context.Set<Patient>().AnyAsync(p => p.UserId != adminUserId))
        {
            logger.LogInformation("Patients already seeded");
            return;
        }

        var patientFaker = new Faker<Patient>()
            .RuleFor(p => p.Name, f => {
                var name = f.Name.FullName();
                while (name.Length > 100)
                    name = f.Name.FullName();
                return name;
            })
            .RuleFor(p => p.DateOfBirth, f => f.Date.Between(DateTime.Now.AddYears(-80), DateTime.Now.AddYears(-18)))
            .RuleFor(p => p.PhoneNumber, f => {
                var phone = f.Phone.PhoneNumber();
                return phone.Length > 20 ? phone[..20] : phone;
            })
            .RuleFor(p => p.Email, (f, p) => {
                var email = $"{p.Name.Replace(" ", "").ToLower()}@patient.com";
                return email.Length > 256 ? email[..256] : email; 
            });

        var patients = patientFaker.Generate(10); 

        foreach (var patient in patients)
        {
            try
            {
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

                var result = await userManager.CreateAsync(patientUser, "StrongPatient123!");
                if (!result.Succeeded)
                {
                    logger.LogError("Failed to create user {Email}: {Errors}",
                        patient.Email, string.Join(", ", result.Errors.Select(e => e.Description)));
                    continue;
                }

                result = await userManager.AddToRoleAsync(patientUser, "Patient");
                if (!result.Succeeded)
                {
                    logger.LogWarning("Failed to add role to {Email}: {Errors}",
                        patient.Email, string.Join(", ", result.Errors.Select(e => e.Description)));
                }

                patient.UserId = patientUser.Id;
                context.Set<Patient>().Add(patient);

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
        if (await context.Set<Appointment>().AnyAsync())
        {
            logger.LogInformation("Appointments already seeded");
            return;
        }

        var doctors = await context.Set<Doctor>().ToListAsync();
        var patients = await context.Set<Patient>().ToListAsync();

        if (doctors.Count == 0 || patients.Count == 0)
        {
            logger.LogWarning("Cannot seed appointments - need at least 1 doctor and 1 patient");
            return;
        }

        var appointmentFaker = new Faker<Appointment>()
            .RuleFor(a => a.PatientId, (f, a) => f.PickRandom(patients).Id)
            .RuleFor(a => a.DoctorId, f => f.PickRandom(doctors).Id)
            .RuleFor(a => a.AppointmentDateTime, f => f.Date.Between(DateTime.Now.AddDays(-30), DateTime.Now.AddDays(30)))
            .RuleFor(a => a.EndDateTime, (f, a) => a.AppointmentDateTime.AddMinutes(30))
            .RuleFor(a => a.Status, f => f.PickRandom("Pending", "Approved", "Completed"))
            .RuleFor(a => a.Reason, f => f.Lorem.Sentence())
            .RuleFor(a => a.Notes, f => f.Lorem.Paragraph());

        for (int i = 0; i < 5; i++)
        {
            var appointments = appointmentFaker.Generate(20); // 20 appointments per batch
            await context.Set<Appointment>().AddRangeAsync(appointments);
            await context.SaveChangesAsync();
            logger.LogInformation("Seeded batch of {Count} appointments", appointments.Count);
        }
    }
}