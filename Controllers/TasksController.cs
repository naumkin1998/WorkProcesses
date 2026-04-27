using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorkProcesses.Data;
using WorkProcesses.Models;
using WorkProcesses.Services;
using WorkProcesses.ViewModels;

namespace WorkProcesses.Controllers
{
    /// <summary>
    /// Контроллер для управления рабочими заданиями (TaskItem).
    /// Требует авторизации — все действия доступны только авторизованным пользователям.
    /// </summary>
    [Authorize]
    public class TasksController : Controller
    {
        // Контекст базы данных для работы с сущностями (задания, отчёты, пользователи)
        private readonly AppDbContext _context;

        // Менеджер пользователей ASP.NET Core Identity для получения данных текущего пользователя
        private readonly UserManager<AppUser> _userManager;

        /// <summary>
        /// Конструктор контроллера. Получает зависимости через Dependency Injection.
        /// </summary>
        /// <param name="context">Контекст базы данных</param>
        /// <param name="userManager">Менеджер пользователей Identity</param>
        public TasksController(AppDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ==================== GET: /Tasks ====================
        /// <summary>
        /// Главная страница со списком заданий.
        /// Права доступа:
        /// - Админ и начальник службы: видят ВСЕ задания
        /// - Начальник отдела: видят задания своего отдела + задания, которые они выдали
        /// - Обычный сотрудник: видят только свои задания
        /// </summary>
        /// <returns>Представление со списком заданий</returns>
        public async Task<IActionResult> Index()
        {
            // Получаем текущего авторизованного пользователя
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge(); // Если пользователь не найден — перенаправляем на логин

            // Определяем роли пользователя через проверки Claim-ов
            var isAdmin = User.IsInRole(RoleNames.Admin);
            var isServiceHead = User.IsInRole(RoleNames.ServiceHead);
            var isDepartmentHead = User.IsInRole(RoleNames.DepartmentHead);
            var departmentId = currentUser.DepartmentId; // Отдел пользователя

            List<TaskItem> tasks = new(); // Коллекция для хранения заданий

            // --- Ветвление по ролям для фильтрации заданий ---
            if (isAdmin || isServiceHead)
            {
                // Администратор и начальник службы видят абсолютно все задания
                // Include() подгружает связанные сущности (AssignedTo, AssignedBy, Reports)
                tasks = await _context.Tasks
                    .Include(t => t.AssignedTo)      // Кому назначено (пользователь)
                    .Include(t => t.AssignedBy)      // Кто назначил (пользователь)
                    .Include(t => t.Reports)         // Отчёты по заданию
                    .OrderByDescending(t => t.CreatedAt) // Сначала новые
                    .ToListAsync();
            }
            else if (isDepartmentHead && departmentId.HasValue)
            {
                // Начальник отдела видит:
                // 1) Задания сотрудников своего отдела (AssignedTo.DepartmentId == departmentId)
                // 2) Задания, которые он сам выдал (AssignedById == currentUser.Id)
                tasks = await _context.Tasks
                    .Include(t => t.AssignedTo)
                    .Include(t => t.AssignedBy)
                    .Include(t => t.Reports)
                    .Where(t => t.AssignedTo!.DepartmentId == departmentId || t.AssignedById == currentUser.Id)
                    .OrderByDescending(t => t.CreatedAt)
                    .ToListAsync();
            }
            else
            {
                // Обычный сотрудник видит только задания, назначенные лично ему
                tasks = await _context.Tasks
                    .Include(t => t.AssignedTo)
                    .Include(t => t.AssignedBy)
                    .Include(t => t.Reports)
                    .Where(t => t.AssignedToId == currentUser.Id)
                    .OrderByDescending(t => t.CreatedAt)
                    .ToListAsync();
            }

            return View(tasks); // Передаём список заданий в представление Index.cshtml
        }

        public IActionResult Test()
        {
            return View("Test");
        }

        // ==================== GET: /Tasks/Create ====================
        /// <summary>
        /// Страница создания нового задания (GET-запрос).
        /// Формирует список сотрудников, которым можно выдать задание, в зависимости от роли.
        /// </summary>
        /// <returns>Представление с формой создания задания</returns>
        // GET: Tasks/Create
        public async Task<IActionResult> Create()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            var isAdmin = User.IsInRole(RoleNames.Admin);
            var isServiceHead = User.IsInRole(RoleNames.ServiceHead);
            var isDepartmentHead = User.IsInRole(RoleNames.DepartmentHead);

            IQueryable<AppUser> employeesQuery = _context.Users.Include(u => u.Department);
            if (!isAdmin && !isServiceHead)
            {
                if (isDepartmentHead && currentUser.DepartmentId.HasValue)
                    employeesQuery = employeesQuery.Where(u => u.DepartmentId == currentUser.DepartmentId);
                else
                    // обычный сотрудник не может создавать задания
                    return RedirectToAction(nameof(Index));
            }

            var employees = await employeesQuery.ToListAsync();

            var model = new TaskViewModel
            {
                Deadline = DateTime.Now.AddDays(3),
                TaskType = TaskType.Single,
                Employees = employees.Select(e => new EmployeeSelectItem
                {
                    Id = e.Id,
                    FullName = e.FullName,
                    DepartmentName = e.Department?.Name ?? "—"
                }).ToList()
            };
            return View(model);
        }

