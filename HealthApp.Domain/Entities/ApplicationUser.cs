using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace HealthApp.Domain.Entities
{
    public class ApplicationUser : IdentityUser
    {
        public Patient Patient { get; set; }
        public Doctor Doctor { get; set; }
    }
}
