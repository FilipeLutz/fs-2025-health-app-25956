using HealthApp.Domain.Entities;

namespace HealthApp.Domain.Interfaces;

public interface IPatientService
{
    Task<Patient> GetPatientByIdAsync(int id);
    Task<IEnumerable<Patient>> GetAllPatientsAsync();
    Task UpdatePatientAsync(Patient patient);
    Task<IEnumerable<Appointment>> GetPatientAppointmentsAsync(int patientId);
}