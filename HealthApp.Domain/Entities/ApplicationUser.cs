using HealthApp.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

public class ApplicationUser : IdentityUser
{
    [Required, PersonalData]
    public string FirstName { get; set; }

    [Required, PersonalData]
    public string LastName { get; set; }

    public Patient Patient { get; set; }
    public Doctor Doctor { get; set; }
}