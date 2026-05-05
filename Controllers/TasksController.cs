using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorkProcesses.Models;
using WorkProcesses.Services;
using WorkProcesses.ViewModels;
using WorkProcesses.Data;

namespace WorkProcesses.Controllers
{
    [Authorize]
    public class TasksController : Controller
    {
        private readonly ITaskService _taskService;
        private readonly UserManager<AppUser> _userManager;

        public TasksController(ITaskService taskService, UserManager<AppUser> userManager)
        {
            _taskService = taskService;
            _userManager = userManager;
        }

        // GET: /Tasks
        public async Task<IActionResult> Index(string? filterDate, string? filterType)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            var isAdmin = User.IsInRole(RoleNames.Admin);
            var isServiceHead = User.IsInRole(RoleNames.ServiceHead);
            var isDepartmentHead = User.IsInRole(RoleNames.DepartmentHead);

            var tasks = await _taskService.GetUserTasksAsync(
                currentUser.Id,
                isAdmin,
                isServiceHead,
                isDepartmentHead,
                currentUser.DepartmentId);

            // Логика фильтрации по дате
            DateTime start = DateTime.Today;
            DateTime end = DateTime.Today.AddDays(1).AddSeconds(-1);
            bool useDateFilter = false;

            if (!string.IsNullOrEmpty(filterType))
            {
                switch (filterType.ToLower())
                {
                    case "today":
                        start = DateTime.Today;
                        end = start.AddDays(1).AddSeconds(-1);
                        useDateFilter = true;
                        break;
                    case "week":
                        start = DateTime.Today;
                        end = start.AddDays(7);
                        useDateFilter = true;
                        break;
                    case "month":
                        start = DateTime.Today;
                        end = start.AddMonths(1);
                        useDateFilter = true;
                        break;
                }
            }
            else if (!string.IsNullOrEmpty(filterDate) && DateTime.TryParse(filterDate, out var customDate))
            {
                start = customDate.Date;
                end = start.AddDays(1).AddSeconds(-1);
                useDateFilter = true;
            }

            if (useDateFilter)
            {
                tasks = tasks.Where(t => t.Deadline >= start && t.Deadline <= end).ToList();
                ViewBag.FilterDate = start;
            }
            else
            {
                ViewBag.FilterDate = null;
            }

            return View(tasks);
        }

        // GET: /Tasks/Create
        public async Task<IActionResult> Create()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            var isAdmin = User.IsInRole(RoleNames.Admin);
            var isServiceHead = User.IsInRole(RoleNames.ServiceHead);
            var isDepartmentHead = User.IsInRole(RoleNames.DepartmentHead);

            if (!isAdmin && !isServiceHead && !isDepartmentHead)
                return RedirectToAction(nameof(Index));

            // Доступные сотрудники
            var employees = await _taskService.GetAvailableEmployeesAsync(currentUser.Id, isAdmin, isServiceHead, isDepartmentHead);

            // Справочники
            var resources = await _taskService.GetAvailableResourcesAsync(currentUser.Id, isAdmin, isServiceHead, isDepartmentHead, currentUser.DepartmentId);
            var workTypes = await _taskService.GetWorkTypesAsync();
            var workBases = await _taskService.GetWorkBasesAsync();
            var priorities = await _taskService.GetPrioritiesAsync();
            var projects = await _taskService.GetProjectsAsync();

