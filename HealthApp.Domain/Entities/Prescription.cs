namespace HealthApp.Domain.Entities;

public class Prescription
{
    public int Id { get; set; }
    public int AppointmentId { get; set; }
    public required Appointment Appointment { get; set; }

    public string? Medication { get; set; }
    public string? Dosage { get; set; }
    public string? Instructions { get; set; }
    public DateTime PrescribedDate { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiryDate { get; set; }
    public string? Notes { get; set; }
}