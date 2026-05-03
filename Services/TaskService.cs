using Microsoft.EntityFrameworkCore;
using WorkProcesses.Data;
using WorkProcesses.Models;
using WorkProcesses.ViewModels;

namespace WorkProcesses.Services
{
    public class TaskService : ITaskService
    {
        private readonly AppDbContext _context;

        public TaskService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<TaskItem>> GetUserTasksAsync(string userId, bool isAdmin, bool isServiceHead, bool isDepartmentHead, int? departmentId)
        {
            var query = _context.Tasks
                .Include(t => t.Assignments)
                    .ThenInclude(a => a.AppUser)
                .Include(t => t.AssignedBy)
                .Include(t => t.Reports)
                .AsNoTracking();

            if (isAdmin || isServiceHead)
            {
                return await query.OrderByDescending(t => t.CreatedAt).ToListAsync();
            }
            else if (isDepartmentHead && departmentId.HasValue)
            {
                return await query
                    .Where(t => t.Assignments.Any(a => a.AppUser.DepartmentId == departmentId) || t.AssignedById == userId)
                    .OrderByDescending(t => t.CreatedAt)
                    .ToListAsync();
            }
            else
            {
                return await query
                    .Where(t => t.Assignments.Any(a => a.AppUserId == userId))
                    .OrderByDescending(t => t.CreatedAt)
                    .ToListAsync();
            }
        }

        public async Task<TaskItem> CreateTaskAsync(TaskViewModel model, string assignedById, List<string> selectedEmployeeIds)
        {
            var task = new TaskItem
            {
                Title = model.Title,
                Description = model.Description,
                Deadline = model.ReportPeriodicity == ReportPeriodicity.Permanent ? DateTime.MaxValue : (model.Deadline ?? DateTime.Now),  // model.Deadline у вас DateTime? (в модели представления nullable)
                StartTime = model.StartTime,
                TaskType = model.TaskType,  // можно позже заменить на ReportPeriodicity
                AssignedById = assignedById,
                AssignedToId = selectedEmployeeIds.FirstOrDefault() ?? string.Empty,
                CreatedAt = DateTime.Now,
                IsCompleted = false,
                // Новые поля
                ResourceId = model.ResourceId,
                WorkTypeId = model.WorkTypeId,
                WorkBasisId = model.WorkBasisId,
                WorkBasisComment = model.WorkBasisComment,
                PriorityId = model.PriorityId,
                ProjectId = model.ProjectId,
                IsImportant = model.IsImportant,
                ReportPeriodicity = model.ReportPeriodicity,
                ReportTime = model.ReportTime,
                ReportWeekDay = model.ReportWeekDay,
                ReportMonthDay = model.ReportMonthDay
            };
            _context.Tasks.Add(task);
            await _context.SaveChangesAsync();

            foreach (var empId in selectedEmployeeIds)
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
            return task;
        }

        public async Task<TaskItem?> GetTaskByIdAsync(int taskId)
        {
            return await _context.Tasks
                .Include(t => t.Assignments)
                    .ThenInclude(a => a.AppUser)
                .Include(t => t.AssignedBy)
                .Include(t => t.Reports)
                    .ThenInclude(r => r.AppUser)
                .FirstOrDefaultAsync(t => t.Id == taskId);
        }

        public async Task<bool> AddReportAsync(int taskId, string userId, string reportText, DateTime forDate)
        {
            var task = await _context.Tasks.FindAsync(taskId);
            if (task == null) return false;
            if (task.IsCompleted) return false;

            // Проверка, что пользователь является исполнителем
            var isAssignee = await _context.TaskAssignments.AnyAsync(a => a.TaskItemId == taskId && a.AppUserId == userId);
            if (!isAssignee) return false;

            // Для неразовых заданий проверяем уникальность даты
            if (task.TaskType != TaskType.Single)
            {
                var exists = await _context.Reports.AnyAsync(r => r.TaskItemId == taskId && r.ForDate.Date == forDate.Date);
                if (exists) return false;
            }

            var report = new Report
            {
                TaskItemId = taskId,
                AppUserId = userId,
                Text = reportText,
                ForDate = forDate,
                ReportedAt = DateTime.Now,
                IsApproved = false
            };
            _context.Reports.Add(report);
            await _context.SaveChangesAsync();
            return true;
        }


