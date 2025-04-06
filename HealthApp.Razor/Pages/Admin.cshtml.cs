using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;

namespace HealthApp.Razor.Pages
{
    public class AdminModel : PageModel
    {
        public List<UserViewModel> Users { get; set; }

        public void OnGet()
        {
            Users = new List<UserViewModel>
            {
                new UserViewModel { Id = 1, UserName = "user1", Email = "user1@example.com" },
                new UserViewModel { Id = 2, UserName = "user2", Email = "user2@example.com" }
            };
        }
    }

    public class UserViewModel
    {
        public int Id { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
    }
}