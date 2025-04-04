using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using HealthApp.Domain.Entities;
using HealthApp.Razor.Data;
using Microsoft.AspNetCore.Identity;

namespace HealthApp.Domain.Services
{

    public class ApplicationDbContext : IdentityDbContext<IdentityUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Patient> Patients { get; set; }
        public DbSet<Doctor> Doctors { get; set; }
        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<Schedule> Schedules { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Prescription> Prescriptions { get; set; }
        public DbSet<DoctorPatient> DoctorPatients { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Appointment>()
                .HasOne(a => a.Doctor)
                .WithMany(d => d.Appointments)
                .HasForeignKey(a => a.DoctorId);

            builder.Entity<Appointment>()
                .HasOne(a => a.Patient)
                .WithMany(p => p.Appointments)
                .HasForeignKey(a => a.PatientId);

            builder.Entity<Schedule>()
                .HasOne(s => s.Doctor)
                .WithMany(d => d.Schedules)
                .HasForeignKey(s => s.DoctorId);

            builder.Entity<Prescription>()
                .HasOne(p => p.Appointment)
                .WithOne(a => a.Prescription)
                .HasForeignKey<Prescription>(p => p.AppointmentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Appointment>()
                .HasIndex(a => a.AppointmentDateTime);

            builder.Entity<Appointment>()
                .HasIndex(a => a.Status);

            builder.Entity<Doctor>()
                .HasIndex(d => d.UserId)
                .IsUnique();

            builder.Entity<Patient>()
                .HasIndex(p => p.UserId)
                .IsUnique();

            builder.Entity<DoctorPatient>()
        .HasKey(dp => new { dp.DoctorId, dp.PatientId });

            builder.Entity<DoctorPatient>()
                .HasOne(dp => dp.Doctor)
                .WithMany(d => d.DoctorPatients)
                .HasForeignKey(dp => dp.DoctorId);

            builder.Entity<DoctorPatient>()
                .HasOne(dp => dp.Patient)
                .WithMany(p => p.DoctorPatients)
                .HasForeignKey(dp => dp.PatientId);
        }
    }
}