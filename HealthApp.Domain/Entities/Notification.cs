namespace HealthApp.Domain.Entities;

public class Notification
{
    public int Id { get; set; }
    public required string UserId { get; set; } 
    public string? Message { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsRead { get; set; }
    public string? NotificationType { get; set; } 
    public int? RelatedEntityId { get; set; }
    public NotificationStatus Status { get; set; }
}

public enum NotificationStatus
{
    Unread,
    Read,
    Archived
}