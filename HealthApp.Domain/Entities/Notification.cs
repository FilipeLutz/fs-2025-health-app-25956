namespace HealthApp.Domain.Entities;

public class Notification
{
    public int Id { get; set; }
    public required string UserId { get; set; } // Identity User ID
    public string? Message { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsRead { get; set; }
    public string? NotificationType { get; set; } // Appointment, Reminder, etc.
    public int? RelatedEntityId { get; set; } // ID of related appointment, etc.
}