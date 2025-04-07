using HealthApp.Domain.Entities;
using HealthApp.Domain.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace HealthApp.Razor.Areas.Identity.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public RegisterModel(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ReturnUrl { get; set; }

        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        public class InputModel
        {
            [Required]
            [Display(Name = "Full Name")]
            public string FullName { get; set; }

            [Required]
            [EmailAddress]
            [Display(Name = "Email")]
            public string Email { get; set; }

            [Required]
            [StringLength(100, MinimumLength = 8)]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [Compare("Password", ErrorMessage = "Passwords don't match.")]
            public string ConfirmPassword { get; set; }

            [Required]
            [Display(Name = "User Type")]
            public string UserType { get; set; }

            [Display(Name = "Specialization")]
            public string? Specialization { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            ReturnUrl = returnUrl;
            Input = new InputModel();
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            if (ModelState.IsValid)
            {
                var user = new ApplicationUser { UserName = Input.Email, Email = Input.Email };
                var result = await _userManager.CreateAsync(user, Input.Password);

                if (result.Succeeded)
                {
                    if (!await _roleManager.RoleExistsAsync(Input.UserType))
                    {
                        await _roleManager.CreateAsync(new IdentityRole(Input.UserType));
                    }

                    await _userManager.AddToRoleAsync(user, Input.UserType);

                    if (Input.UserType == "Patient")
                    {
                        _context.Set<Patient>().Add(new Patient
                        {
                            UserId = user.Id
                        });
                    }
                    else if (Input.UserType == "Doctor")
                    {
                        _context.Set<Doctor>().Add(new Doctor
                        {
                            UserId = user.Id,
                            Specialization = Input.Specialization,
                            LicenseNumber = "TEMP-" + Guid.NewGuid().ToString()[..8]
                        });
                    }

                    await _context.SaveChangesAsync();
                    await _signInManager.SignInAsync(user, isPersistent: false);

                    // Redirect to appropriate dashboard based on role
                    if (Input.UserType == "Doctor")
                    {
                        return RedirectToPage("/Doctor"); // Redirect to Doctor Dashboard
                    }
                    else if (Input.UserType == "Patient")
                    {
                        return RedirectToPage("/Patient"); // Redirect to Patient Dashboard
                    }

                    // Default redirect if no specific role is found
                    return LocalRedirect(returnUrl);
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return Page();
        }
    }
}