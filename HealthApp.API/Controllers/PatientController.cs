using HealthApp.Domain.Entities;
using HealthApp.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace HealthApp.API.Controllers
{
    [Authorize(Roles = "Admin,Doctor")]
    [Route("api/[controller]")]
    [ApiController]
    public class PatientsController : ControllerBase
    {
        private readonly IPatientService _patientService;

        public PatientsController(IPatientService patientService)
        {
            _patientService = patientService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllPatients()
        {
            var patients = await _patientService.GetAllPatientsAsync();
            return Ok(patients);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetPatient(int id)
        {
            var patient = await _patientService.GetPatientByIdAsync(id);
            if (patient == null) return NotFound();
            return Ok(patient);
        }

        [HttpGet("{id}/appointments")]
        public async Task<IActionResult> GetPatientAppointments(int id)
        {
            var appointments = await _patientService.GetPatientAppointmentsAsync(id);
            return Ok(appointments);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePatient(int id, [FromBody] Patient patient)
        {
            if (id != patient.Id) return BadRequest();
            await _patientService.UpdatePatientAsync(patient);
            return NoContent();
        }
    }
}