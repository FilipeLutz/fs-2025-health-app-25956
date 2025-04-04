using System.ComponentModel.DataAnnotations;
using System.Collections.Generic; 
using HealthApp.Domain.Entities;
using HealthApp.Razor.Data;

namespace HealthApp.Domain.Entities;

public class Patient
{
    [Key]
    public int Id { get; set; }

    [Required]
    public required string UserId { get; set; }

    [Required, MaxLength(100)]
    public required string Name { get; set; }

    [Required]
    public DateTime DateOfBirth { get; set; }

    [MaxLength(20)]
    public string? PhoneNumber { get; set; }

    [MaxLength(100)]
    public required string Email { get; set; }

    [MaxLength(500)]
    public string? Address { get; set; }

    public ICollection<DoctorPatient>? DoctorPatients { get; set; }
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    public ICollection<Prescription> MedicalRecords { get; set; } = new List<Prescription>();

    // Additional patient-specific properties
    public string? BloodType { get; set; }
    public double Height { get; set; }
    public double Weight { get; set; }
    public string? Allergies { get; set; }

    public static implicit operator Patient(Domain.Patient v)
    {
        throw new NotImplementedException();
    }
}