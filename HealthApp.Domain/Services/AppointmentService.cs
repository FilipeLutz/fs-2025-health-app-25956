using HealthApp.Razor.Data;
using HealthApp.Domain.Entities;
using HealthApp.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HealthApp.Domain.Services;

public class AppointmentService : IAppointmentRepository
{
    private readonly ApplicationDbContext _context;
    private readonly INotificationService _notificationService;

    public AppointmentService(ApplicationDbContext context, INotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
    }

    public async Task<Appointment> CreateAppointmentAsync(Appointment appointment)
    {
        // Validate no overlapping appointments
        var exists = await _context.Appointments
            .AnyAsync(a => a.DoctorId == appointment.DoctorId &&
                          a.AppointmentDateTime == appointment.AppointmentDateTime);

        if (exists) throw new Exception("Time slot already booked");

        _context.Appointments.Add(appointment);
        await _context.SaveChangesAsync();

        await _notificationService.SendAppointmentConfirmationAsync(appointment);

        return appointment;
    }

    public async Task<Appointment> GetByIdAsync(int id)
    {
        return await _context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .FirstOrDefaultAsync(a => a.Id == id) 
            ?? throw new InvalidOperationException($"Appointment with ID {id} not found.");
    }

    public async Task<IEnumerable<Appointment>> GetAllAsync()
    {
        return await _context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .ToListAsync();
    }

    public async Task<IEnumerable<Appointment>> GetByPatientIdAsync(int patientId)
    {
        return await _context.Appointments
            .Where(a => a.PatientId == patientId)
            .Include(a => a.Doctor)
            .OrderBy(a => a.AppointmentDateTime)
            .ToListAsync();
    }

    public async Task<IEnumerable<Appointment>> GetByDoctorIdAsync(int doctorId)
    {
        return await _context.Appointments
            .Where(a => a.DoctorId == doctorId)
            .Include(a => a.Patient)
            .OrderBy(a => a.AppointmentDateTime)
            .ToListAsync();
    }

    public async Task<IEnumerable<Appointment>> GetByDateRangeAsync(DateTime start, DateTime end)
    {
        return await _context.Appointments
            .Where(a => a.AppointmentDateTime >= start && a.AppointmentDateTime <= end)
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .OrderBy(a => a.AppointmentDateTime)
            .ToListAsync();
    }

    public async Task AddAsync(Appointment appointment)
    {
        if (await IsTimeSlotAvailableAsync(appointment.DoctorId, appointment.AppointmentDateTime, appointment.EndDateTime))
        {
            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();

            // Send notification
            await _notificationService.SendAppointmentConfirmationAsync(appointment);
        }
        else
        {
            throw new InvalidOperationException("The selected time slot is not available.");
        }
    }

    public async Task UpdateAsync(Appointment appointment)
    {
        _context.Entry(appointment).State = EntityState.Modified;
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var appointment = await GetByIdAsync(id);
        if (appointment != null)
        {
            _context.Appointments.Remove(appointment);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.Appointments.AnyAsync(e => e.Id == id);
    }

    public async Task<bool> IsTimeSlotAvailableAsync(int doctorId, DateTime start, DateTime end)
    {
        return !await _context.Appointments
            .Where(a => a.DoctorId == doctorId)
            .Where(a => a.Status != "Cancelled")
            .AnyAsync(a =>
                (start >= a.AppointmentDateTime && start < a.EndDateTime) ||
                (end > a.AppointmentDateTime && end <= a.EndDateTime) ||
                (start <= a.AppointmentDateTime && end >= a.EndDateTime));
    }

    public async Task<IEnumerable<Appointment>> GetPendingAppointmentsAsync(int doctorId)
    {
        return await _context.Appointments
            .Where(a => a.DoctorId == doctorId && a.Status == "Pending")
            .Include(a => a.Patient)
            .OrderBy(a => a.AppointmentDateTime)
            .ToListAsync();
    }

    public async Task<int> GetAppointmentCountByStatusAsync(string status)
    {
        return await _context.Appointments.CountAsync(a => a.Status == status);
    }

    public async Task CancelAppointmentAsync(int id, string reason)
    {
        var appointment = await _context.Appointments.FindAsync(id);

        if (appointment.AppointmentDateTime < DateTime.Now.AddHours(48))
            throw new Exception("Cannot cancel within 48 hours");

        appointment.Status = "Cancelled";
        appointment.CancellationReason = reason;

        await _context.SaveChangesAsync();
        await _notificationService.SendAppointmentCancellationAsync(appointment);
    }

    Task<bool> IAppointmentRepository.CancelAppointmentAsync(int appointmentId, string reason)
    {
        throw new NotImplementedException();
    }
}