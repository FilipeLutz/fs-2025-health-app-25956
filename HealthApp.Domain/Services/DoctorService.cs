using HealthApp.Domain.Entities;
using HealthApp.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HealthApp.Domain.Services;

public class DoctorService : IDoctorService
{
    private readonly ApplicationDbContext _context;

    public DoctorService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Doctor> GetDoctorByIdAsync(int id)
    {
        return await _context.Doctors
            .Include(d => d.User)
            .Include(d => d.Schedules)
            .FirstOrDefaultAsync(d => d.Id == id)
            ?? throw new KeyNotFoundException($"Doctor with ID {id} not found.");
    }

    public async Task<IEnumerable<Doctor>> GetAllDoctorsAsync()
    {
        return await _context.Doctors
            .Include(d => d.User)
            .ToListAsync();
    }

    public async Task<IEnumerable<Doctor>> GetDoctorsBySpecializationAsync(string specialization)
    {
        return await _context.Doctors
            .Where(d => d.Specialization.Contains(specialization))
            .Include(d => d.User)
            .ToListAsync();
    }

    public async Task<IEnumerable<Schedule>> GetDoctorScheduleAsync(int doctorId)
    {
        return await _context.Schedules
            .Where(s => s.DoctorId == doctorId)
            .OrderBy(s => s.DayOfWeek)
            .ThenBy(s => s.StartTime)
            .ToListAsync();
    }

    public async Task AddScheduleAsync(Schedule schedule)
    {
        _context.Schedules.Add(schedule);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateScheduleAsync(Schedule schedule)
    {
        _context.Schedules.Update(schedule);
        await _context.SaveChangesAsync();
    }

    public async Task RemoveScheduleAsync(int scheduleId)
    {
        var schedule = await _context.Schedules.FindAsync(scheduleId);
        if (schedule != null)
        {
            _context.Schedules.Remove(schedule);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<Appointment>> GetDoctorAppointmentsAsync(int doctorId, DateTime? date = null)
    {
        var query = _context.Appointments
            .Where(a => a.DoctorId == doctorId)
            .Include(a => a.Patient)
            .ThenInclude(p => p.User)
            .AsQueryable();

        if (date.HasValue)
        {
            query = query.Where(a => a.AppointmentDateTime.Date == date.Value.Date);
        }

        return await query
            .OrderBy(a => a.AppointmentDateTime)
            .ToListAsync();
    }

    public async Task<IEnumerable<Appointment>> GetDoctorAppointmentsAsync(int doctorId, DateTime startDate, DateTime endDate)
    {
        return await _context.Appointments
            .Where(a => a.DoctorId == doctorId && a.AppointmentDateTime >= startDate && a.AppointmentDateTime <= endDate)
            .Include(a => a.Patient)
                .ThenInclude(p => p.User)
            .OrderBy(a => a.AppointmentDateTime)
            .ToListAsync();
    }

    public async Task UpdateAppointmentStatusAsync(int appointmentId, string status, string notes = null)
    {
        var appointment = await _context.Appointments.FindAsync(appointmentId);
        if (appointment != null)
        {
            appointment.Status = status;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<Appointment> GetByIdAsync(int id)
    {
        return await _context.Appointments
            .Include(a => a.Patient)
                .ThenInclude(p => p.User)
            .FirstOrDefaultAsync(a => a.Id == id)
            ?? throw new KeyNotFoundException($"Appointment with ID {id} not found.");
    }

    public async Task UpdateAsync(Appointment appointment)
    {
        _context.Appointments.Update(appointment);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> IsTimeSlotAvailableAsync(int doctorId, DateTime start, DateTime end)
    {
        return !await _context.Appointments
            .AnyAsync(a => a.DoctorId == doctorId && a.AppointmentDateTime >= start && a.AppointmentDateTime < end);
    }

    public Task<int> GetCountAsync()
    {
        throw new NotImplementedException();
    }

    public async Task<IEnumerable<Appointment>> GetDoctorAppointmentsAsync(string doctorId)
    {
        return await _context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .Where(a => a.Doctor.UserId == doctorId)
            .OrderBy(a => a.AppointmentDateTime)
            .ToListAsync();
    }

    public async Task<IEnumerable<Appointment>> GetDoctorAppointmentsAsync(string doctorId, DateTime? date)
    {
        var query = _context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .Where(a => a.Doctor.UserId == doctorId);

        if (date.HasValue)
        {
            query = query.Where(a => a.AppointmentDateTime.Date == date.Value.Date);
        }

        return await query
            .OrderBy(a => a.AppointmentDateTime)
            .ToListAsync();
    }
    public async Task<IEnumerable<Appointment>> GetDoctorAppointmentsAsync(string doctorId, DateTime startDate, DateTime endDate)
    {
        return await _context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .Where(a => a.Doctor.UserId == doctorId &&
                       a.AppointmentDateTime >= startDate &&
                       a.AppointmentDateTime <= endDate)
            .OrderBy(a => a.AppointmentDateTime)
            .ToListAsync();
    }
    public async Task<IEnumerable<Appointment>> GetDoctorAppointmentsAsync(
        string doctorId,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var query = _context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .Where(a => a.Doctor.UserId == doctorId);

        if (startDate.HasValue)
            query = query.Where(a => a.AppointmentDateTime >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(a => a.AppointmentDateTime <= endDate.Value);

        return await query.ToListAsync();
    }

    public Task<IEnumerable<Appointment>> GetDoctorAppointmentsAsync(int doctorId)
    {
        throw new NotImplementedException();
    }
    public async Task<Doctor> GetDoctorByUserIdAsync(string userId)
    {
        return await _context.Doctors
            .Include(d => d.User)
            .Include(d => d.Schedules)
            .FirstOrDefaultAsync(d => d.UserId == userId)
            ?? throw new KeyNotFoundException($"Doctor with User ID {userId} not found.");
    }
}
