using HealthApp.Domain.Entities;
using HealthApp.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HealthApp.Razor.Pages
{
    public class AdminDashboardModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAppointmentRepository _appointmentService;
        private readonly INotificationService _notificationService;

        public AdminDashboardModel(
            UserManager<ApplicationUser> userManager,
            IAppointmentRepository appointmentService,
            INotificationService notificationService)
        {
            _userManager = userManager;
            _appointmentService = appointmentService;
            _notificationService = notificationService;
        }

        // List of Users (Admins, Doctors, Patients)
        public List<UserViewModel> Users { get; set; }

        // All Appointments
        public List<Appointment> AllAppointments { get; set; }

        // Notifications
        public SystemNotificationModel Notification { get; set; }

        public class UserViewModel
        {
            public string UserId { get; set; }
            public string UserName { get; set; }
            public string Email { get; set; }
            public string Role { get; set; }
            public string Status { get; set; }
        }

        public class SystemNotificationModel
        {
            public string Title { get; set; }
            public string Message { get; set; }
        }

        public async Task OnGetAsync()
        {
            var users = _userManager.Users.ToList(); 
            Users = new List<UserViewModel>();

            
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user); 

                Users.Add(new UserViewModel
                {
                    UserId = user.Id,
                    UserName = user.UserName,
                    Email = user.Email,
                    Role = roles.FirstOrDefault(), 
                    Status = user.LockoutEnd.HasValue && user.LockoutEnd > DateTime.Now ? "Inactive" : "Active"
                });
            }

            AllAppointments = await _appointmentService.GetAllAppointmentsAsync();
        }

        public async Task<IActionResult> OnPostDeleteUserAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            var result = await _userManager.DeleteAsync(user);

            if (result.Succeeded)
                TempData["SuccessMessage"] = "User deleted successfully.";
            else
                TempData["ErrorMessage"] = "Failed to delete user.";

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostSendNotificationAsync()
        {
            if (ModelState.IsValid)
            {
                // Send notification to all users (or specific roles)
                await _notificationService.SendSystemNotificationAsync(Notification.Title, Notification.Message);
                TempData["SuccessMessage"] = "Notification sent successfully.";
            }

            return RedirectToPage();
        }
    }
}