        // ==================== POST: /Tasks/Create ====================
        /// <summary>
        /// Обработка отправки формы создания задания.
        /// Сохраняет новое задание в базу данных.
        /// </summary>
        /// <param name="model">Данные из формы (TaskViewModel)</param>
        /// <returns>Перенаправление на Index при успехе, иначе возврат формы с ошибками</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TaskViewModel model)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            if (ModelState.IsValid)
            {
                // Создаём само задание (пока заполняем старое поле AssignedToId первым выбранным, для совместимости)
                var firstAssigneeId = model.SelectedEmployeeIds.FirstOrDefault();
                var task = new TaskItem
                {
                    Title = model.Title,
                    Description = model.Description,
                    Deadline = model.Deadline,
                    TaskType = model.TaskType,
                    AssignedToId = firstAssigneeId ?? string.Empty, // временно для совместимости
                    AssignedById = currentUser.Id,
                    CreatedAt = DateTime.Now,
                    IsCompleted = false
                };

                _context.Tasks.Add(task);
                await _context.SaveChangesAsync(); // сохраняем, чтобы получить Id задания

                // Теперь создаём записи в TaskAssignment для каждого выбранного сотрудника
                foreach (var empId in model.SelectedEmployeeIds)
                {
                    var assignment = new TaskAssignment
                    {
                        TaskItemId = task.Id,
                        AppUserId = empId,
                        IsCompleted = false,
                        IsInWork = false,
                        WorkSeconds = 0,
                        WorkStartTime = null
                    };
                    _context.TaskAssignments.Add(assignment);
                }
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Задание создано и назначено {model.SelectedEmployeeIds.Count} сотрудникам";
                return RedirectToAction(nameof(Index));
            }

            // Если ошибка — перезагружаем список сотрудников и возвращаем представление
            // ... (код перезагрузки Employees такой же, как в GET)
            return View(model);
        }

        // ==================== GET: /Tasks/Details/{id} ====================
        /// <summary>
        /// Страница деталей задания и сдачи отчётов.
        /// Показывает информацию о задании, список отчётов и форму для сдачи нового отчёта.
        /// </summary>
        /// <param name="id">ID задания</param>
        /// <returns>Представление с деталями задания</returns>
        public async Task<IActionResult> Details(int id)
        {
            // Загружаем задание со всеми связанными данными
            var task = await _context.Tasks
                .Include(t => t.AssignedTo)
                .Include(t => t.AssignedBy)
                .Include(t => t.Reports)
                .ThenInclude(r => r.AppUser) // Подгружаем пользователя, сдавшего отчёт
                .FirstOrDefaultAsync(t => t.Id == id);

            if (task == null)
                return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            // Сложная проверка прав доступа к просмотру задания
            var canView = User.IsInRole(RoleNames.Admin) ||                                    // Админ всегда видит
                          User.IsInRole(RoleNames.ServiceHead) ||                             // Начальник службы всегда видит
                          task.AssignedToId == currentUser.Id ||                              // Исполнитель видит своё задание
                          task.AssignedById == currentUser.Id ||                              // Постановщик видит своё задание
                          (User.IsInRole(RoleNames.DepartmentHead) &&                         // Начальник отдела видит задания
                           task.AssignedTo?.DepartmentId == currentUser.DepartmentId);       // ...сотрудников своего отдела

            if (!canView)
            {
                TempData["Error"] = "У вас нет доступа к этому заданию";
                return RedirectToAction(nameof(Index));
            }

            return View(task);
        }

        // ==================== POST: /Tasks/AddReport ====================
        /// <summary>
        /// Сдача отчёта по заданию (выполняется исполнителем).
        /// </summary>
        /// <param name="taskId">ID задания</param>
        /// <param name="reportText">Текст отчёта</param>
        /// <param name="forDate">Дата, за которую сдаётся отчёт (для ежедневных заданий)</param>
        /// <returns>Перенаправление обратно на страницу деталей</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddReport(int taskId, string reportText, DateTime forDate)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            var task = await _context.Tasks.FindAsync(taskId);
            if (task == null)
                return NotFound();

            // Проверка: задание уже выполнено?
            if (task.IsCompleted)
            {
                TempData["Error"] = "Задание уже выполнено, нельзя сдать новый отчёт";
                return RedirectToAction(nameof(Details), new { id = taskId });
            }

