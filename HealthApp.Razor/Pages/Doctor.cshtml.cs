using HealthApp.Domain.Entities;
using HealthApp.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HealthApp.Razor.Pages;

public class DoctorModel : PageModel
{
    private readonly IAppointmentRepository _appointmentService;

    public DoctorModel(IAppointmentRepository appointmentService)
    {
        _appointmentService = appointmentService;
    }

    public List<Appointment> Appointments { get; set; } = new();
    public List<Appointment> PendingAppointments { get; set; } = new();

    public async Task OnGetAsync()
    {
        var doctorId = 1; // Get from auth in real app
        Appointments = (await _appointmentService.GetByDoctorIdAsync(doctorId)).ToList();
        PendingAppointments = (await _appointmentService.GetPendingAppointmentsAsync(doctorId)).ToList();
    }
}