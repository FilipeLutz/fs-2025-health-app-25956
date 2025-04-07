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
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true
            };
            await userManager.CreateAsync(adminUser, "Admin@1234");
            await userManager.AddToRoleAsync(adminUser, "Admin");
        }

        // Seed sample doctors
        var doctorFaker = new Faker<Doctor>()
            .RuleFor(d => d.UserId, f => f.Random.Guid().ToString())
            .RuleFor(d => d.Id, f => f.IndexFaker + 1)
            .RuleFor(u => u.FirstName, f => f.Name.FirstName())
            .RuleFor(u => u.LastName, f => f.Name.LastName())
            .RuleFor(u => u.Email, (f, u) => f.Internet.Email(u.FirstName, u.LastName));

        var specialties = new[] { "Cardiology", "Neurology", "Pediatrics", "Orthopedics" };

        for (int i = 0; i < 10; i++)
        {
            var doctorUser = doctorFaker.Generate();

            var doctor = new Doctor
            {
                Id = doctorUser.Id,
                UserId = doctorUser.UserId,
                FirstName = doctorUser.FirstName,
                LastName = doctorUser.LastName,
                Email = doctorUser.Email,
                PhoneNumber = doctorUser.PhoneNumber,
                Specialization = specialties[Random.Shared.Next(specialties.Length)],
                LicenseNumber = $"MD{Random.Shared.Next(10000, 99999)}"
            };
            context.Doctors.Add(doctor);
        }

        // Seed 1000 patients
        var patientFaker = new Faker<Patient>()
            .RuleFor(p => p.Id, f => f.IndexFaker + 1)
            .RuleFor(p => p.UserId, f => f.Random.Guid().ToString())
            .RuleFor(p => p.FirstName, f => f.Name.FirstName())
            .RuleFor(p => p.LastName, f => f.Name.LastName())
            .RuleFor(p => p.Email, (f, p) => f.Internet.Email(p.FirstName, p.LastName))
            .RuleFor(p => p.PhoneNumber, f => f.Phone.PhoneNumber())
            .RuleFor(p => p.Address, f => f.Address.FullAddress())
            .RuleFor(p => p.DateOfBirth, f => f.Date.Past(30, DateTime.Now))
            .RuleFor(p => p.BloodType, f => f.PickRandom(new[] { "A+", "A-", "B+", "B-", "O+", "O-", "AB+", "AB-" }));

        var patients = patientFaker.Generate(1000);
        foreach (var patientUser in patients)
        {
            var patient = new Patient
            {
                FirstName = patientUser.FirstName,
                LastName = patientUser.LastName,
                Email = patientUser.Email,
                PhoneNumber = patientUser.PhoneNumber,
                Address = patientUser.Address,
                DateOfBirth = patientUser.DateOfBirth,
                BloodType = patientUser.BloodType,
                MedicalHistory = "No known allergies",
                InsuranceInfo = "Basic Coverage"
            };
            context.Patients.Add(patient);
        }

        await context.SaveChangesAsync();
    }
}