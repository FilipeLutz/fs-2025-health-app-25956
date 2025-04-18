using HealthApp.Domain.Entities;
using HealthApp.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace HealthApp.Razor.Pages;

[Authorize(Roles = "Doctor")]
public class DoctorModel : PageModel
{
    private readonly IDoctorService _doctorService;
    private readonly IPrescriptionService _prescriptionService;
    private readonly INotificationService _notificationService;
    private readonly UserManager<ApplicationUser> _userManager;

    public DoctorModel(
        IDoctorService doctorService,
        IPrescriptionService prescriptionService,
        INotificationService notificationService,
        UserManager<ApplicationUser> userManager)
    {
        _doctorService = doctorService;
        _prescriptionService = prescriptionService;
        _notificationService = notificationService;
        _userManager = userManager;
    }

    public List<Appointment> TodayAppointments { get; set; }
    public List<Appointment> UpcomingAppointments { get; set; }
    public List<Appointment> PendingAppointments { get; set; }
    public List<Prescription> PendingRenewals { get; set; }

    [BindProperty(SupportsGet = true)]
    public string SearchTerm { get; set; }

    [BindProperty(SupportsGet = true)]
    public string StatusFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? DateFilter { get; set; }

    [BindProperty]
    public PrescriptionInputModel PrescriptionInput { get; set; }

    public List<SelectListItem> Medications { get; set; }

    public class PrescriptionInputModel
    {
        [Required]
        public int AppointmentId { get; set; }

        [Required]
        public string Medication { get; set; }

        [Required]
        public string Dosage { get; set; }

        [Required]
        public string Frequency { get; set; }

        [Required]
        public int DurationDays { get; set; }

        public string Instructions { get; set; }
        public bool AllowRefills { get; set; }
        public int RefillsAllowed { get; set; }
    }

    public async Task OnGetAsync()
    {
        var doctorUser = await GetDoctorAsync();
        if (doctorUser == null) return;

        var now = DateTime.Now;
        var today = DateTime.Today;

        Medications = GetMedicationsList();

        var appointments = await _doctorService.GetDoctorAppointmentsAsync(doctorUser.Id);
        var filtered = appointments.AsQueryable();

        if (!string.IsNullOrEmpty(SearchTerm))
        {
            filtered = filtered.Where(a =>
                a.Patient != null &&
                (a.Patient.FirstName.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                 a.Patient.LastName.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase)));
        }

        if (!string.IsNullOrEmpty(StatusFilter))
        {
            filtered = filtered.Where(a => a.Status == StatusFilter);
        }

        if (DateFilter.HasValue)
        {
            filtered = filtered.Where(a => a.AppointmentDateTime.Date == DateFilter.Value.Date);
        }

        TodayAppointments = filtered
            .Where(a => a.AppointmentDateTime.Date == today && a.Status == "Approved")
            .OrderBy(a => a.AppointmentDateTime)
            .ToList();

        UpcomingAppointments = filtered
            .Where(a => a.AppointmentDateTime > now && a.Status == "Approved")
            .OrderBy(a => a.AppointmentDateTime)
            .ToList();

        PendingAppointments = filtered
            .Where(a => a.Status == "Pending")
            .OrderBy(a => a.AppointmentDateTime)
            .ToList();

