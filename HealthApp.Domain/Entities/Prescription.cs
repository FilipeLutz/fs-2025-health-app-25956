using System.ComponentModel.DataAnnotations;

namespace HealthApp.Domain.Entities;

public enum RenewalStatus
{
    None,
    Requested,
    Approved,
    Rejected
}

public class Prescription
{
    public int Id { get; set; }
    [Required]
    public int AppointmentId { get; set; }
    public Appointment? Appointment { get; set; }
    [Required]
    public int DoctorId { get; set; }
    public Doctor? Doctor { get; set; }
    [Required]
    public int PatientId { get; set; }
    public Patient? Patient { get; set; }
    [Required, MaxLength(100)]
    public string Medication { get; set; } = null!;
    [Required, MaxLength(50)]
    public string Dosage { get; set; } = null!;
    [Required, MaxLength(50)]
    public string Frequency { get; set; } = null!;
    [Required]
    public int DurationDays { get; set; }
    public string? Instructions { get; set; }
    public bool AllowRefills { get; set; }
    public int RefillsAllowed { get; set; }
    [Required]
    public DateTime PrescribedDate { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiryDate { get; set; }
    public string? Notes { get; set; }
    public RenewalStatus RenewalStatus { get; set; } = RenewalStatus.None;
    public string? RenewalNote { get; set; }
}