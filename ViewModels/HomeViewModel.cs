using WorkProcesses.Models;

namespace WorkProcesses.ViewModels
{
    /// <summary>
    /// Модель для главной страницы
    /// </summary>
    public class HomeViewModel
    {
        /// <summary>
        /// Список сотрудников для левой панели (зависит от роли)
        /// </summary>
        public List<AppUser> Employees { get; set; } = new();

        /// <summary>
        /// ID выбранного сотрудника (для начальника)
        /// </summary>
        public string SelectedUserId { get; set; } = string.Empty;

        /// <summary>
        /// Текущий авторизованный пользователь
        /// </summary>
        public AppUser CurrentUser { get; set; } = null!;

        /// <summary>
        /// Список заданий для выбранного сотрудника (или текущего)
        /// Каждый элемент содержит информацию о задании и его статусе для этого сотрудника
        /// </summary>
        public List<UserTaskInfo> Tasks { get; set; } = new();
    }

    /// <summary>
    /// Информация о задании для конкретного сотрудника (связка TaskItem + TaskAssignment)
    /// </summary>
    public class UserTaskInfo
    {
        public TaskItem Task { get; set; } = null!;
        public TaskAssignment Assignment { get; set; } = null!;
        public double HoursRemaining { get; set; } // часов до дедлайна
        public string RowColorClass { get; set; } = string.Empty; // цвет строки
        public bool CanStartWork { get; set; }     // можно взять в работу
        public bool CanStopWork { get; set; }      // можно остановить работу
        public bool CanSubmitReport { get; set; }  // можно сдать отчёт (хотя бы 10 сек)
    }
}