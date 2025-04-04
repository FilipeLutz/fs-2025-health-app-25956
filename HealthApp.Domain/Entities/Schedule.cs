namespace HealthApp.Domain.Entities;

public class Schedule
{
    public int Id { get; set; }
    public int DoctorId { get; set; }
    public Doctor? Doctor { get; set; }

    public DayOfWeek DayOfWeek { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }

    public bool IsAvailable { get; set; } = true;
    public int MaxAppointments { get; set; } = 5;
}