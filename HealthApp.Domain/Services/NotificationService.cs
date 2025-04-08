using HealthApp.Domain.Entities;
using HealthApp.Domain.EventBus;
using HealthApp.Domain.Interfaces;
using HealthApp.Razor.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace HealthApp.Domain.Services;

public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _context;
    private readonly IEventBus _eventBus;
    private readonly IConfiguration _configuration;

    public NotificationService(ApplicationDbContext context, IEventBus eventBus, IConfiguration configuration)
    {
        _context = context;
        _eventBus = eventBus;
        _configuration = configuration;
    }

    public async Task SendAppointmentConfirmationAsync(Appointment appointment)
    {
        var patient = await _context.Patients.FindAsync(appointment.PatientId);
        var doctor = await _context.Doctors.FindAsync(appointment.DoctorId);

        var message = $"Dear {patient.Name}, your appointment with Dr. {doctor.Name} " +
                     $"on {appointment.AppointmentDateTime.ToString("f")} has been booked successfully.";

        // Send to queue for email service to process
        _eventBus.Publish("email_queue", message);

        // Also create a notification in the database
        await CreateNotificationAsync(patient.UserId,
            $"Appointment booked with Dr. {doctor.Name}",
            "Appointment",
            appointment.Id);
    }

    public async Task SendAppointmentReminderAsync(Appointment appointment)
    {
        var patient = await _context.Patients.FindAsync(appointment.PatientId);
        var doctor = await _context.Doctors.FindAsync(appointment.DoctorId);

        var message = $"Reminder: You have an appointment with Dr. {doctor.Name} " +
                     $"tomorrow at {appointment.AppointmentDateTime.ToString("t")}.";

        _eventBus.Publish("email_queue", message);
        await CreateNotificationAsync(patient.UserId, message, "Reminder", appointment.Id);
    }

    public async Task SendAppointmentCancellationAsync(Appointment appointment)
    {
        var patient = await _context.Patients.FindAsync(appointment.PatientId);
        var doctor = await _context.Doctors.FindAsync(appointment.DoctorId);

        var message = $"Your appointment with Dr. {doctor.Name} " +
                     $"on {appointment.AppointmentDateTime.ToString("f")} has been cancelled.";

        _eventBus.Publish("email_queue", message);
        await CreateNotificationAsync(patient.UserId, message, "Cancellation", appointment.Id);
    }

    public async Task SendAppointmentApprovalAsync(Appointment appointment)
    {
        var patient = await _context.Patients.FindAsync(appointment.PatientId);
        var doctor = await _context.Doctors.FindAsync(appointment.DoctorId);

        var message = $"Your appointment with Dr. {doctor.Name} " +
                     $"on {appointment.AppointmentDateTime.ToString("f")} has been approved.";

        _eventBus.Publish("email_queue", message);
        await CreateNotificationAsync(patient.UserId, message, "Approval", appointment.Id);
    }

    public async Task SendAppointmentRejectionAsync(Appointment appointment)
    {
        var patient = await _context.Patients.FindAsync(appointment.PatientId);
        var doctor = await _context.Doctors.FindAsync(appointment.DoctorId);

        var message = $"Your appointment with Dr. {doctor.Name} " +
                     $"on {appointment.AppointmentDateTime.ToString("f")} has been rejected. " +
                     $"Reason: {appointment.Notes}";

        _eventBus.Publish("email_queue", message);
        await CreateNotificationAsync(patient.UserId, message, "Rejection", appointment.Id);
    }

    public async Task CreateNotificationAsync(string userId, string message, string notificationType, int? relatedEntityId = null)
    {
        var notification = new Notification
        {
            UserId = userId,
            Message = message,
            NotificationType = notificationType,
            RelatedEntityId = relatedEntityId,
            IsRead = false
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();
    }

    public async Task MarkAsReadAsync(int notificationId)
    {
        var notification = await _context.Notifications.FindAsync(notificationId);
        if (notification != null)
        {
            notification.IsRead = true;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<Notification>> GetUserNotificationsAsync(string userId)
    {
        return await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
    }

    public void SendCancellationNotificationAsync(Appointment appointment, string v)
    {
        throw new NotImplementedException();
    }
}