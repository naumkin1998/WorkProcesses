using WorkProcesses.Models;

namespace WorkProcesses.ViewModels
{
    public class TaskViewModel
    {
        // Для создания/редактирования задания
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime Deadline { get; set; } = DateTime.Now.AddDays(3);
        public TaskType TaskType { get; set; } = TaskType.Single;
        public string AssignedToId { get; set; } = string.Empty;

        // Список сотрудников для выбора (кому назначить)
        //public List<EmployeeSelectItem> Employees { get; set; } = new();

        // Для отображения списка заданий
        public List<TaskItem> Tasks { get; set; } = new();

        // В ViewModels/TaskViewModel.cs добавим:
        public List<string> SelectedEmployeeIds { get; set; } = new(); // для хранения ID выбранных сотрудников
        public List<EmployeeSelectItem> Employees { get; set; } = new(); // весь список сотрудников, доступных для выбора
    }

    public class EmployeeSelectItem
    {
        public string Id { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
    }
}