using HealthApp.Domain.Entities;
using HealthApp.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HealthApp.API.Controllers
{
    [Authorize(Roles = "Admin,Doctor")]
    [Route("api/[controller]")]
    [ApiController]
    public class DoctorsController : ControllerBase
    {
        private readonly IDoctorService _doctorService;

        public DoctorsController(IDoctorService doctorService)
        {
            _doctorService = doctorService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllDoctors()
        {
            var doctors = await _doctorService.GetAllDoctorsAsync();
            return Ok(doctors);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetDoctor(int id)
        {
            var doctor = await _doctorService.GetDoctorByIdAsync(id);
            if (doctor == null) return NotFound();
            return Ok(doctor);
        }

        [HttpGet("specialization/{specialization}")]
        public async Task<IActionResult> GetBySpecialization(string specialization)
        {
            var doctors = await _doctorService.GetDoctorsBySpecializationAsync(specialization);
            return Ok(doctors);
        }

        [HttpGet("{id}/schedule")]
        public async Task<IActionResult> GetDoctorSchedule(int id)
        {
            var schedule = await _doctorService.GetDoctorScheduleAsync(id);
            return Ok(schedule);
        }

        [HttpPost("{id}/schedule")]
        public async Task<IActionResult> AddSchedule(int id, [FromBody] Schedule schedule)
        {
            await _doctorService.AddScheduleAsync(schedule);
            return CreatedAtAction(nameof(GetDoctorSchedule), new { id }, schedule);
        }

        [HttpPut("schedule/{scheduleId}")]
        public async Task<IActionResult> UpdateSchedule(int scheduleId, [FromBody] Schedule schedule)
        {
            if (scheduleId != schedule.Id) return BadRequest();
            await _doctorService.UpdateScheduleAsync(schedule);
            return NoContent();
        }

        [HttpDelete("schedule/{scheduleId}")]
        public async Task<IActionResult> RemoveSchedule(int scheduleId)
        {
            await _doctorService.RemoveScheduleAsync(scheduleId);
            return NoContent();
        }
    }
}