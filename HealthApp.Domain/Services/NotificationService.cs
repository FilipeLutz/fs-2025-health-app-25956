using System.Text.Json;
using HealthApp.Domain.Entities;
using HealthApp.Domain.Interfaces;
using HealthApp.Domain.EventBus;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HealthApp.Domain.Services;

public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _context;
    private readonly IEventBus _eventBus;
    private readonly IConfiguration _configuration;
    private readonly IEmailService _emailService;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        ApplicationDbContext context,
        IEventBus eventBus,
        IEmailService emailService,
        IConfiguration configuration,
        ILogger<NotificationService> logger)
    {
        _context = context;
        _eventBus = eventBus;
        _emailService = emailService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendAppointmentConfirmationAsync(Appointment appointment)
    {
        var patient = await _context.Patients
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.Id == appointment.PatientId);

        var doctor = await _context.Doctors
            .Include(d => d.User)
            .FirstOrDefaultAsync(d => d.Id == appointment.DoctorId);

        try
        {
            var message = new
            {
                Type = "AppointmentConfirmation",
                AppointmentId = appointment,
                PatientEmail = patient?.User?.Email,
                DoctorName = $"{doctor?.FirstName} {doctor?.LastName}",
                AppointmentTime = appointment.AppointmentDateTime.ToString("f"),
                Location = "Main Hospital, Room 101"
            };

            _eventBus.Publish("notifications", JsonSerializer.Serialize(message));

            if (!string.IsNullOrEmpty(message.PatientEmail))
            {
                await _emailService.SendEmailAsync(
                    message.PatientEmail,
                    "Appointment Confirmation",
                    $"Your appointment with Dr. {message.DoctorName} is confirmed for {message.AppointmentTime}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send appointment confirmation");
            throw;
        }
    }


    public async Task SendAppointmentReminderAsync(Appointment appointment)
    {
        var patient = await _context.Patients.FindAsync(appointment.PatientId);
        var doctor = await _context.Doctors.FindAsync(appointment.DoctorId);

        var message = $"Reminder: You have an appointment with Dr. {doctor} " +
                     $"tomorrow at {appointment.AppointmentDateTime.ToString("t")}.";

        _eventBus.Publish("email_queue", message);
        await CreateNotificationAsync(
            patient.UserId, 
            message, 
            "Reminder",
            appointment.Id
        );
    }

    public async Task SendAppointmentCancellationAsync(Appointment appointment)
    {
        var patient = await _context.Patients.FindAsync(appointment.PatientId);
        var doctor = await _context.Doctors.FindAsync(appointment.DoctorId);

        var message = $"Your appointment with Dr. {doctor} " +
                     $"on {appointment.AppointmentDateTime.ToString("f")} has been cancelled.";

        _eventBus.Publish("email_queue", message);
        await CreateNotificationAsync(patient.UserId, message, "Cancellation", appointment.Id);
    }

    public async Task SendAppointmentApprovalAsync(Appointment appointment)
    {
        var patient = await _context.Patients.FindAsync(appointment.PatientId);
        var doctor = await _context.Doctors.FindAsync(appointment.DoctorId);

        var message = $"Your appointment with Dr. {doctor} " +
                     $"on {appointment.AppointmentDateTime.ToString("f")} has been approved.";

        _eventBus.Publish("email_queue", message);
        await CreateNotificationAsync(patient.UserId, message, "Approval", appointment.Id);
    }

    public async Task SendAppointmentRejectionAsync(Appointment appointment)
    {
        var patient = await _context.Patients.FindAsync(appointment.PatientId);
        var doctor = await _context.Doctors.FindAsync(appointment.DoctorId);

        var message = $"Your appointment with Dr. {doctor} " +
                     $"on {appointment.AppointmentDateTime.ToString("f")} has been rejected. " +
                     $"Reason: {appointment.CancellationReason}";

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
    public async Task SendAppointmentRescheduleAsync(Appointment appointment)
    {
        var patient = await _context.Patients.FindAsync(appointment.PatientId);
        var doctor = await _context.Doctors.FindAsync(appointment.DoctorId);

        var message = $"Your appointment with Dr. {doctor} " +
                     $"has been rescheduled to {appointment.AppointmentDateTime.ToString("f")}.";

        _eventBus.Publish("email_queue", message);
        await CreateNotificationAsync(patient.UserId, message, "Reschedule", appointment.Id);
    }

    public async Task SendAppointmentCompletionAsync(Appointment appointment)
    {
        var patient = await _context.Patients.FindAsync(appointment.PatientId);
        var doctor = await _context.Doctors.FindAsync(appointment.DoctorId);

        var message = $"Your appointment with Dr. {doctor} " +
                     $"on {appointment.AppointmentDateTime.ToString("f")} has been marked as completed.";

        _eventBus.Publish("email_queue", message);
        await CreateNotificationAsync(patient.UserId, message, "Completion", appointment.Id);
    }

    public async Task SendPrescriptionNotificationAsync(Prescription prescription)
    {
        var patient = await _context.Patients.FindAsync(prescription.PatientId);
        var doctor = await _context.Doctors.FindAsync(prescription.DoctorId);

        var message = $"You have a new prescription from Dr. {doctor} " +
                     $"for {prescription.Medication}.";

        _eventBus.Publish("email_queue", message);
        await CreateNotificationAsync(patient.UserId, message, "Prescription", prescription.Id);
    }

    public Task SendAsync(Notification notification)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<Notification>> GetAllAsync()
    {
        throw new NotImplementedException();
    }

    public Task BroadcastAsync(string message, string targetRole)
    {
        throw new NotImplementedException();
    }
}