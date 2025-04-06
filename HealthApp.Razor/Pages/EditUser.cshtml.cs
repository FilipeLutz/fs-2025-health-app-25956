using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;

namespace HealthApp.Razor.Pages
{
    public class EditUserModel : PageModel
    {
        [BindProperty]
        public string UserName { get; set; }

        [BindProperty]
        public string Email { get; set; }

        [BindProperty]
        public List<RoleViewModel> Roles { get; set; }

        public void OnGet()
        {
            // Initialize properties here if needed
        }

        public void OnPost()
        {
            // Handle form submission
        }
    }

    public class RoleViewModel
    {
        public string RoleName { get; set; }
        public bool IsSelected { get; set; }
    }
}