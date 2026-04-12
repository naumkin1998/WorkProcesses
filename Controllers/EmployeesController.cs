using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorkProcesses.Data;
using WorkProcesses.Models;

namespace WorkProcesses.Controllers
{
    [Authorize]
    public class EmployeesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public EmployeesController(AppDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Список сотрудников (все или только своего отдела)
        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            var isAdmin = User.IsInRole(RoleNames.Admin);
            var isServiceHead = User.IsInRole(RoleNames.ServiceHead);
            var isDepartmentHead = User.IsInRole(RoleNames.DepartmentHead);

            List<AppUser> employees = new();

            if (isAdmin || isServiceHead)
            {
                employees = await _context.Users
                    .Include(u => u.Department)
                    .OrderBy(u => u.FullName)
                    .ToListAsync();
            }
            else if (isDepartmentHead && currentUser.DepartmentId.HasValue)
            {
                employees = await _context.Users
                    .Include(u => u.Department)
                    .OrderBy(u => u.FullName)
                    .ToListAsync();
            }
            else // обычный сотрудник
            {
                employees = await _context.Users
                    .Include(u => u.Department)
                    .OrderBy(u => u.FullName)
                    .ToListAsync();
            }

            return View(employees);
        }

        // Страница изменения своего статуса (GET)
        public async Task<IActionResult> UpdateStatus()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();
            return View(user);
        }

        // Сохранение нового статуса (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(StatusType status, string? absenceReason, DateTime? absenceStartDate, DateTime? absenceEndDate)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            user.CurrentStatus = status;
            user.AbsenceReason = absenceReason;
            user.AbsenceStartDate = absenceStartDate;
            user.AbsenceEndDate = absenceEndDate;
            user.StatusUpdatedAt = DateTime.Now;

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                TempData["Success"] = "Статус обновлён!";
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            return View(user);
        }
    }
}