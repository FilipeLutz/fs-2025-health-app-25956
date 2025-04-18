using HealthApp.Domain.Entities;

namespace HealthApp.Domain.Interfaces;

public interface IAppointmentRepository
{
    IQueryable<Appointment> GetAll();
    Task<Appointment> CreateAppointmentAsync(AppointmentCreateDto dto);
    Task<Appointment> GetByIdAsync(int id);
    Task<IEnumerable<Appointment>> GetAllAsync();
    IQueryable<Appointment> GetPatientAppointments(string patientId);
    Task<IEnumerable<Appointment>> GetByPatientIdAsync(int patientId);
    Task<IEnumerable<Appointment>> GetByDoctorIdAsync(int doctorId);
    Task<IEnumerable<Appointment>> GetByDateRangeAsync(DateTime start, DateTime end);
    Task AddAsync(Appointment appointment);
    Task UpdateAsync(Appointment appointment);
    Task DeleteAsync(int id);
    Task<bool> ExistsAsync(int id);
    Task<bool> IsTimeSlotAvailableAsync(int doctorId, DateTime start, DateTime end);
    Task<IEnumerable<Appointment>> GetPendingAppointmentsAsync(int doctorId);
    Task<int> GetAppointmentCountByStatusAsync(string status);
    Task<bool> CancelAppointmentAsync(int appointmentId, string reason);
    IEnumerable<object> GetDoctorAppointments(int id);
    Task<IEnumerable<Appointment>> GetRecentAsync(int count);
    Task<List<Appointment>> GetAllAppointmentsAsync();
}