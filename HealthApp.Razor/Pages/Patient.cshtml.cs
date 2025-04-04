using HealthApp.Domain.Entities;
using HealthApp.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HealthApp.Razor.Pages;

public class PatientModel : PageModel
{
    private readonly IAppointmentRepository _appointmentService;

    public PatientModel(IAppointmentRepository appointmentService)
    {
        _appointmentService = appointmentService;
    }

    public List<Appointment> Appointments { get; set; } = new();

    public async Task OnGetAsync()
    {
        var patientId = 1; // Get from auth in real app
        Appointments = (await _appointmentService.GetByPatientIdAsync(patientId)).ToList();
    }
}