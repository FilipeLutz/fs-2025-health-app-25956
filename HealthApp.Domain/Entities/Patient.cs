using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HealthApp.Domain.Entities;

public class Patient
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; }

    [Required]
    [MaxLength(100)]
    public string LastName { get; set; }

    public string FullName => $"{FirstName} {LastName}";

    [MaxLength(500)]
    public string Address { get; set; }

    public DateTime DateOfBirth { get; set; }
    public string BloodType { get; set; }
    public string Allergies { get; set; }
    public string MedicalHistory { get; set; }
    public string InsuranceInfo { get; set; }

    [ForeignKey("UserId")]
    public string UserId { get; set; }
    public ApplicationUser User { get; set; }

    public ICollection<Appointment> Appointments { get; set; }
    public ICollection<Prescription> Prescriptions { get; set; }
    public string Email { get; set; }
    public string PhoneNumber { get; set; }
}