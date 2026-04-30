using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorkProcesses.Data;
using WorkProcesses.Models;
using WorkProcesses.ViewModels;

namespace WorkProcesses.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public HomeController(AppDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(string? selectedUserId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            var isAdmin = User.IsInRole(RoleNames.Admin);
            var isServiceHead = User.IsInRole(RoleNames.ServiceHead);
            var isDepartmentHead = User.IsInRole(RoleNames.DepartmentHead);

            // Левая панель: список сотрудников, доступных для просмотра
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
                    .Where(u => u.DepartmentId == currentUser.DepartmentId)
                    .OrderBy(u => u.FullName)
                    .ToListAsync();
            }
            else
            {
                employees = new List<AppUser> { currentUser };
            }

            // Определяем, чьи задания показывать
            string targetUserId = selectedUserId ?? currentUser.Id;
            if (targetUserId != currentUser.Id)
            {
                bool canViewOther = isAdmin || isServiceHead ||
                                    (isDepartmentHead && employees.Any(e => e.Id == targetUserId));
                if (!canViewOther)
                {
                    TempData["Error"] = "Нет прав на просмотр заданий этого сотрудника";
                    targetUserId = currentUser.Id;
                }
            }

            // Загружаем назначения (TaskAssignment) для целевого сотрудника
            var assignments = await _context.TaskAssignments
                .Include(a => a.TaskItem)
                    .ThenInclude(t => t.AssignedBy)
                .Include(a => a.TaskItem)
                    .ThenInclude(t => t.Priority)
                .Include(a => a.TaskItem)
                    .ThenInclude(t => t.Project)
                .Where(a => a.AppUserId == targetUserId && !a.IsCompleted)
                .OrderBy(a => a.TaskItem.Deadline)
                .ToListAsync();

            // Преобразуем в удобную модель для представления
            var tasksInfo = assignments.Select(a => new UserTaskInfo
            {
                Task = a.TaskItem,
                Assignment = a,
                HoursRemaining = (a.TaskItem.Deadline - DateTime.Now).TotalHours,
                RowColorClass = GetRowColorClass(a.TaskItem),
                CanStartWork = !a.IsCompleted && !a.IsInWork && a.AppUserId == currentUser.Id,
                CanStopWork = !a.IsCompleted && a.IsInWork && a.AppUserId == currentUser.Id,
                CanSubmitReport = !a.IsCompleted && a.AppUserId == currentUser.Id && (a.WorkSeconds + (a.IsInWork && a.WorkStartTime.HasValue ? (int)(DateTime.Now - a.WorkStartTime.Value).TotalSeconds : 0)) >= 10,
                WorkSeconds = a.WorkSeconds,
                IsInWork = a.IsInWork,
                WorkStartTime = a.WorkStartTime
            }).ToList();

            var model = new HomeViewModel
            {
                Employees = employees,
                SelectedUserId = targetUserId,
                CurrentUser = currentUser,
                Tasks = tasksInfo
            };

            return View(model);
        }

        private string GetRowColorClass(TaskItem task)
        {
            if (task.Deadline < DateTime.Now) return "table-danger";
            var hoursLeft = (task.Deadline - DateTime.Now).TotalHours;
            if (hoursLeft < 1) return "table-warning";
            return "";
        }

        [HttpPost]
        public async Task<IActionResult> StartWork(int assignmentId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var assignment = await _context.TaskAssignments
                .Include(a => a.TaskItem)
                .FirstOrDefaultAsync(a => a.Id == assignmentId);
            if (assignment == null || assignment.AppUserId != user.Id)
                return BadRequest();

            // Проверяем, нет ли уже активного задания у пользователя
            var active = await _context.TaskAssignments
                .FirstOrDefaultAsync(a => a.AppUserId == user.Id && a.IsInWork && a.Id != assignmentId);
            if (active != null)
            {
                // Останавливаем предыдущее
                if (active.WorkStartTime.HasValue)
                    active.WorkSeconds += (int)(DateTime.Now - active.WorkStartTime.Value).TotalSeconds;
                active.IsInWork = false;
                active.WorkStartTime = null;
            }

            assignment.IsInWork = true;
            assignment.WorkStartTime = DateTime.Now;
            await _context.SaveChangesAsync();

            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> StopWork(int assignmentId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var assignment = await _context.TaskAssignments.FindAsync(assignmentId);
            if (assignment == null || assignment.AppUserId != user.Id)
                return BadRequest();

            if (assignment.IsInWork && assignment.WorkStartTime.HasValue)
            {
                assignment.WorkSeconds += (int)(DateTime.Now - assignment.WorkStartTime.Value).TotalSeconds;
                assignment.IsInWork = false;
                assignment.WorkStartTime = null;
                await _context.SaveChangesAsync();
            }
            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> SubmitReport(int assignmentId, string reportText, DateTime forDate)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var assignment = await _context.TaskAssignments
                .Include(a => a.TaskItem)
                .FirstOrDefaultAsync(a => a.Id == assignmentId);
            if (assignment == null || assignment.AppUserId != user.Id)
                return BadRequest("Назначение не найдено");

            if (assignment.IsCompleted)
                return BadRequest("Задание уже выполнено");

            int totalSeconds = assignment.WorkSeconds;
            if (assignment.IsInWork && assignment.WorkStartTime.HasValue)
                totalSeconds += (int)(DateTime.Now - assignment.WorkStartTime.Value).TotalSeconds;

            if (totalSeconds < 10)
                return BadRequest("Для сдачи отчёта необходимо проработать над заданием не менее 10 секунд");

            var report = new Report
            {
                TaskItemId = assignment.TaskItemId,
                AppUserId = user.Id,
                Text = reportText,
                ForDate = forDate,
                IsApproved = false,
                ReportedAt = DateTime.Now
            };
            _context.Reports.Add(report);
            await _context.SaveChangesAsync();

            // Останавливаем работу после сдачи отчёта
            if (assignment.IsInWork && assignment.WorkStartTime.HasValue)
            {
                assignment.WorkSeconds += (int)(DateTime.Now - assignment.WorkStartTime.Value).TotalSeconds;
                assignment.IsInWork = false;
                assignment.WorkStartTime = null;
                await _context.SaveChangesAsync();
            }

            return Ok();
        }

        [HttpGet]
        public async Task<IActionResult> GetWorkDuration(int assignmentId)
        {
            var assignment = await _context.TaskAssignments.FindAsync(assignmentId);
            if (assignment == null) return Json(new { seconds = 0 });
            int seconds = assignment.WorkSeconds;
            if (assignment.IsInWork && assignment.WorkStartTime.HasValue)
                seconds += (int)(DateTime.Now - assignment.WorkStartTime.Value).TotalSeconds;
            return Json(new { seconds });
        }
    }

    // Вспомогательные модели (можно вынести в ViewModels)
    public class UserTaskInfo
    {
        public TaskItem Task { get; set; }
        public TaskAssignment Assignment { get; set; }
        public double HoursRemaining { get; set; }
        public string RowColorClass { get; set; }
        public bool CanStartWork { get; set; }
        public bool CanStopWork { get; set; }
        public bool CanSubmitReport { get; set; }
        public int WorkSeconds { get; set; }
        public bool IsInWork { get; set; }
        public DateTime? WorkStartTime { get; set; }
    }

    public class HomeViewModel
    {
        public List<AppUser> Employees { get; set; } = new();
        public string SelectedUserId { get; set; } = string.Empty;
        public AppUser CurrentUser { get; set; } = null!;
        public List<UserTaskInfo> Tasks { get; set; } = new();
    }
}