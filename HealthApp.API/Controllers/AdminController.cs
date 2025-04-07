using HealthApp.Domain.Entities;
using HealthApp.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace HealthApp.Razor.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IDoctorService _doctorService;
        private readonly IAppointmentRepository _appointmentService;

        public AdminController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IDoctorService doctorService,
            IAppointmentRepository appointmentService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _doctorService = doctorService;
            _appointmentService = appointmentService;
        }

        public async Task<IActionResult> Dashboard()
        {
            var model = new AdminDashboardViewModel
            {
                UserCount = await _userManager.Users.CountAsync(),
                DoctorCount = await _doctorService.GetCountAsync(),
                RecentAppointments = await _appointmentService.GetRecentAsync(10),
                PendingApprovals = await _userManager.Users
                    .Where(u => u.LockoutEnd.HasValue)
                    .CountAsync()
            };

            return View(model);
        }

        public async Task<IActionResult> ManageUsers()
        {
            var users = await _userManager.Users.ToListAsync();
            return View(users);
        }

        public async Task<IActionResult> ManageDoctors()
        {
            var doctors = await _doctorService.GetAllDoctorsAsync();
            return View(doctors);
        }

        public async Task<IActionResult> ManageAppointments()
        {
            var appointments = await _appointmentService.GetAllAsync();
            return View(appointments);
        }

        public async Task<IActionResult> Reports()
        {
            return View();
        }

        public async Task<IActionResult> SystemSettings()
        {
            return View();
        }
    }

    public class AdminDashboardViewModel
    {
        public int UserCount { get; set; }
        public int DoctorCount { get; set; }
        public IEnumerable<Appointment> RecentAppointments { get; set; }
        public int PendingApprovals { get; set; }
    }
}