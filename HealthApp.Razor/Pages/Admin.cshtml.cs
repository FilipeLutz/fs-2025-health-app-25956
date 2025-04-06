using HealthApp.Domain.Entities;
using HealthApp.Domain.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HealthApp.Razor.Pages
{
    [Authorize(Roles = "Admin")]
    public class AdminModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ApplicationDbContext _context;

        public AdminModel(
            UserManager<IdentityUser> userManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public List<UserWithRole> Users { get; set; } = new();
        public List<Patient> Patients { get; set; } = new();
        public List<Doctor> Doctors { get; set; } = new();

        public class UserWithRole
        {
            public required string UserId { get; set; }
            public required string Email { get; set; }
            public required string Role { get; set; }
        }

        public async Task OnGetAsync()
        {
            // Load users with roles
            var identityUsers = await _userManager.Users.ToListAsync();
            foreach (var user in identityUsers)
            {
                var roles = await _userManager.GetRolesAsync(user);
                Users.Add(new UserWithRole
                {
                    UserId = user.Id,
                    Email = user.Email ?? string.Empty,
                    Role = roles.FirstOrDefault() ?? "No Role"
                });
            }

            Patients = await _context.Patients.ToListAsync();
            Doctors = await _context.Doctors.ToListAsync();
        }

        [BindProperty]
        public InputModel Input { get; set; } = new InputModel
        {
            Email = string.Empty,
            Password = string.Empty,
            Role = string.Empty,
            FullName = string.Empty
        };

        public class InputModel
        {
            public required string Email { get; set; }
            public required string Password { get; set; }
            public required string Role { get; set; }
            public required string FullName { get; set; }
            public string? Specialization { get; set; }
        }

        public async Task<IActionResult> OnPostCreateUserAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = new IdentityUser { UserName = Input.Email, Email = Input.Email };
            var result = await _userManager.CreateAsync(user, Input.Password);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, Input.Role);

                switch (Input.Role)
                {
                    case "Patient":
                        _context.Patients.Add(new Patient
                        {
                            UserId = user.Id,
                            Name = Input.FullName,
                            Email = Input.Email
                            
                        });
                        break;
                    case "Doctor":
                        _context.Doctors.Add(new Doctor
                        {
                            UserId = user.Id,
                            Name = Input.FullName,
                            Email = Input.Email
                        });
                        break;
                }

                await _context.SaveChangesAsync();
                return RedirectToPage();
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return Page();
        }
    }
}