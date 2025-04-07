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
public class PatientDashboardModel : PageModel
{
    private readonly IAppointmentRepository _appointmentService;
    private readonly IDoctorService _doctorService;
    private readonly IPrescriptionService _prescriptionService;
    private readonly INotificationService _notificationService;
    private readonly UserManager<ApplicationUser> _userManager;

    public PatientDashboardModel(
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

    // Appointment Lists
    public List<Appointment> UpcomingAppointments { get; set; }
    public List<Appointment> PastAppointments { get; set; }
    public List<Prescription> ActivePrescriptions { get; set; }
    public List<Prescription> ExpiredPrescriptions { get; set; }

    // Filter Properties
    [BindProperty(SupportsGet = true)]
    public string SearchTerm { get; set; }
    [BindProperty(SupportsGet = true)]
    public string StatusFilter { get; set; }
    [BindProperty(SupportsGet = true)]
    public DateTime? DateFilter { get; set; }

    // Doctor Search Properties
    [BindProperty]
    public string DoctorSearchTerm { get; set; }
    [BindProperty]
    public string SpecializationFilter { get; set; }
    public List<Doctor> SearchResults { get; set; }
    public List<SelectListItem> Specializations { get; set; }

    // New Appointment Booking
    [BindProperty]
    public AppointmentInputModel AppointmentInput { get; set; }
    public List<DateTime> AvailableSlots { get; set; }

    public class AppointmentInputModel
    {
        [Required]
        public int DoctorId { get; set; }

        [Required]
        public DateTime AppointmentDateTime { get; set; }

        [Required]
        public string Reason { get; set; }
    }

    public async Task OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        var now = DateTime.Now;

        // Initialize specializations dropdown
        Specializations = (await _doctorService.GetAllDoctorsAsync())
            .Select(d => d.Specialization)
            .Distinct()
            .Select(s => new SelectListItem { Value = s, Text = s })
            .ToList();

        // Get filtered appointments
        var appointmentsQuery = _appointmentService.GetPatientAppointments(user.Id);

        if (!string.IsNullOrEmpty(StatusFilter))
        {
            appointmentsQuery = appointmentsQuery.Where(a => a.Status == StatusFilter);
        }

        if (DateFilter.HasValue)
        {
            appointmentsQuery = appointmentsQuery.Where(a => a.AppointmentDateTime.Date == DateFilter.Value.Date);
        }

        var allAppointments = appointmentsQuery.ToList();

        UpcomingAppointments = allAppointments
            .Where(a => a.AppointmentDateTime >= now)
            .OrderBy(a => a.AppointmentDateTime)
            .ToList();

        PastAppointments = allAppointments
            .Where(a => a.AppointmentDateTime < now)
            .OrderByDescending(a => a.AppointmentDateTime)
            .ToList();

        // Get prescriptions
        var prescriptions = await _prescriptionService.GetByPatientIdAsync(user.Patient.Id);
        ActivePrescriptions = prescriptions
            .Where(p => p.ExpiryDate == null || p.ExpiryDate > now)
            .ToList();

        ExpiredPrescriptions = prescriptions
            .Where(p => p.ExpiryDate != null && p.ExpiryDate <= now)
            .ToList();
    }

    public async Task<IActionResult> OnPostSearchDoctorsAsync()
    {
        var doctors = await _doctorService.GetAllDoctorsAsync();

        if (!string.IsNullOrEmpty(DoctorSearchTerm))
        {
            doctors = doctors.Where(d =>
                d.FirstName.Contains(DoctorSearchTerm) ||
                d.LastName.Contains(DoctorSearchTerm) ||
                d.Specialization.Contains(DoctorSearchTerm))
                .ToList();
        }

        if (!string.IsNullOrEmpty(SpecializationFilter))
        {
            doctors = doctors.Where(d => d.Specialization == SpecializationFilter)
                .ToList();
        }

        SearchResults = doctors.ToList();
        await OnGetAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostGetAvailableSlotsAsync(int doctorId, DateTime date)
    {
        var schedules = await _doctorService.GetDoctorScheduleAsync(doctorId);
        var appointments = await _doctorService.GetDoctorAppointmentsAsync(doctorId, date);

        AvailableSlots = schedules
            .Where(s => s.DayOfWeek == date.DayOfWeek)
            .SelectMany(s => GenerateTimeSlots(
                date.Date.Add(s.StartTime),
                date.Date.Add(s.EndTime),
                TimeSpan.FromMinutes(30)))
            .Where(slot => !appointments.Any(a => a.AppointmentDateTime == slot))
            .ToList();

        await OnGetAsync(); 
        return Page();
    }

    public async Task<IActionResult> OnPostBookAppointmentAsync()
    {
        if (!ModelState.IsValid)
        {
            await OnGetAsync();
            return Page();
        }

        var user = await _userManager.GetUserAsync(User);

        try
        {
            var appointment = new Appointment
            {
                DoctorId = AppointmentInput.DoctorId,
                PatientId = user.Patient.Id,
                AppointmentDateTime = AppointmentInput.AppointmentDateTime,
                EndDateTime = AppointmentInput.AppointmentDateTime.AddMinutes(30),
                Status = "Pending",
                Reason = AppointmentInput.Reason
            };

            await _appointmentService.AddAsync(appointment);
            await _notificationService.SendAppointmentConfirmationAsync(appointment);

            TempData["SuccessMessage"] = "Appointment booked successfully!";
            return RedirectToPage();
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await OnGetAsync();
            return Page();
        }
    }

    public async Task<IActionResult> OnPostCancelAsync(int id, string reason)
    {
        var user = await _userManager.GetUserAsync(User);
        var appointment = await _appointmentService.GetByIdAsync(id);

        if (appointment == null || appointment.PatientId != user.Patient.Id)
        {
            TempData["ErrorMessage"] = "Appointment not found";
            return RedirectToPage();
        }

        if (appointment.AppointmentDateTime < DateTime.Now.AddHours(48))
        {
            TempData["ErrorMessage"] = "Appointments can only be cancelled at least 48 hours in advance";
            return RedirectToPage();
        }

        try
        {
            await _appointmentService.CancelAppointmentAsync(id, reason);
            await _notificationService.SendAppointmentCancellationAsync(appointment);
            TempData["SuccessMessage"] = "Appointment cancelled successfully";
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
            TempData["ErrorMessage"] = "Prescription not found";
            return RedirectToPage();
        }

        if (prescription.ExpiryDate < DateTime.Now)
        {
            TempData["ErrorMessage"] = "This prescription has already expired";
            return RedirectToPage();
        }

        await _notificationService.CreateNotificationAsync(
            prescription.Doctor.UserId,
            $"Patient {user.Patient.FullName} has requested a renewal for prescription {prescription.Medication}",
            "PrescriptionRenewal",
            prescription.Id);

        TempData["SuccessMessage"] = "Renewal request sent to your doctor";
        return RedirectToPage();
    }

    private IEnumerable<DateTime> GenerateTimeSlots(DateTime start, DateTime end, TimeSpan duration)
    {
        for (var time = start; time < end; time = time.Add(duration))
        {
            yield return time;
        }
    }
}