            // Проверка: отчёт может сдать только тот, кому назначено
            if (task.AssignedToId != currentUser.Id)
            {
                TempData["Error"] = "Вы не можете сдать отчёт за другого сотрудника";
                return RedirectToAction(nameof(Details), new { id = taskId });
            }

            // Для ежедневных/еженедельных заданий можно добавить проверку на уникальность даты (опционально)
            // Например, чтобы нельзя было сдать два отчёта за один день.
            var existingReport = await _context.Reports
                .AnyAsync(r => r.TaskItemId == taskId && r.ForDate.Date == forDate.Date);
            if (existingReport && task.TaskType != TaskType.Single)
            {
                TempData["Error"] = "Отчёт за эту дату уже сдан";
                return RedirectToAction(nameof(Details), new { id = taskId });
            }

            var report = new Report
            {
                TaskItemId = taskId,
                AppUserId = currentUser.Id,
                Text = reportText,
                ForDate = forDate,
                ReportedAt = DateTime.Now,
                IsApproved = false
            };

            _context.Reports.Add(report);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Отчёт успешно сдан! Ожидает проверки начальником.";
            return RedirectToAction(nameof(Details), new { id = taskId });
        }

        // ==================== POST: /Tasks/ApproveReport ====================
        /// <summary>
        /// Принятие отчёта начальником (или админом/начальником службы).
        /// Для разового задания — автоматически помечает задание как выполненное.
        /// </summary>
        /// <param name="reportId">ID отчёта, который нужно принять</param>
        /// <returns>Перенаправление на страницу деталей задания</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveReport(int reportId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            var report = await _context.Reports
                .Include(r => r.TaskItem)
                .FirstOrDefaultAsync(r => r.Id == reportId);

            if (report == null)
                return NotFound();

            var task = report.TaskItem;

            // Проверка прав: принять может начальник отдела, начальник службы или админ
            var canApprove = User.IsInRole(RoleNames.Admin) ||
                             User.IsInRole(RoleNames.ServiceHead) ||
                             (User.IsInRole(RoleNames.DepartmentHead) && task.AssignedTo?.DepartmentId == currentUser.DepartmentId) ||
                             task.AssignedById == currentUser.Id;

            if (!canApprove)
            {
                TempData["Error"] = "У вас нет прав на принятие этого отчёта";
                return RedirectToAction(nameof(Details), new { id = task.Id });
            }

            // Если задание уже выполнено — не даём принять ещё один отчёт
            if (task.IsCompleted)
            {
                TempData["Error"] = "Задание уже выполнено, нельзя принять дополнительный отчёт";
                return RedirectToAction(nameof(Details), new { id = task.Id });
            }

            report.IsApproved = true;
            report.ApprovedAt = DateTime.Now;
            report.ApprovedById = currentUser.Id;

            // Если задание разовое — помечаем как выполненное
            if (task.TaskType == TaskType.Single)
            {
                task.IsCompleted = true;
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Отчёт принят!";
            return RedirectToAction(nameof(Details), new { id = task.Id });
        }

        // Экспорт всех заданий в Excel
        public async Task<IActionResult> ExportToExcel()
        {
            var excelService = HttpContext.RequestServices.GetRequiredService<ExcelExportService>();
            var fileBytes = await excelService.ExportAllTasksToExcel();
            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Задания_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
        }

        // Экспорт отчётов по конкретному заданию
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



        // ==================== POST: /Tasks/EditReport  ====================
        /// <summary>
        /// Редактирование отчёта (только для автора, пока не принят)
        /// </summary>
        /// <param name="id">ID отчёта, который нужно отредактировать</param>
        public async Task<IActionResult> EditReport(int id)
        {
            var report = await _context.Reports
                .Include(r => r.TaskItem)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (report == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            // Проверка: только автор отчёта и отчёт ещё не принят
            if (report.AppUserId != currentUser.Id || report.IsApproved)
            {
                TempData["Error"] = "Редактирование недоступно";
                return RedirectToAction(nameof(Details), new { id = report.TaskItemId });
            }

            return View(report);
        }

        // POST: Сохранение отредактированного отчёта
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditReport(int id, string text)
        {
            var report = await _context.Reports.FindAsync(id);
            if (report == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            if (report.AppUserId != currentUser.Id || report.IsApproved)
            {
                TempData["Error"] = "Нельзя редактировать этот отчёт";
                return RedirectToAction(nameof(Details), new { id = report.TaskItemId });
            }

            report.Text = text;
            report.ReportedAt = DateTime.Now; // обновляем дату последнего изменения
            await _context.SaveChangesAsync();

            TempData["Success"] = "Отчёт обновлён";
            return RedirectToAction(nameof(Details), new { id = report.TaskItemId });
        }
    }
}