        PendingRenewals = (await _prescriptionService.GetByDoctorIdAsync(doctorUser.Id))
            .Where(p => p.RenewalStatus == RenewalStatus.Requested)
            .ToList();
    }

    public string GetStatusBadgeClass(string status)
    {
        return status switch
        {
            "Pending" => "badge-warning",
            "Approved" => "badge-success",
            "Completed" => "badge-info",
            "Cancelled" => "badge-danger",
            _ => "badge-secondary"
        };
    }

    public async Task<IActionResult> OnPostApproveAsync(int id)
    {
        await _doctorService.UpdateAppointmentStatusAsync(id, "Approved");
        var appointment = await _doctorService.GetByIdAsync(id);
        await _notificationService.SendAppointmentApprovalAsync(appointment);

        TempData["SuccessMessage"] = "Appointment approved.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRejectAsync(int id, string reason)
    {
        await _doctorService.UpdateAppointmentStatusAsync(id, "Rejected", reason);
        var appointment = await _doctorService.GetByIdAsync(id);
        await _notificationService.SendAppointmentRejectionAsync(appointment);

        TempData["SuccessMessage"] = "Appointment rejected.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostCompleteAsync(int id)
    {
        await _doctorService.UpdateAppointmentStatusAsync(id, "Completed");
        var appointment = await _doctorService.GetByIdAsync(id);
        await _notificationService.SendAppointmentCompletionAsync(appointment);

        TempData["SuccessMessage"] = "Appointment marked as completed.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostCreatePrescriptionAsync()
    {
        if (!ModelState.IsValid)
        {
            await OnGetAsync();
            return Page();
        }

        var user = await _userManager.GetUserAsync(User);
        var appointment = await _doctorService.GetByIdAsync(PrescriptionInput.AppointmentId);

        if (appointment == null || appointment.Doctor.UserId != user.Id)
        {
            TempData["ErrorMessage"] = "Invalid appointment.";
            return RedirectToPage();
        }

        var prescription = new Prescription
        {
            AppointmentId = PrescriptionInput.AppointmentId,
            DoctorId = appointment.DoctorId,
            PatientId = appointment.PatientId,
            Medication = PrescriptionInput.Medication,
            Dosage = PrescriptionInput.Dosage,
            Frequency = PrescriptionInput.Frequency,
            DurationDays = PrescriptionInput.DurationDays,
            Instructions = PrescriptionInput.Instructions,
            AllowRefills = PrescriptionInput.AllowRefills,
            RefillsAllowed = PrescriptionInput.RefillsAllowed,
            PrescribedDate = DateTime.UtcNow
        };

        await _prescriptionService.CreateAsync(prescription);
        await _notificationService.SendPrescriptionNotificationAsync(prescription);

        TempData["SuccessMessage"] = "Prescription created.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRescheduleAsync(int id, DateTime newDateTime)
    {
        var user = await _userManager.GetUserAsync(User);
        var appointment = await _doctorService.GetByIdAsync(id);

        if (!await _doctorService.IsTimeSlotAvailableAsync(user.Doctor.Id, newDateTime, newDateTime.AddMinutes(appointment.DurationMinutes)))
        {
            TempData["ErrorMessage"] = "Time slot not available.";
            return RedirectToPage();
        }

        appointment.AppointmentDateTime = newDateTime;
        appointment.Status = "Rescheduled";
        await _doctorService.UpdateAsync(appointment);

        await _notificationService.SendAppointmentRescheduleAsync(appointment);
        TempData["SuccessMessage"] = "Appointment rescheduled.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostExportScheduleAsync(DateTime startDate, DateTime endDate)
    {
        var user = await _userManager.GetUserAsync(User);
        var appointments = await _doctorService.GetDoctorAppointmentsAsync(user.Doctor.Id, startDate, endDate);

        var csv = "Patient,Date,Time,Status,Reason\n";
        foreach (var appt in appointments)
        {
            csv += $"{appt.Patient.FullName},{appt.AppointmentDateTime:yyyy-MM-dd},{appt.AppointmentDateTime:HH:mm},{appt.Status},{appt.Reason}\n";
        }

        return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", $"DoctorSchedule_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}.csv");
    }

    public async Task<IActionResult> OnPostRespondRenewalAsync(int prescriptionId, bool approve, string? note)
    {
        try
        {
            await _prescriptionService.RespondToRenewalRequestAsync(prescriptionId, approve, note);
            TempData["SuccessMessage"] = approve ? "Renewal approved." : "Renewal rejected.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Error: {ex.Message}";
        }

        return RedirectToPage();
    }

    private async Task<Doctor> GetDoctorAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        return await _doctorService.GetDoctorByUserIdAsync(user.Id);
    }

    private List<SelectListItem> GetMedicationsList() => new()
    {
        new SelectListItem { Value = "Amoxicillin", Text = "Amoxicillin (Antibiotic)" },
        new SelectListItem { Value = "Lisinopril", Text = "Lisinopril (Blood Pressure)" },
        new SelectListItem { Value = "Atorvastatin", Text = "Atorvastatin (Cholesterol)" },
        new SelectListItem { Value = "Metformin", Text = "Metformin (Diabetes)" },
        new SelectListItem { Value = "Albuterol", Text = "Albuterol (Asthma)" }
    };
}