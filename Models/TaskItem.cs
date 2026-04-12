using System;

namespace WorkProcesses.Models
{
    public class TaskItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime Deadline { get; set; }
        public TaskType TaskType { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public bool IsCompleted { get; set; } = false;

        // Внешние ключи
        public string AssignedToId { get; set; } = string.Empty;  // Кому
        public string AssignedById { get; set; } = string.Empty;  // Кто выдал

        // Навигационные свойства
        public AppUser? AssignedTo { get; set; }
        public AppUser? AssignedBy { get; set; }
        public List<Report> Reports { get; set; } = new();

        /// <summary>
        /// Просрочено?
        /// </summary>
        public bool IsOverdue => Deadline < DateTime.Now && !IsCompleted;

        /// <summary>
        /// Может ли начальник видеть это задание?
        /// Начальник службы — видит всё
        /// Начальник отдела — видит задания сотрудников СВОЕГО отдела
        /// </summary>
        public bool CanManagerView(string managerId, int? managerDepartmentId, bool isServiceHead)
        {
            if (isServiceHead) return true;  // Начальник службы видит всё
            if (managerDepartmentId == AssignedTo?.DepartmentId) return true;  // Свой отдел
            return AssignedById == managerId;  // Сам выдал
        }
    }
}
