using HealthApp.Domain;
using HealthApp.Razor.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace HealthApp.Razor.Pages
{
    [Authorize(Roles = "Admin")]
    public class AdminModel : PageModel
    {
        public string UserId { get; set; }
        public List<Patient> Patients { get; set; }
        public void OnGet()
        {
            UserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            Patients = MockData.Patients();
        }
    }
}
