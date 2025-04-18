using HealthApp.Domain.Entities;
using HealthApp.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HealthApp.Domain.Services
{
    public class PrescriptionService : IPrescriptionService
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notificationService;

        public PrescriptionService(ApplicationDbContext context, INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
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
            prescription.PrescribedDate = DateTime.UtcNow;
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
            var patientMeds = await _context.Prescriptions
                .Where(p => p.PatientId == patientId)
                .Select(p => p.Medication)
                .ToListAsync();

            // Placeholder logic – in real system use drug interaction API
            return patientMeds.Any(m => m.Equals(medication, StringComparison.OrdinalIgnoreCase));
        }

        public async Task<bool> RenewAsync(int id)
        {
            var prescription = await GetByIdAsync(id);
            if (prescription == null || !prescription.AllowRefills || prescription.RefillsAllowed <= 0)
                return false;

            var newPrescription = new Prescription
            {
                AppointmentId = prescription.AppointmentId,
                DoctorId = prescription.DoctorId,
                PatientId = prescription.PatientId,
                Medication = prescription.Medication,
                Dosage = prescription.Dosage,
                Frequency = prescription.Frequency,
                DurationDays = prescription.DurationDays,
                Instructions = prescription.Instructions,
                AllowRefills = prescription.AllowRefills,
                RefillsAllowed = prescription.RefillsAllowed - 1,
                PrescribedDate = DateTime.UtcNow
            };

            await CreateAsync(newPrescription);
            return true;
        }

        public async Task RequestRenewalAsync(int id, string patientUserId)
        {
            var prescription = await _context.Prescriptions
                .Include(p => p.Patient)
                .FirstOrDefaultAsync(p => p.Id == id && p.Patient.UserId == patientUserId);

            if (prescription == null)
                throw new Exception("Prescription not found or not owned by current user.");

            if (prescription.RenewalStatus != RenewalStatus.None)
                throw new Exception("Renewal already requested or processed.");

            prescription.RenewalStatus = RenewalStatus.Requested;
            _context.Prescriptions.Update(prescription);
            await _context.SaveChangesAsync();
        }

        public async Task RespondToRenewalRequestAsync(int id, bool approved, string? doctorNote)
        {
            var prescription = await _context.Prescriptions
                .Include(p => p.Patient)
                .Include(p => p.Doctor)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (prescription == null || prescription.RenewalStatus != RenewalStatus.Requested)
                throw new Exception("Renewal request not found or already processed.");

            if (approved)
            {
                var renewed = new Prescription
                {
                    AppointmentId = prescription.AppointmentId,
                    DoctorId = prescription.DoctorId,
                    PatientId = prescription.PatientId,
                    Medication = prescription.Medication,
                    Dosage = prescription.Dosage,
                    Frequency = prescription.Frequency,
                    DurationDays = prescription.DurationDays,
                    Instructions = prescription.Instructions,
                    AllowRefills = prescription.AllowRefills,
                    RefillsAllowed = prescription.RefillsAllowed,
                    PrescribedDate = DateTime.UtcNow,
                    RenewalStatus = RenewalStatus.None
                };

                prescription.RenewalStatus = RenewalStatus.Approved;
                prescription.RenewalNote = doctorNote;

                _context.Prescriptions.Update(prescription);
                _context.Prescriptions.Add(renewed);

                await _notificationService.CreateNotificationAsync(
                    prescription.Patient.UserId,
                    "Your prescription renewal has been approved.",
                    "PrescriptionRenewalApproved",
                    renewed.Id);
            }
            else
            {
                prescription.RenewalStatus = RenewalStatus.Rejected;
                prescription.RenewalNote = doctorNote;

                _context.Prescriptions.Update(prescription);

                await _notificationService.CreateNotificationAsync(
                    prescription.Patient.UserId,
                    $"Your renewal request was rejected: {doctorNote}",
                    "PrescriptionRenewalRejected",
                    prescription.Id);
            }

            await _context.SaveChangesAsync();
        }
    }
}