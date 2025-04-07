using HealthApp.Domain.Entities;
using HealthApp.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HealthApp.Domain.Services
{
    public class PrescriptionService : IPrescriptionService
    {
        private readonly ApplicationDbContext _context;

        public PrescriptionService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Prescription>> GetAllAsync()
        {
            return await _context.Prescriptions
                .Include(p => p.Patient)
                .Include(p => p.Doctor)
                .ToListAsync();
        }

        public async Task<Prescription> GetByIdAsync(int id)
        {
            return await _context.Prescriptions
                .Include(p => p.Patient)
                .Include(p => p.Doctor)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<IEnumerable<Prescription>> GetByPatientIdAsync(int patientId)
        {
            return await _context.Prescriptions
                .Where(p => p.PatientId == patientId)
                .Include(p => p.Doctor)
                .ToListAsync();
        }

        public async Task<IEnumerable<Prescription>> GetByDoctorIdAsync(int doctorId)
        {
            return await _context.Prescriptions
                .Where(p => p.DoctorId == doctorId)
                .Include(p => p.Patient)
                .ToListAsync();
        }

        public async Task CreateAsync(Prescription prescription)
        {
            await _context.Prescriptions.AddAsync(prescription);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Prescription prescription)
        {
            _context.Prescriptions.Update(prescription);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var prescription = await GetByIdAsync(id);
            if (prescription != null)
            {
                _context.Prescriptions.Remove(prescription);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> CheckForInteractions(string medication, int patientId)
        {
            // Implement your medication interaction logic here
            // This is a placeholder implementation
            var patientMeds = await _context.Prescriptions
                .Where(p => p.PatientId == patientId)
                .Select(p => p.Medication)
                .ToListAsync();

            // Simple check - in real app you'd use a drug interaction API/service
            return patientMeds.Any(m => m == medication);
        }

        public async Task<bool> RenewAsync(int id)
        {
            var prescription = await GetByIdAsync(id);
            if (prescription == null) return false;

            // Check if prescription is renewable
            if (!prescription.AllowRefills || prescription.RefillsAllowed <= 0)
                return false;

            // Create a new prescription with the same details
            var renewedPrescription = new Prescription
            {
                Medication = prescription.Medication,
                Dosage = prescription.Dosage,
                Frequency = prescription.Frequency,
                DurationDays = prescription.DurationDays,
                Instructions = prescription.Instructions,
                PatientId = prescription.PatientId,
                DoctorId = prescription.DoctorId,
                PrescribedDate = DateTime.Now,
                AllowRefills = prescription.AllowRefills,
                RefillsAllowed = prescription.RefillsAllowed - 1
            };

            await CreateAsync(renewedPrescription);
            return true;
        }
    }
}