using HealthApp.Domain.Entities;
using HealthApp.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HealthApp.Domain.Services;

public class PatientService : IPatientService
{
    private readonly ApplicationDbContext _context;

    public PatientService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Patient> GetPatientByIdAsync(int id)
    {
        return await _context.Patients
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<IEnumerable<Patient>> GetAllPatientsAsync()
    {
        return await _context.Patients
            .Include(p => p.User)
            .ToListAsync();
    }

    public async Task UpdatePatientAsync(Patient patient)
    {
        _context.Patients.Update(patient);
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<Appointment>> GetPatientAppointmentsAsync(int patientId)
    {
        return await _context.Appointments
            .Where(a => a.PatientId == patientId)
            .Include(a => a.Doctor)
            .ThenInclude(d => d.User)
            .OrderByDescending(a => a.AppointmentDateTime)
            .ToListAsync();
    }
}