        public async Task<bool> ApproveReportAsync(int reportId, string approverId, bool isAdmin, bool isServiceHead, bool isDepartmentHead, int? departmentId)
        {
            var report = await _context.Reports
                .Include(r => r.TaskItem)
                .ThenInclude(t => t.Assignments)
                .ThenInclude(a => a.AppUser)
                .FirstOrDefaultAsync(r => r.Id == reportId);
            if (report == null) return false;

            var task = report.TaskItem;
            if (task == null) return false;

            // Проверка прав
            bool canApprove = isAdmin || isServiceHead ||
                              (isDepartmentHead && task.Assignments.Any(a => a.AppUser.DepartmentId == departmentId)) ||
                              task.AssignedById == approverId;
            if (!canApprove) return false;

            if (task.IsCompleted) return false;

            report.IsApproved = true;
            report.ApprovedAt = DateTime.Now;
            report.ApprovedById = approverId;

            if (task.TaskType == TaskType.Single)
            {
                task.IsCompleted = true;
                // Закрыть все назначения
                var assignments = await _context.TaskAssignments.Where(a => a.TaskItemId == task.Id).ToListAsync();
                foreach (var a in assignments)
                {
                    a.IsCompleted = true;
                    a.IsInWork = false;
                    a.WorkStartTime = null;
                }
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> EditReportAsync(int reportId, string userId, string newText)
        {
            var report = await _context.Reports.FindAsync(reportId);
            if (report == null) return false;
            if (report.AppUserId != userId || report.IsApproved) return false;
            report.Text = newText;
            report.ReportedAt = DateTime.Now;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> StartWorkAsync(int assignmentId, string userId)
        {
            var assignment = await _context.TaskAssignments
                .Include(a => a.TaskItem)
                .FirstOrDefaultAsync(a => a.Id == assignmentId);
            if (assignment == null) return false;
            if (assignment.AppUserId != userId) return false;
            if (assignment.IsCompleted) return false;

            // Останавливаем все другие активные задания пользователя
            var activeAssignments = await _context.TaskAssignments
                .Where(a => a.AppUserId == userId && a.IsInWork && a.Id != assignmentId)
                .ToListAsync();
            foreach (var active in activeAssignments)
            {
                if (active.WorkStartTime.HasValue)
                {
                    active.WorkSeconds += (int)(DateTime.Now - active.WorkStartTime.Value).TotalSeconds;
                    active.WorkStartTime = null;
                }
                active.IsInWork = false;
            }

            assignment.IsInWork = true;
            assignment.WorkStartTime = DateTime.Now;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> StopWorkAsync(int assignmentId, string userId)
        {
            var assignment = await _context.TaskAssignments.FindAsync(assignmentId);
            if (assignment == null) return false;
            if (assignment.AppUserId != userId) return false;

            if (assignment.IsInWork && assignment.WorkStartTime.HasValue)
            {
                assignment.WorkSeconds += (int)(DateTime.Now - assignment.WorkStartTime.Value).TotalSeconds;
                assignment.IsInWork = false;
                assignment.WorkStartTime = null;
                await _context.SaveChangesAsync();
            }
            return true;
        }

        public async Task<int> GetWorkDurationAsync(int assignmentId)
        {
            var assignment = await _context.TaskAssignments.FindAsync(assignmentId);
            if (assignment == null) return 0;
            int seconds = assignment.WorkSeconds;
            if (assignment.IsInWork && assignment.WorkStartTime.HasValue)
                seconds += (int)(DateTime.Now - assignment.WorkStartTime.Value).TotalSeconds;
            return seconds;
        }

        public async Task<List<EmployeeSelectItem>> GetAvailableEmployeesAsync(string currentUserId, bool isAdmin, bool isServiceHead, bool isDepartmentHead)
        {
            IQueryable<AppUser> query = _context.Users.Include(u => u.Department);
            if (!isAdmin && !isServiceHead)
            {
                var currentUser = await _context.Users.FindAsync(currentUserId);
                if (isDepartmentHead && currentUser?.DepartmentId != null)
                    query = query.Where(u => u.DepartmentId == currentUser.DepartmentId);
                else
                    return new List<EmployeeSelectItem>(); // обычный сотрудник не может создавать задания
            }
            var employees = await query.ToListAsync();
            return employees.Select(e => new EmployeeSelectItem
            {
                Id = e.Id,
                FullName = e.FullName,
                DepartmentName = e.Department?.Name ?? "—"
            }).ToList();
        }

        public async Task<Report?> GetReportByIdAsync(int reportId)
        {
            return await _context.Reports.Include(r => r.TaskItem).FirstOrDefaultAsync(r => r.Id == reportId);
        }

        public async Task<int> GetTaskIdByReportIdAsync(int reportId)
        {
            var report = await _context.Reports.FindAsync(reportId);
            return report?.TaskItemId ?? 0;
        }

        public async Task<List<ReferenceItem>> GetAvailableResourcesAsync(string userId, bool isAdmin, bool isServiceHead, bool isDepartmentHead, int? departmentId)
        {
            IQueryable<Resource> query = _context.Resources;
            if (!isAdmin && !isServiceHead && isDepartmentHead && departmentId.HasValue)
            {
                query = query.Where(r => r.ResourceDepartments.Any(rd => rd.DepartmentId == departmentId));
            }
            else if (!isAdmin && !isServiceHead && !isDepartmentHead)
            {
                return new List<ReferenceItem>();
            }
            var resources = await query.Select(r => new ReferenceItem { Id = r.Id, Name = r.Name }).ToListAsync();
            return resources;
        }

        public async Task<List<ReferenceItem>> GetWorkTypesAsync() => await _context.WorkTypes.Select(w => new ReferenceItem { Id = w.Id, Name = w.Name }).ToListAsync();
        public async Task<List<ReferenceItem>> GetWorkBasesAsync() => await _context.WorkBases.Select(w => new ReferenceItem { Id = w.Id, Name = w.Name }).ToListAsync();
        public async Task<List<ReferenceItem>> GetPrioritiesAsync() => await _context.Priorities.Select(p => new ReferenceItem { Id = p.Id, Name = p.Name }).ToListAsync();
        public async Task<List<ReferenceItem>> GetProjectsAsync() => await _context.Projects.Select(p => new ReferenceItem { Id = p.Id, Name = p.Name }).ToListAsync();

        public async Task<bool> DeleteTaskAsync(int taskId, string userId, bool isAdmin, bool isServiceHead, bool isDepartmentHead, int? departmentId)
        {
            var task = await _context.Tasks
                .Include(t => t.Assignments)
                .FirstOrDefaultAsync(t => t.Id == taskId);
            if (task == null) return false;

            // Проверка прав: админ, начальник службы, начальник отдела (если задание в его отделе) или автор
            bool canDelete = isAdmin || isServiceHead ||
                             (isDepartmentHead && task.Assignments.Any(a => a.AppUser.DepartmentId == departmentId)) ||
                             task.AssignedById == userId;
            if (!canDelete) return false;

            _context.Tasks.Remove(task);
            await _context.SaveChangesAsync();
            return true;
        }

    }
}