using Bogus;
using HealthApp.Domain.Entities;
using HealthApp.Domain.Services;
using Microsoft.AspNetCore.Identity;

namespace HealthApp.Razor.Data;

public static class SeedData
{
    public static async Task Initialize(IServiceProvider serviceProvider)
    {
        var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        // Seed roles
        string[] roleNames = { "Admin", "Doctor", "Patient" };
        foreach (var roleName in roleNames)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }

        // Seed admin user
        var adminEmail = "admin@healthapp.com";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                FirstName = "System",
                LastName = "Admin",
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true
            };
            await userManager.CreateAsync(adminUser, "Admin@1234");
            await userManager.AddToRoleAsync(adminUser, "Admin");
        }

        // Seed sample doctors with associated ApplicationUser
        var specialties = new[] { "Cardiology", "Neurology", "Pediatrics", "Orthopedics" };
        var doctorFaker = new Faker<Doctor>()
            .RuleFor(d => d.FirstName, f => f.Name.FirstName())
            .RuleFor(d => d.LastName, f => f.Name.LastName())
            .RuleFor(d => d.PhoneNumber, f => f.Phone.PhoneNumber())
            .RuleFor(d => d.Email, (f, u) => f.Internet.Email(u.FirstName, u.LastName))
            .RuleFor(d => d.Specialization, f => f.PickRandom(specialties))
            .RuleFor(d => d.LicenseNumber, f => $"MD{f.Random.Number(10000, 99999)}");

        for (int i = 0; i < 10; i++)
        {
            var doctor = doctorFaker.Generate();

            var doctorUser = new ApplicationUser
            {
                FirstName = doctor.FirstName,
                LastName = doctor.LastName,
                Email = doctor.Email,
                UserName = doctor.Email,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(doctorUser, "Doctor@1234");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(doctorUser, "Doctor");

                doctor.UserId = doctorUser.Id;
                context.Doctors.Add(doctor);
            }
        }

        // Seed 1000 patients with associated ApplicationUser
        var patientFaker = new Faker<Patient>()
            .RuleFor(p => p.FirstName, f => f.Name.FirstName())
            .RuleFor(p => p.LastName, f => f.Name.LastName())
            .RuleFor(p => p.Email, (f, p) => f.Internet.Email(p.FirstName, p.LastName))
            .RuleFor(p => p.PhoneNumber, f => f.Phone.PhoneNumber())
            .RuleFor(p => p.Address, f => f.Address.FullAddress())
            .RuleFor(p => p.DateOfBirth, f => f.Date.Past(30, DateTime.Now.AddYears(-18)))
            .RuleFor(p => p.BloodType, f => f.PickRandom(new[] { "A+", "A-", "B+", "B-", "O+", "O-", "AB+", "AB-" }))
            .RuleFor(p => p.Allergies, f => f.Lorem.Word())
            .RuleFor(p => p.MedicalHistory, f => f.Lorem.Paragraph())
            .RuleFor(p => p.InsuranceInfo, f => f.Company.CompanyName());

        var patients = patientFaker.Generate(1000);
        foreach (var patient in patients)
        {
            var patientUser = new ApplicationUser
            {
                FirstName = patient.FirstName,
                LastName = patient.LastName,
                Email = patient.Email,
                UserName = patient.Email,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(patientUser, "Patient@1234");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(patientUser, "Patient");

                patient.UserId = patientUser.Id;
                context.Patients.Add(patient);
            }
        }

        await context.SaveChangesAsync();
    }
}