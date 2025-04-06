using HealthApp.Domain.Entities;
using HealthApp.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HealthApp.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class AppointmentsController : ControllerBase
{
    private readonly IAppointmentRepository _appointmentService;

    public AppointmentsController(IAppointmentRepository appointmentService)
    {
        _appointmentService = appointmentService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Appointment>>> GetAll()
    {
        var appointments = await _appointmentService.GetAllAsync();
        return Ok(appointments);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Appointment>> GetById(int id)
    {
        var appointment = await _appointmentService.GetByIdAsync(id);
        if (appointment == null) return NotFound();
        return Ok(appointment);
    }

    [HttpGet("patient/{patientId}")]
    public async Task<ActionResult<IEnumerable<Appointment>>> GetByPatient(int patientId)
    {
        var appointments = await _appointmentService.GetByPatientIdAsync(patientId);
        return Ok(appointments);
    }



    [HttpGet("doctor/{doctorId}")]
    public async Task<ActionResult<IEnumerable<Appointment>>> GetByDoctor(int doctorId)
    {
        var appointments = await _appointmentService.GetByDoctorIdAsync(doctorId);
        return Ok(appointments);
    }

    [HttpPost]
    public async Task<ActionResult<Appointment>> Create(Appointment appointment)
    {
        try
        {
            await _appointmentService.AddAsync(appointment);
            return CreatedAtAction(nameof(GetById), new { id = appointment.Id }, appointment);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, Appointment appointment)
    {
        if (id != appointment.Id) return BadRequest();

        try
        {
            await _appointmentService.UpdateAsync(appointment);
            return NoContent();
        }
        catch
        {
            if (!await _appointmentService.ExistsAsync(id)) return NotFound();
            throw;
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _appointmentService.DeleteAsync(id);
        return NoContent();
    }

    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> Cancel(int id, [FromBody] string reason)
    {
        var success = await _appointmentService.CancelAppointmentAsync(id, reason);
        if (!success) return BadRequest("Cannot cancel appointment within 48 hours of scheduled time.");
        return NoContent();
    }

    [HttpGet("pending/{doctorId}")]
    public async Task<ActionResult<IEnumerable<Appointment>>> GetPending(int doctorId)
    {
        var appointments = await _appointmentService.GetPendingAppointmentsAsync(doctorId);
        return Ok(appointments);
    }
}