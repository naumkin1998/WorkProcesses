using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorkProcesses.Data;
using WorkProcesses.Models;
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

            return View("~/Views/Tasks/Index.cshtml", tasks);
            //return View(tasks); // Передаём список заданий в представление Index.cshtml
        }

        // ==================== GET: /Tasks/Create ====================
        /// <summary>
        /// Страница создания нового задания (GET-запрос).
        /// Формирует список сотрудников, которым можно выдать задание, в зависимости от роли.
        /// </summary>
        /// <returns>Представление с формой создания задания</returns>
        public async Task<IActionResult> Create()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            var isAdmin = User.IsInRole(RoleNames.Admin);
            var isServiceHead = User.IsInRole(RoleNames.ServiceHead);
            var isDepartmentHead = User.IsInRole(RoleNames.DepartmentHead);
            var departmentId = currentUser.DepartmentId;

            // Список сотрудников, которым текущий пользователь МОЖЕТ выдавать задания
            List<AppUser> availableEmployees = new();

            if (isAdmin || isServiceHead)
            {
                // Админ и начальник службы могут выдавать задания ЛЮБЫМ сотрудникам
                availableEmployees = await _context.Users
                    .Include(u => u.Department) // Подгружаем отдел для отображения
                    .ToListAsync();
            }
            else if (isDepartmentHead && departmentId.HasValue)
            {
                // Начальник отдела может выдавать задания ТОЛЬКО сотрудникам своего отдела
                availableEmployees = await _context.Users
                    .Include(u => u.Department)
                    .Where(u => u.DepartmentId == departmentId)
                    .ToListAsync();
            }
            else
            {
                // Обычный сотрудник не имеет права выдавать задания
                TempData["Error"] = "У вас нет прав на выдачу заданий";
                return RedirectToAction(nameof(Index));
            }

            // Создаём ViewModel с предзаполненными значениями
            var model = new TaskViewModel
            {
                Deadline = DateTime.Now.AddDays(3),    // Дедлайн по умолчанию — через 3 дня
                TaskType = TaskType.Single,            // По умолчанию разовое задание
                Employees = availableEmployees.Select(e => new EmployeeSelectItem
                {
                    Id = e.Id,
                    FullName = e.FullName,
                    DepartmentName = e.Department?.Name ?? "—" // Если отдела нет — прочерк
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
        [ValidateAntiForgeryToken] // Защита от CSRF-атак
        public async Task<IActionResult> Create(TaskViewModel model)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            // Проверяем валидность модели (атрибуты валидации из TaskViewModel)
            if (ModelState.IsValid)
            {
                // Создаём новую сущность TaskItem на основе ViewModel
                var task = new TaskItem
                {
                    Title = model.Title,
                    Description = model.Description,
                    Deadline = model.Deadline,
                    TaskType = model.TaskType,
                    AssignedToId = model.AssignedToId,   // Кому назначаем (выбрано в форме)
                    AssignedById = currentUser.Id,       // Кто назначает (текущий пользователь)
                    CreatedAt = DateTime.Now,
                    IsCompleted = false                  // Новое задание не выполнено
                };

                _context.Tasks.Add(task);    // Добавляем в контекст
                await _context.SaveChangesAsync(); // Сохраняем в БД

                TempData["Success"] = "Задание успешно создано!";
                return RedirectToAction(nameof(Index));
            }

            // Если модель невалидна — перезагружаем список сотрудников и возвращаем форму с ошибками
            var isAdmin = User.IsInRole(RoleNames.Admin);
            var isServiceHead = User.IsInRole(RoleNames.ServiceHead);
            var isDepartmentHead = User.IsInRole(RoleNames.DepartmentHead);
            var departmentId = currentUser.DepartmentId;

            List<AppUser> availableEmployees = new();

            if (isAdmin || isServiceHead)
                availableEmployees = await _context.Users.Include(u => u.Department).ToListAsync();
            else if (isDepartmentHead && departmentId.HasValue)
                availableEmployees = await _context.Users.Include(u => u.Department).Where(u => u.DepartmentId == departmentId).ToListAsync();

            model.Employees = availableEmployees.Select(e => new EmployeeSelectItem
            {
                Id = e.Id,
                FullName = e.FullName,
                DepartmentName = e.Department?.Name ?? "—"
            }).ToList();

            return View(model); // Возвращаем ту же страницу с ошибками валидации
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

            // Проверка: отчёт может сдать только тот, кому задание назначено
            if (task.AssignedToId != currentUser.Id)
            {
                TempData["Error"] = "Вы не можете сдать отчёт за другого сотрудника";
                return RedirectToAction(nameof(Details), new { id = taskId });
            }

            // Создаём новый отчёт
            var report = new Report
            {
                TaskItemId = taskId,
                AppUserId = currentUser.Id,
                Text = reportText,
                ForDate = forDate,          // Дата, за которую отчитывается сотрудник
                ReportedAt = DateTime.Now,  // Время сдачи отчёта
                IsApproved = false          // По умолчанию не принят
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

            // Загружаем отчёт вместе с заданием
            var report = await _context.Reports
                .Include(r => r.TaskItem)
                .FirstOrDefaultAsync(r => r.Id == reportId);

            if (report == null)
                return NotFound();

            var task = report.TaskItem;

            // Проверка прав на принятие отчёта:
            // - Админ или начальник службы
            // - Начальник отдела, если задание выдано сотруднику его отдела
            // - Постановщик задания (кто выдал)
            var canApprove = User.IsInRole(RoleNames.Admin) ||
                             User.IsInRole(RoleNames.ServiceHead) ||
                             (User.IsInRole(RoleNames.DepartmentHead) && task.AssignedTo?.DepartmentId == currentUser.DepartmentId) ||
                             task.AssignedById == currentUser.Id;

            if (!canApprove)
            {
                TempData["Error"] = "У вас нет прав на принятие этого отчёта";
                return RedirectToAction(nameof(Details), new { id = task.Id });
            }

            // Принимаем отчёт
            report.IsApproved = true;
            report.ApprovedAt = DateTime.Now;
            report.ApprovedById = currentUser.Id;

            // Если задание разовое (Single) — отмечаем как полностью выполненное
            if (task.TaskType == TaskType.Single)
            {
                task.IsCompleted = true;
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Отчёт принят!";
            return RedirectToAction(nameof(Details), new { id = task.Id });
        }
    }
}