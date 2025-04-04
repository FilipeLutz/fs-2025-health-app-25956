using HealthApp.Domain.Entities;

namespace HealthApp.Domain.Interfaces;

public interface INotificationService
{
    Task SendAppointmentConfirmationAsync(Appointment appointment);
    Task SendAppointmentReminderAsync(Appointment appointment);
    Task SendAppointmentCancellationAsync(Appointment appointment);
    Task SendAppointmentApprovalAsync(Appointment appointment);
    Task SendAppointmentRejectionAsync(Appointment appointment);
    Task CreateNotificationAsync(string userId, string message, string notificationType, int? relatedEntityId = null);
    Task MarkAsReadAsync(int notificationId);
    Task<IEnumerable<Notification>> GetUserNotificationsAsync(string userId);
    void SendCancellationNotificationAsync(Appointment appointment, string v);
}