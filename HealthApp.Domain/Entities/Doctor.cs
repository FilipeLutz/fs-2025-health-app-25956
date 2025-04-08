using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HealthApp.Domain.Entities;

public class Doctor
{
    public int Id { get; set; }
    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; }
    [Required]
    [MaxLength(100)]
    public string LastName { get; set; }
    public string FullName => $"{FirstName} {LastName}";
    public string Email { get; set; }
    public string PhoneNumber { get; set; }
    public string Specialization { get; set; }
    public string LicenseNumber { get; set; }

    [ForeignKey("UserId")]
    public string UserId { get; set; }
    public ApplicationUser User { get; set; }

    public ICollection<Schedule> Schedules { get; set; }
    public ICollection<Appointment> Appointments { get; set; }
    public ICollection<Prescription> Prescriptions { get; set; }
}
