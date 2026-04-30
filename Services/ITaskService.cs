using WorkProcesses.Models;
using WorkProcesses.ViewModels;

namespace WorkProcesses.Services
{
    /// <summary>
    /// Сервис для работы с заданиями (бизнес-логика)
    /// </summary>
    public interface ITaskService
    {
        /// <summary>
        /// Получить список заданий для пользователя с учётом его роли
        /// </summary>
        Task<List<TaskItem>> GetUserTasksAsync(string userId, bool isAdmin, bool isServiceHead, bool isDepartmentHead, int? departmentId);

        /// <summary>
        /// Создать новое задание с несколькими исполнителями
        /// </summary>
        Task<TaskItem> CreateTaskAsync(TaskViewModel model, string assignedById, List<string> selectedEmployeeIds);

        /// <summary>
        /// Получить задание по ID с подгрузкой связанных данных
        /// </summary>
        Task<TaskItem?> GetTaskByIdAsync(int taskId);

        /// <summary>
        /// Сдать отчёт по заданию (исполнитель)
        /// </summary>
        Task<bool> AddReportAsync(int taskId, string userId, string reportText, DateTime forDate);

        /// <summary>
        /// Принять отчёт (начальник)
        /// </summary>
        Task<bool> ApproveReportAsync(int reportId, string approverId, bool isAdmin, bool isServiceHead, bool isDepartmentHead, int? departmentId);

        /// <summary>
        /// Редактировать отчёт (только автором, если не принят)
        /// </summary>
        Task<bool> EditReportAsync(int reportId, string userId, string newText);

        /// <summary>
        /// Начать работу над заданием (установить IsInWork = true)
        /// </summary>
        Task<bool> StartWorkAsync(int assignmentId, string userId);

        /// <summary>
        /// Остановить работу над заданием (зафиксировать время)
        /// </summary>
        Task<bool> StopWorkAsync(int assignmentId, string userId);

        /// <summary>
        /// Получить накопленные секунды работы по назначению
        /// </summary>
        Task<int> GetWorkDurationAsync(int assignmentId);

        /// <summary>
        /// Получить список сотрудников, доступных для назначения (с учётом роли)
        /// </summary>
        Task<List<EmployeeSelectItem>> GetAvailableEmployeesAsync(string currentUserId, bool isAdmin, bool isServiceHead, bool isDepartmentHead);
        Task<Report?> GetReportByIdAsync(int reportId);
        Task<int> GetTaskIdByReportIdAsync(int reportId);

        Task<List<ReferenceItem>> GetAvailableResourcesAsync(string userId, bool isAdmin, bool isServiceHead, bool isDepartmentHead, int? departmentId);
        Task<List<ReferenceItem>> GetWorkTypesAsync();
        Task<List<ReferenceItem>> GetWorkBasesAsync();
        Task<List<ReferenceItem>> GetPrioritiesAsync();
        Task<List<ReferenceItem>> GetProjectsAsync();

    }
}