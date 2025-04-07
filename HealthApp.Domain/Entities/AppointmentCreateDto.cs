namespace HealthApp.Domain.Entities;

public class AppointmentCreateDto
{
    public int DoctorId { get; set; }
    public int PatientId { get; set; }
    public DateTime AppointmentDateTime { get; set; }
    public string Reason { get; set; }
}