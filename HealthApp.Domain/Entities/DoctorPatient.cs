using HealthApp.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace HealthApp.Razor.Data
{
    public class DoctorPatient
    {
        public int DoctorId { get; set; }
        public Doctor? Doctor { get; set; }
        public int PatientId { get; set; }
        public Patient? Patient { get; set; }
    }
}
