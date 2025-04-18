using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using HealthApp.Domain.Services;
using HealthApp.Domain.Interfaces;
using HealthApp.Domain.EventBus;
using HealthApp.Domain.Entities;
using HealthApp.Razor.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IEventBus>(provider =>
{
    var config = provider.GetRequiredService<IConfiguration>();
    var logger = provider.GetRequiredService<ILogger<RabbitMQEventBus>>();
    return new RabbitMQEventBus(config, logger);
});

// Email service
builder.Services.AddTransient<IEmailService, EmailService>(); 

// Register application services
builder.Services.AddScoped<IAppointmentRepository, AppointmentService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IDoctorService, DoctorService>();
builder.Services.AddScoped<IPatientService, PatientService>();
builder.Services.AddScoped<IPrescriptionService, PrescriptionService>();

try
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(connectionString,
            sqlOptions => sqlOptions.EnableRetryOnFailure()));

    builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

    builder.Services.ConfigureApplicationCookie(options =>
    {
        options.LoginPath = "/Identity/Account/Login";
        options.AccessDeniedPath = "/Identity/Account/AccessDenied";
    });

    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
        options.AddPolicy("DoctorOrAdmin", policy =>
            policy.RequireRole("Doctor", "Admin"));
    });

    builder.Services.AddRazorPages(options =>
    {
        options.Conventions.AuthorizeFolder("/Admin", "Admin");
        options.Conventions.AuthorizeFolder("/Doctor", "Doctor");
        options.Conventions.AuthorizeFolder("/Patient", "Patient");
    });

    builder.Services.AddSession(options =>
    {
        options.IdleTimeout = TimeSpan.FromMinutes(30);
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
    });

    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
    }
    else
    {
        app.UseExceptionHandler("/Error");
        app.UseHsts();
    }

    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        try
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

            context.Database.Migrate();
            await SeedData.Initialize(services); 
        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "An error occurred seeding the DB.");
        }
    }

    app.UseHttpsRedirection();
    app.UseStaticFiles();
    app.UseRouting();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapRazorPages();
    app.Run();
}
catch (Exception ex)
{
    Console.WriteLine($"Host terminated unexpectedly: {ex.Message}");
    throw;
}
finally
{
    Console.WriteLine("Application shutting down...");
}