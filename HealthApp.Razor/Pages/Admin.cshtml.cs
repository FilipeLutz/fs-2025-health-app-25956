using HealthApp.Domain.Entities;
using HealthApp.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace HealthApp.Razor.Pages;

[Authorize(Roles = "Admin")]
public class UserManagementModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IDoctorService _doctorService;
    private readonly IAppointmentRepository _appointmentService;
    private readonly INotificationService _notificationService;

    public UserManagementModel(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IDoctorService doctorService,
        IAppointmentRepository appointmentService,
        INotificationService notificationService)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _doctorService = doctorService;
        _appointmentService = appointmentService;
        _notificationService = notificationService;
    }

    // User Management
    public List<UserViewModel> Users { get; set; }
    public List<IdentityRole> AllRoles { get; set; }
    public List<SelectListItem> StatusOptions { get; set; } = new List<SelectListItem>
    {
        new SelectListItem { Value = "Active", Text = "Active" },
        new SelectListItem { Value = "Inactive", Text = "Inactive" }
    };

    // Appointment Management
    public List<Appointment> AllAppointments { get; set; }
    public List<Doctor> Doctors { get; set; }

    // Report Generation
    [BindProperty]
    public ReportFilterModel ReportFilter { get; set; }

    // System Notification
    [BindProperty]
    public SystemNotificationModel SystemNotification { get; set; }

    public class UserViewModel
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public string Status { get; set; }
        public List<string> Roles { get; set; }
    }

    public class ReportFilterModel
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? DoctorId { get; set; }
        public string Status { get; set; }
    }

    public class SystemNotificationModel
    {
        [Required]
        public string Title { get; set; }
        [Required]
        public string Message { get; set; }
        public string TargetRole { get; set; }
        public bool SendEmail { get; set; }
    }

    public async Task OnGetAsync()
    {
        await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        var users = await _userManager.Users.ToListAsync();
        AllRoles = await _roleManager.Roles.ToListAsync();
        Doctors = (await _doctorService.GetAllDoctorsAsync()).ToList();
        var allAppointments = await _appointmentService.GetAllAsync();
        AllAppointments = _appointmentService.GetAll()
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .OrderByDescending(a => a.AppointmentDateTime)
            .Take(50)
            .ToList();

        Users = new List<UserViewModel>();
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            Users.Add(new UserViewModel
            {
                Id = user.Id,
                Email = user.Email,
                Status = user.LockoutEnd.HasValue && user.LockoutEnd > DateTime.Now ? "Inactive" : "Active",
                Roles = roles.ToList()
            });
        }
    }

    // User Management Actions
    public async Task<IActionResult> OnPostToggleRoleAsync(string userId, string role)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound();

        var isInRole = await _userManager.IsInRoleAsync(user, role);

        if (isInRole)
        {
            await _userManager.RemoveFromRoleAsync(user, role);
            TempData["SuccessMessage"] = $"Removed {role} role from user";
        }
        else
        {
            await _userManager.AddToRoleAsync(user, role);
            TempData["SuccessMessage"] = $"Added {role} role to user";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostToggleStatusAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound();

        if (user.LockoutEnd.HasValue && user.LockoutEnd > DateTime.Now)
        {
            // Activate user
            await _userManager.SetLockoutEndDateAsync(user, null);
            TempData["SuccessMessage"] = "User activated successfully";
        }
        else
        {
            // Deactivate user
            await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
            TempData["SuccessMessage"] = "User deactivated successfully";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteUserAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound();

        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return Page();
        }

        TempData["SuccessMessage"] = "User deleted successfully";
        return RedirectToPage();
    }

    // Appointment Management Actions
    public async Task<IActionResult> OnPostCancelAppointmentAsync(int appointmentId, string reason, bool overrideRestriction = false)
    {
        var appointment = await _appointmentService.GetByIdAsync(appointmentId);
        if (appointment == null)
            return NotFound();

        if (!overrideRestriction && appointment.AppointmentDateTime < DateTime.Now.AddHours(48))
        {
            TempData["ErrorMessage"] = "Cannot cancel within 48 hours unless override is specified";
            return RedirectToPage();
        }

        await _appointmentService.CancelAppointmentAsync(appointmentId, reason);
        await _notificationService.SendAppointmentCancellationAsync(appointment);

        TempData["SuccessMessage"] = $"Appointment cancelled{(overrideRestriction ? " (override used)" : "")}";
        return RedirectToPage();
    }

    // Report Generation
    public async Task<IActionResult> OnPostGenerateReportAsync()
    {
        var query = _appointmentService.GetAll()
            .Include(a => a.Patient)
            .Include(a => a.Doctor);

        if (ReportFilter.StartDate.HasValue)
            query = (Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<Appointment, Doctor?>)query.Where(a => a.AppointmentDateTime >= ReportFilter.StartDate.Value);

        if (ReportFilter.EndDate.HasValue)
            query = (Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<Appointment, Doctor?>)query.Where(a => a.AppointmentDateTime <= ReportFilter.EndDate.Value);

        if (ReportFilter.DoctorId.HasValue)
            query = (Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<Appointment, Doctor?>)query.Where(a => a.DoctorId == ReportFilter.DoctorId.Value);

        if (!string.IsNullOrEmpty(ReportFilter.Status))
            query = (Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<Appointment, Doctor?>)query.Where(a => a.Status == ReportFilter.Status);

        var appointments = await query.ToListAsync();

        // Generate CSV content
        var csvContent = "Date,Time,Patient,Doctor,Status,Reason\n";
        foreach (var appt in appointments)
        {
            csvContent += $"{appt.AppointmentDateTime.ToShortDateString()}," +
                         $"{appt.AppointmentDateTime.ToShortTimeString()}," +
                         $"{appt.Patient.FullName}," +
                         $"Dr. {appt.Doctor.FullName}," +
                         $"{appt.Status}," +
                         $"{appt.Reason}\n";
        }

        return File(System.Text.Encoding.UTF8.GetBytes(csvContent), "text/csv",
            $"AppointmentsReport_{DateTime.Now:yyyyMMdd}.csv");
    }

    // System Notification
    public async Task<IActionResult> OnPostSendSystemNotificationAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadDataAsync();
            return Page();
        }

        var users = _userManager.Users.AsQueryable();

        if (!string.IsNullOrEmpty(SystemNotification.TargetRole))
            users = users.Where(u => _userManager.IsInRoleAsync(u, SystemNotification.TargetRole).Result);

        foreach (var user in await users.ToListAsync())
        {
            await _notificationService.CreateNotificationAsync(
                user.Id,
                SystemNotification.Message,
                "SystemNotification",
                null);

            if (SystemNotification.SendEmail)
            {
               // await _emailService.SendAsync(user.Email, SystemNotification.Title, SystemNotification.Message);
            }
        }

        TempData["SuccessMessage"] = "Notification sent successfully";
        return RedirectToPage();
    }
}