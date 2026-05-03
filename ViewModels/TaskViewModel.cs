using WorkProcesses.Models;

namespace WorkProcesses.ViewModels
{
    public class TaskViewModel
    {
        // Основные
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        // Даты
        public DateTime? StartTime { get; set; }
        public DateTime? Deadline { get; set; } = DateTime.Now.AddDays(3);

        // Справочники
        public int? ResourceId { get; set; }
        public int? WorkTypeId { get; set; }
        public int? WorkBasisId { get; set; }
        public string? WorkBasisComment { get; set; }
        public int? PriorityId { get; set; }
        public int? ProjectId { get; set; }
        public bool IsImportant { get; set; }

        // Периодичность отчёта
        public ReportPeriodicity ReportPeriodicity { get; set; } = ReportPeriodicity.None;
        public DateTime? ReportTime { get; set; }        // время отчёта (храним DateTime, но используем только время)
        public DayOfWeek? ReportWeekDay { get; set; }
        public int? ReportMonthDay { get; set; }

        // Тип задания (наследуем из старого, но можно использовать ReportPeriodicity)
        public TaskType TaskType { get; set; } = TaskType.Single;

        // Список выбранных сотрудников (ID)
        public List<string> SelectedEmployeeIds { get; set; } = new();

        // Для выпадающих списков
        public List<EmployeeSelectItem> Employees { get; set; } = new();
        public List<ReferenceItem> Resources { get; set; } = new();
        public List<ReferenceItem> WorkTypes { get; set; } = new();
        public List<ReferenceItem> WorkBases { get; set; } = new();
        public List<ReferenceItem> Priorities { get; set; } = new();
        public List<ReferenceItem> Projects { get; set; } = new();
    }

    public class EmployeeSelectItem
    {
        public string Id { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
    }

    public class ReferenceItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}