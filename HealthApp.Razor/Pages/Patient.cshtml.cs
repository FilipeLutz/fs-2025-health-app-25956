using HealthApp.Domain.Entities;
using HealthApp.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace HealthApp.Razor.Pages;

[Authorize(Roles = "Patient")]
public class PatientModel : PageModel
{
    private readonly IAppointmentRepository _appointmentService;
    private readonly IDoctorService _doctorService;
    private readonly IPrescriptionService _prescriptionService;
    private readonly INotificationService _notificationService;
    private readonly UserManager<ApplicationUser> _userManager;

    public PatientModel(
        IAppointmentRepository appointmentService,
        IDoctorService doctorService,
        IPrescriptionService prescriptionService,
        INotificationService notificationService,
        UserManager<ApplicationUser> userManager)
    {
        _appointmentService = appointmentService;
        _doctorService = doctorService;
        _prescriptionService = prescriptionService;
        _notificationService = notificationService;
        _userManager = userManager;
    }

    // Data properties
    public List<Appointment> UpcomingAppointments { get; set; }
    public List<Appointment> PastAppointments { get; set; }
    public List<Prescription> ActivePrescriptions { get; set; }
    public List<Prescription> ExpiredPrescriptions { get; set; }

    public List<Doctor> SearchResults { get; set; }
    public List<SelectListItem> Specializations { get; set; }

    // Filters & bindings
    [BindProperty(SupportsGet = true)]
    public string StatusFilter { get; set; }
    [BindProperty(SupportsGet = true)]
    public DateTime? DateFilter { get; set; }
    [BindProperty]
    public string DoctorSearchTerm { get; set; }
    [BindProperty]
    public string SpecializationFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public string SearchTerm { get; set; }

    public string GetStatusBadgeClass(string status)
    {
        return status switch
        {
            "Pending" => "badge-warning",
            "Approved" => "badge-success",
            "Completed" => "badge-primary",
            "Cancelled" => "badge-danger",
            _ => "badge-secondary"
        };
    }

    public async Task OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        var now = DateTime.Now;

        // Appointments
        var appointments = _appointmentService.GetPatientAppointments(user.Id);

        if (!string.IsNullOrEmpty(StatusFilter))
            appointments = appointments.Where(a => a.Status == StatusFilter);

        if (DateFilter.HasValue)
            appointments = appointments.Where(a => a.AppointmentDateTime.Date == DateFilter.Value.Date);

        var allAppointments = appointments.ToList();
        UpcomingAppointments = allAppointments.Where(a => a.AppointmentDateTime >= now).OrderBy(a => a.AppointmentDateTime).ToList();
        PastAppointments = allAppointments.Where(a => a.AppointmentDateTime < now).OrderByDescending(a => a.AppointmentDateTime).ToList();

        // Prescriptions
        var prescriptions = await _prescriptionService.GetByPatientIdAsync(user.Patient.Id);
        ActivePrescriptions = prescriptions.Where(p => p.ExpiryDate == null || p.ExpiryDate > now).ToList();
        ExpiredPrescriptions = prescriptions.Where(p => p.ExpiryDate != null && p.ExpiryDate <= now).ToList();

        // Specializations
        Specializations = (await _doctorService.GetAllDoctorsAsync())
            .Select(d => d.Specialization)
            .Distinct()
            .Select(s => new SelectListItem { Value = s, Text = s })
            .ToList();
    }

    public async Task<IActionResult> OnPostSearchDoctorsAsync()
    {
        var doctors = await _doctorService.GetAllDoctorsAsync();

        if (!string.IsNullOrEmpty(DoctorSearchTerm))
        {
            doctors = doctors.Where(d =>
                d.FirstName.Contains(DoctorSearchTerm, StringComparison.OrdinalIgnoreCase) ||
                d.LastName.Contains(DoctorSearchTerm, StringComparison.OrdinalIgnoreCase) ||
                d.Specialization.Contains(DoctorSearchTerm, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        if (!string.IsNullOrEmpty(SpecializationFilter))
        {
            doctors = doctors.Where(d => d.Specialization == SpecializationFilter).ToList();
        }

        SearchResults = doctors.ToList();
        await OnGetAsync(); // Refresh appointments and prescriptions
        return Page();
    }

    public async Task<IActionResult> OnPostCancelAsync(int id, string reason)
    {
        var user = await _userManager.GetUserAsync(User);
        var appointment = await _appointmentService.GetByIdAsync(id);

        if (appointment == null || appointment.PatientId != user.Patient.Id)
        {
            TempData["ErrorMessage"] = "Invalid appointment.";
            return RedirectToPage();
        }

        if (appointment.AppointmentDateTime < DateTime.Now.AddHours(48))
        {
            TempData["ErrorMessage"] = "Appointments can only be canceled 48h in advance.";
            return RedirectToPage();
        }

        try
        {
            await _appointmentService.CancelAppointmentAsync(id, reason);
            await _notificationService.SendAppointmentCancellationAsync(appointment);
            TempData["SuccessMessage"] = "Appointment canceled.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRequestRenewalAsync(int prescriptionId)
    {
        var user = await _userManager.GetUserAsync(User);
        var prescription = await _prescriptionService.GetByIdAsync(prescriptionId);

        if (prescription == null || prescription.PatientId != user.Patient.Id)
        {
            TempData["ErrorMessage"] = "Prescription not found.";
            return RedirectToPage();
        }

        try
        {
            await _prescriptionService.RequestRenewalAsync(prescriptionId, user.Id);
            TempData["SuccessMessage"] = "Renewal request sent.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Error: {ex.Message}";
        }

        return RedirectToPage();
    }
}