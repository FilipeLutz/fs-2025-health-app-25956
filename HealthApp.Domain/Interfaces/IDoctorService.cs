using HealthApp.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HealthApp.Domain.Interfaces;

public interface IDoctorService
{
    // Doctor operations
    Task<Doctor> GetDoctorByIdAsync(int id);
    Task<IEnumerable<Doctor>> GetAllDoctorsAsync();
    Task<IEnumerable<Doctor>> GetDoctorsBySpecializationAsync(string specialization);

    // Schedule operations
    Task<IEnumerable<Schedule>> GetDoctorScheduleAsync(int doctorId);
    Task AddScheduleAsync(Schedule schedule);
    Task UpdateScheduleAsync(Schedule schedule);
    Task RemoveScheduleAsync(int scheduleId);

    // Appointment operations
    Task<Appointment> GetByIdAsync(int id);
    Task<Doctor> GetDoctorByUserIdAsync(string userId);
    Task<IEnumerable<Appointment>> GetDoctorAppointmentsAsync(int doctorId);
    Task<IEnumerable<Appointment>> GetDoctorAppointmentsAsync(int doctorId, DateTime? date = null);
    Task<IEnumerable<Appointment>> GetDoctorAppointmentsAsync(int doctorId, DateTime startDate, DateTime endDate);
    Task UpdateAppointmentStatusAsync(int appointmentId, string status, string notes = null);
    Task UpdateAsync(Appointment appointment);
    Task<bool> IsTimeSlotAvailableAsync(int doctorId, DateTime start, DateTime end);
    Task<int> GetCountAsync();
}