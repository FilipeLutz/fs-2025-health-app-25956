using System;
using System.ComponentModel.DataAnnotations;

namespace HealthApp.Domain.Entities
{
    public class Feedback
    {
        public int Id { get; set; }

        [Required]
        public int AppointmentId { get; set; }

        public Appointment Appointment { get; set; }

        [Required]
        [Range(1, 5)]
        public int Rating { get; set; }

        [MaxLength(1000)]
        public string Comment { get; set; }

        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    }
}