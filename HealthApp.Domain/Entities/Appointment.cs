using System.ComponentModel.DataAnnotations;

namespace HealthApp.Domain.Entities;

public class Appointment
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int PatientId { get; set; }
    public required Patient Patient { get; set; }

    [Required]
    public int DoctorId { get; set; }
    public required Doctor Doctor { get; set; }

    [Required]
    public DateTime AppointmentDateTime { get; set; }

    [Required]
    public DateTime EndDateTime { get; set; }

    [Required, MaxLength(50)]
    public string? Status { get; set; }
    [MaxLength(500)]
    public string? Reason { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public bool IsCancelled { get; set; }
    public DateTime? CancellationDate { get; set; }
    public string? CancellationReason { get; set; }

    public Prescription? Prescription { get; set; }
}