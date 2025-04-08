using HealthApp.Domain.Entities;

namespace HealthApp.Domain.Interfaces;

public interface INotificationService
{
    // Appointment notifications
    Task SendAppointmentConfirmationAsync(Appointment appointment);
    Task SendAppointmentReminderAsync(Appointment appointment);
    Task SendAppointmentCancellationAsync(Appointment appointment);
    Task SendAppointmentApprovalAsync(Appointment appointment);
    Task SendAppointmentRejectionAsync(Appointment appointment);
    Task SendAppointmentRescheduleAsync(Appointment appointment);
    Task SendAppointmentCompletionAsync(Appointment appointment);

    // Prescription notifications
    Task SendPrescriptionNotificationAsync(Prescription prescription);

    // General notifications
    Task CreateNotificationAsync(string userId, string message, string notificationType, int? relatedEntityId = null);
    Task MarkAsReadAsync(int notificationId);
    Task<IEnumerable<Notification>> GetUserNotificationsAsync(string userId);
    Task SendAsync(Notification notification);
    Task<IEnumerable<Notification>> GetAllAsync();
    Task BroadcastAsync(string message, string targetRole);
    void SendCancellationNotificationAsync(Appointment appointment, string v);
}