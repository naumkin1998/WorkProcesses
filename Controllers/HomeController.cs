using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WorkProcesses.Models;
using System.Diagnostics;

namespace WorkProcesses.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly UserManager<AppUser> _userManager;

        public HomeController(ILogger<HomeController> logger, UserManager<AppUser> userManager)
        {
            _logger = logger;
            _userManager = userManager;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        // ВРЕМЕННЫЙ МЕТОД ДЛЯ НАЗНАЧЕНИЯ РОЛИ ADMIN
        // После использования можно удалить
        public async Task<IActionResult> AssignAdminRole(string email)
        {
            if (string.IsNullOrEmpty(email))
                return Content("Укажите email параметром: /Home/AssignAdminRole?email=ваш@email.ru");

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
                return Content($"Пользователь с email {email} не найден");

            var result = await _userManager.AddToRoleAsync(user, RoleNames.Admin);
            if (result.Succeeded)
                return Content($"Пользователю {email} успешно назначена роль Admin");

            return Content($"Ошибка: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}