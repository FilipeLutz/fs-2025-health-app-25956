using HealthApp.Razor.Data;
using System.ComponentModel.DataAnnotations;

namespace HealthApp.Domain.Entities;

public class Doctor
{
    [Key]
    public int Id { get; set; }

    [Required]
    public required string UserId { get; set; }

    [Required, MaxLength(100)]
    public required string Name { get; set; }

    [Required, MaxLength(100)]
    public required string Specialization { get; set; }

    [MaxLength(100)]
    public required string Email { get; set; }

    [MaxLength(500)]
    public string? Bio { get; set; }

    public ICollection<DoctorPatient>? DoctorPatients { get; set; }
    public ICollection<Appointment>? Appointments { get; set; }
    public ICollection<Schedule>? Schedules { get; set; }

    // Additional doctor-specific properties
    public string? LicenseNumber { get; set; }
    public int? YearsOfExperience { get; set; }
    public string? HospitalAffiliation { get; set; }
}
