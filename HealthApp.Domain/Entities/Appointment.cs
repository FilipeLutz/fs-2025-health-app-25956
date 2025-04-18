using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HealthApp.Domain.Entities;

public class Appointment
{
    public int Id { get; set; }
    public DateTime AppointmentDateTime { get; set; }
    public DateTime EndDateTime { get; set; } 
    public int DurationMinutes { get; set; } = 30;
    public string Status { get; set; }
    public string? Reason { get; set; }
    public string? CancellationReason { get; set; }
    public string? Notes { get; set; }

    [ForeignKey("Patient")]
    public int PatientId { get; set; }
    public Patient? Patient { get; set; }

    [ForeignKey("Doctor")]
    public int DoctorId { get; set; }
    public Doctor? Doctor { get; set; }

    public Prescription? Prescription { get; set; }
    public bool IsDeleted { get; internal set; }
    [NotMapped]
    public object Feedback { get; set; }
}