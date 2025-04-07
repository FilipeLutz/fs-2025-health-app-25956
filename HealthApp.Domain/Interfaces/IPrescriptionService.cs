using HealthApp.Domain.Entities;
namespace HealthApp.Domain.Interfaces
{
    public interface IPrescriptionService
    {
        Task<Prescription> GetByIdAsync(int id);
        Task<IEnumerable<Prescription>> GetByPatientIdAsync(int patientId);
        Task<IEnumerable<Prescription>> GetByDoctorIdAsync(int doctorId);
        Task CreateAsync(Prescription prescription);
        Task UpdateAsync(Prescription prescription);
        Task DeleteAsync(int id);
        Task<bool> CheckForInteractions(string medication, int patientId);
        Task<IEnumerable<Prescription>> GetAllAsync();
        Task<bool> RenewAsync(int id);
    }
}