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
    public class PrescriptionsController : ControllerBase
    {
        private readonly IPrescriptionService _prescriptionService;

        public PrescriptionsController(IPrescriptionService prescriptionService)
        {
            _prescriptionService = prescriptionService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllPrescriptions()
        {
            var prescriptions = await _prescriptionService.GetAllAsync();
            return Ok(prescriptions);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetPrescription(int id)
        {
            var prescription = await _prescriptionService.GetByIdAsync(id);
            if (prescription == null) return NotFound();
            return Ok(prescription);
        }

        [HttpGet("patient/{patientId}")]
        public async Task<IActionResult> GetByPatient(int patientId)
        {
            var prescriptions = await _prescriptionService.GetByPatientIdAsync(patientId);
            return Ok(prescriptions);
        }

        [HttpGet("doctor/{doctorId}")]
        public async Task<IActionResult> GetByDoctor(int doctorId)
        {
            var prescriptions = await _prescriptionService.GetByDoctorIdAsync(doctorId);
            return Ok(prescriptions);
        }

        [HttpPost]
        public async Task<IActionResult> CreatePrescription([FromBody] Prescription prescription)
        {
            await _prescriptionService.CreateAsync(prescription);
            return CreatedAtAction(nameof(GetPrescription), new { id = prescription.Id }, prescription);
        }

        [HttpPost("{id}/renew")]
        public async Task<IActionResult> RenewPrescription(int id)
        {
            var success = await _prescriptionService.RenewAsync(id);
            if (!success) return BadRequest("Prescription cannot be renewed");
            return Ok();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePrescription(int id)
        {
            await _prescriptionService.DeleteAsync(id);
            return NoContent();
        }
    }
}