            var model = new TaskViewModel
            {
                Deadline = DateTime.Now.AddDays(3),
                StartTime = DateTime.Now,
                TaskType = TaskType.Single,
                Employees = employees,
                Resources = resources,
                WorkTypes = workTypes,
                WorkBases = workBases,
                Priorities = priorities,
                Projects = projects
            };
            return View(model);
        }

        // POST: /Tasks/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TaskViewModel model)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            if (ModelState.IsValid && model.SelectedEmployeeIds != null && model.SelectedEmployeeIds.Any())
            {
                await _taskService.CreateTaskAsync(model, currentUser.Id, model.SelectedEmployeeIds);
                TempData["Success"] = $"Задание создано и назначено {model.SelectedEmployeeIds.Count} сотрудникам";
                return RedirectToAction(nameof(Index));
            }

            // В случае ошибки перезагружаем справочники и возвращаем форму
            var isAdmin = User.IsInRole(RoleNames.Admin);
            var isServiceHead = User.IsInRole(RoleNames.ServiceHead);
            var isDepartmentHead = User.IsInRole(RoleNames.DepartmentHead);
            model.Employees = await _taskService.GetAvailableEmployeesAsync(currentUser.Id, isAdmin, isServiceHead, isDepartmentHead);
            model.Resources = await _taskService.GetAvailableResourcesAsync(currentUser.Id, isAdmin, isServiceHead, isDepartmentHead, currentUser.DepartmentId);
            model.WorkTypes = await _taskService.GetWorkTypesAsync();
            model.WorkBases = await _taskService.GetWorkBasesAsync();
            model.Priorities = await _taskService.GetPrioritiesAsync();
            model.Projects = await _taskService.GetProjectsAsync();
            return View(model);
        }

        // GET: /Tasks/Details/{id}
        public async Task<IActionResult> Details(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            var task = await _taskService.GetTaskByIdAsync(id);
            if (task == null) return NotFound();

            // Проверка доступа
            bool canView = User.IsInRole(RoleNames.Admin) ||
                           User.IsInRole(RoleNames.ServiceHead) ||
                           task.AssignedById == currentUser.Id ||
                           task.Assignments.Any(a => a.AppUserId == currentUser.Id) ||
                           (User.IsInRole(RoleNames.DepartmentHead) && task.Assignments.Any(a => a.AppUser.DepartmentId == currentUser.DepartmentId));

            if (!canView)
            {
                TempData["Error"] = "У вас нет доступа к этому заданию";
                return RedirectToAction(nameof(Index));
            }

            return View(task);
        }

        // POST: /Tasks/AddReport
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddReport(int taskId, string reportText, DateTime forDate)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            var result = await _taskService.AddReportAsync(taskId, currentUser.Id, reportText, forDate);
            if (!result)
                TempData["Error"] = "Не удалось сдать отчёт. Возможно, задание уже выполнено или вы не исполнитель.";
            else
                TempData["Success"] = "Отчёт успешно сдан! Ожидает проверки.";
           
            return RedirectToAction(nameof(Details), new { id = taskId });
        }

        // POST: /Tasks/ApproveReport
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveReport(int reportId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            var isAdmin = User.IsInRole(RoleNames.Admin);
            var isServiceHead = User.IsInRole(RoleNames.ServiceHead);
            var isDepartmentHead = User.IsInRole(RoleNames.DepartmentHead);

            var result = await _taskService.ApproveReportAsync(
                reportId,
                currentUser.Id,
                isAdmin,
                isServiceHead,
                isDepartmentHead,
                currentUser.DepartmentId);

            if (!result)
                TempData["Error"] = "Не удалось принять отчёт. Проверьте права или статус задания.";
            else
                TempData["Success"] = "Отчёт принят!";

            // Найдём taskId для перенаправления
            var report = await _taskService.GetReportByIdAsync(reportId); // добавим этот метод в сервис
            if (report == null) return RedirectToAction(nameof(Index));
            return RedirectToAction(nameof(Details), new { id = report.TaskItemId });
        }

        // GET: /Tasks/EditReport/{id}
        public async Task<IActionResult> EditReport(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            var report = await _taskService.GetReportByIdAsync(id);
            if (report == null || report.AppUserId != currentUser.Id || report.IsApproved)
            {
                TempData["Error"] = "Редактирование недоступно";
                return RedirectToAction(nameof(Index));
            }
            return View(report);
        }

        // POST: /Tasks/EditReport
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditReport(int id, string text)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            var result = await _taskService.EditReportAsync(id, currentUser.Id, text);
            if (!result)
                TempData["Error"] = "Не удалось отредактировать отчёт";
            else
                TempData["Success"] = "Отчёт обновлён";

            return RedirectToAction(nameof(Details), new { id = await _taskService.GetTaskIdByReportIdAsync(id) });
        }

        // POST: /Tasks/StartWork
        [HttpPost]
        public async Task<IActionResult> StartWork(int assignmentId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            var result = await _taskService.StartWorkAsync(assignmentId, currentUser.Id);
            if (!result) return BadRequest("Не удалось начать работу");
            return Ok();
        }

        // POST: /Tasks/StopWork
        [HttpPost]
        public async Task<IActionResult> StopWork(int assignmentId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            var result = await _taskService.StopWorkAsync(assignmentId, currentUser.Id);
            if (!result) return BadRequest("Не удалось остановить работу");
            return Ok();
        }

        // GET: /Tasks/GetWorkDuration
        [HttpGet]
        public async Task<IActionResult> GetWorkDuration(int assignmentId)
        {
            var seconds = await _taskService.GetWorkDurationAsync(assignmentId);
            return Json(new { seconds });
        }

        // Экспорт в Excel (оставляем без изменений, но можно вынести в сервис)
        public async Task<IActionResult> ExportToExcel()
        {
            var excelService = HttpContext.RequestServices.GetRequiredService<ExcelExportService>();
            var fileBytes = await excelService.ExportAllTasksToExcel();
            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Задания_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
        }

        public async Task<IActionResult> ExportReports(int id)
        {
            var excelService = HttpContext.RequestServices.GetRequiredService<ExcelExportService>();
            var fileBytes = await excelService.ExportReportsByTask(id);
            if (fileBytes.Length == 0)
            {
                TempData["Error"] = "Не найдено отчётов для экспорта";
                return RedirectToAction(nameof(Details), new { id });
            }
            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Отчёты_задания_{id}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            var isAdmin = User.IsInRole(RoleNames.Admin);
            var isServiceHead = User.IsInRole(RoleNames.ServiceHead);
            var isDepartmentHead = User.IsInRole(RoleNames.DepartmentHead);

            var result = await _taskService.DeleteTaskAsync(id, currentUser.Id, isAdmin, isServiceHead, isDepartmentHead, currentUser.DepartmentId);
            if (!result)
            {
                TempData["Error"] = "Не удалось удалить задание. Проверьте права.";
                return RedirectToAction(nameof(Index));
            }
            TempData["Success"] = "Задание удалено.";
            return RedirectToAction(nameof(Index));
        }

    }
}