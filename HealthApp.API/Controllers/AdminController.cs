using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using HealthApp.Domain.Entities;
using System.Threading.Tasks;
using System.Linq;

namespace HealthApp.Razor.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AdminController(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<IActionResult> AdminDashboard()
        {
            var users = _userManager.Users.ToList();
            return View(users);
        }

        public async Task<IActionResult> EditUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            var userRoles = await _userManager.GetRolesAsync(user);
            var allRoles = _roleManager.Roles.ToList();

            var model = new EditUserViewModel
            {
                UserId = user.Id,
                Name = user.UserName,
                Email = user.Email,
                Roles = allRoles.Select(r => new RoleSelectionModel
                {
                    RoleName = r.Name,
                    IsSelected = userRoles.Contains(r.Name)
                }).ToList()
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> EditUser(string userId, EditUserViewModel model)
        {
            if (userId != model.UserId) return BadRequest();

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            user.UserName = model.Name;
            user.Email = model.Email;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                foreach (var error in updateResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View(model);
            }

            var currentRoles = await _userManager.GetRolesAsync(user);
            var rolesToRemove = currentRoles.Except(model.Roles.Where(r => r.IsSelected).Select(r => r.RoleName)).ToList();
            var rolesToAdd = model.Roles.Where(r => r.IsSelected).Select(r => r.RoleName).Except(currentRoles).ToList();

            await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
            await _userManager.AddToRolesAsync(user, rolesToAdd);

            return RedirectToAction(nameof(AdminDashboard));
        }

        public async Task<IActionResult> DeleteUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                return RedirectToAction(nameof(AdminDashboard));
            }

            return View("Error");
        }
    }
}
