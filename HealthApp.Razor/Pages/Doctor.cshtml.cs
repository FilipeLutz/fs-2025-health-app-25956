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
public class DoctorDashboardModel : PageModel
{
    private readonly IDoctorService _doctorService;
    private readonly IPrescriptionService _prescriptionService;
    private readonly INotificationService _notificationService;
    private readonly UserManager<ApplicationUser> _userManager;

    public DoctorDashboardModel(
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

    // Appointment Lists
    public List<Appointment> TodayAppointments { get; set; }
    public List<Appointment> UpcomingAppointments { get; set; }
    public List<Appointment> PendingAppointments { get; set; }

    // Filter Properties
    [BindProperty(SupportsGet = true)]
    public string SearchTerm { get; set; }
    [BindProperty(SupportsGet = true)]
    public string StatusFilter { get; set; }
    [BindProperty(SupportsGet = true)]
    public DateTime? DateFilter { get; set; }

    // Prescription Properties
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
        var userId = _userManager.GetUserId(User);
        var doctorUser = await _doctorService.GetDoctorByUserIdAsync(userId);

        if (doctorUser == null) return;

        var today = DateTime.Today;
        var now = DateTime.Now;

        Medications = GetMedicationsList();

        var appointments = await _doctorService.GetDoctorAppointmentsAsync(doctorUser.Id);
        var query = appointments?.AsQueryable() ?? Enumerable.Empty<Appointment>().AsQueryable();

        if (!string.IsNullOrEmpty(SearchTerm))
        {
            query = query.Where(a =>
                a.Patient != null &&
                (a.Patient.FirstName != null && a.Patient.FirstName.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                a.Patient.LastName != null && a.Patient.LastName.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase)));
        }

        if (!string.IsNullOrEmpty(StatusFilter))
        {
            query = query.Where(a => a.Status == StatusFilter);
        }

        if (DateFilter.HasValue)
        {
            query = query.Where(a => a.AppointmentDateTime.Date == DateFilter.Value.Date);
        }

        TodayAppointments = query
            .Where(a => a.AppointmentDateTime.Date == today && a.Status == "Approved")
            .OrderBy(a => a.AppointmentDateTime)
            .ToList();

        UpcomingAppointments = query
            .Where(a => a.AppointmentDateTime > now && a.Status == "Approved")
            .OrderBy(a => a.AppointmentDateTime)
            .ToList();

        PendingAppointments = query
            .Where(a => a.Status == "Pending")
            .OrderBy(a => a.AppointmentDateTime)
            .ToList();
    }

    public async Task<IActionResult> OnPostApproveAsync(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        await _doctorService.UpdateAppointmentStatusAsync(id, "Approved");

        var appointment = await _doctorService.GetByIdAsync(id);
        await _notificationService.SendAppointmentApprovalAsync(appointment);

        TempData["SuccessMessage"] = "Appointment approved successfully";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRejectAsync(int id, string reason)
    {
        var user = await _userManager.GetUserAsync(User);
        await _doctorService.UpdateAppointmentStatusAsync(id, "Rejected", reason);

        var appointment = await _doctorService.GetByIdAsync(id);
        await _notificationService.SendAppointmentRejectionAsync(appointment);

        TempData["SuccessMessage"] = "Appointment rejected successfully";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostCompleteAsync(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        await _doctorService.UpdateAppointmentStatusAsync(id, "Completed");

        var appointment = await _doctorService.GetByIdAsync(id);
        await _notificationService.SendAppointmentCompletionAsync(appointment);

        TempData["SuccessMessage"] = "Appointment marked as completed";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRescheduleAsync(int id, DateTime newDateTime)
    {
        var user = await _userManager.GetUserAsync(User);
        var appointment = await _doctorService.GetByIdAsync(id);

        // Check if new time is available
        var isAvailable = await _doctorService.IsTimeSlotAvailableAsync(
            user.Doctor.Id,
            newDateTime,
            newDateTime.AddMinutes(appointment.DurationMinutes));

        if (!isAvailable)
        {
            TempData["ErrorMessage"] = "The selected time slot is not available";
            return RedirectToPage();
        }

        // Update appointment
        appointment.AppointmentDateTime = newDateTime;
        appointment.Status = "Rescheduled";
        await _doctorService.UpdateAsync(appointment);

        await _notificationService.SendAppointmentRescheduleAsync(appointment);

        TempData["SuccessMessage"] = "Appointment rescheduled successfully";
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

        if (appointment == null || appointment.DoctorId != user.Doctor.Id)
        {
            TempData["ErrorMessage"] = "Invalid appointment";
            return RedirectToPage();
        }

        var prescription = new Prescription
        {
            AppointmentId = PrescriptionInput.AppointmentId,
            DoctorId = user.Doctor.Id,
            PatientId = appointment.PatientId,
            Medication = PrescriptionInput.Medication,
            Dosage = PrescriptionInput.Dosage,
            Frequency = PrescriptionInput.Frequency,
            DurationDays = PrescriptionInput.DurationDays,
            Instructions = PrescriptionInput.Instructions,
            AllowRefills = PrescriptionInput.AllowRefills,
            RefillsAllowed = PrescriptionInput.RefillsAllowed,
            PrescribedDate = DateTime.Now
        };

        await _prescriptionService.CreateAsync(prescription);
        await _notificationService.SendPrescriptionNotificationAsync(prescription);

        TempData["SuccessMessage"] = "Prescription created successfully";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostExportScheduleAsync(DateTime startDate, DateTime endDate)
    {
        var user = await _userManager.GetUserAsync(User);
        var appointments = await _doctorService.GetDoctorAppointmentsAsync(
            user.Doctor.Id,
            startDate,
            endDate);

        // Generate CSV content
        var csvContent = "Patient,Date,Time,Status, Reason\n";
        foreach (var appt in appointments)
        {
            csvContent += $"{appt.Patient.FullName},{appt.AppointmentDateTime.ToShortDateString()}," +
                         $"{appt.AppointmentDateTime.ToShortTimeString()},{appt.Status}, {appt.Reason}\n";
        }

        return File(System.Text.Encoding.UTF8.GetBytes(csvContent), "text/csv",
            $"DoctorSchedule_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}.csv");
    }

    private List<SelectListItem> GetMedicationsList()
    {
        return new List<SelectListItem>
    {
        new SelectListItem { Value = "Amoxicillin", Text = "Amoxicillin (Antibiotic)" },
        new SelectListItem { Value = "Lisinopril", Text = "Lisinopril (Blood Pressure)" },
        new SelectListItem { Value = "Atorvastatin", Text = "Atorvastatin (Cholesterol)" },
        new SelectListItem { Value = "Metformin", Text = "Metformin (Diabetes)" },
        new SelectListItem { Value = "Albuterol", Text = "Albuterol (Asthma)" }
    };
    }
}