using HealthApp.Domain.Entities;
using HealthApp.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HealthApp.Razor.Pages;

[Authorize(Roles = "Patient")]
public class BookAppointmentModel : PageModel
{
    private readonly IAppointmentRepository _appointmentService;
    private readonly IDoctorService _doctorService;
    private readonly UserManager<ApplicationUser> _userManager;

    public BookAppointmentModel(IAppointmentRepository appointmentService,
                              IDoctorService doctorService,
                              UserManager<ApplicationUser> userManager)
    {
        _appointmentService = appointmentService;
        _doctorService = doctorService;
        _userManager = userManager;
    }

    [BindProperty]
    public AppointmentCreateDto Input { get; set; }

    public List<SelectListItem> Doctors { get; set; }
    public List<DateTime> AvailableSlots { get; set; }

    public async Task OnGetAsync(int? doctorId, DateTime? date)
    {
        Doctors = (await _doctorService.GetAllDoctorsAsync())
            .Select(d => new SelectListItem
            {
                Value = d.Id.ToString(),
                Text = $"{d.FirstName} {d.LastName} ({d.Specialization})"
            }).ToList();

        if (doctorId.HasValue && date.HasValue)
        {
            var schedules = await _doctorService.GetDoctorScheduleAsync(doctorId.Value);
            // Implement logic to calculate available slots based on schedule and existing appointments
            // This is a simplified version - you'll need to expand it
            AvailableSlots = schedules
                .Where(s => s.DayOfWeek == date.Value.DayOfWeek)
                .SelectMany(s => GenerateTimeSlots(date.Value.Date.Add(s.StartTime), date.Value.Date.Add(s.EndTime), TimeSpan.FromMinutes(30)))
                .ToList();
        }
    }

    private IEnumerable<DateTime> GenerateTimeSlots(DateTime start, DateTime end, TimeSpan duration)
    {
        for (var time = start; time < end; time = time.Add(duration))
        {
            yield return time;
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        var user = await _userManager.GetUserAsync(User);
        Input.PatientId = user.Patient.Id;

        try
        {
            var appointment = await _appointmentService.CreateAppointmentAsync(Input);
            TempData["SuccessMessage"] = "Appointment booked successfully!";
            return RedirectToPage("/Patient/Dashboard");
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await OnGetAsync(Input.DoctorId, Input.AppointmentDateTime.Date);
            return Page();
        }
    }
}