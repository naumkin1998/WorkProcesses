
using Microsoft.AspNetCore.Identity;

namespace WorkProcesses.Models
{
    public class AppUser : IdentityUser
    {
        /// <summary>
        /// Полное имя сотрудника
        /// </summary>
        public string FullName { get; set; } = string.Empty;

        /// <summary>
        /// Должность
        /// </summary>
        public string Position { get; set; } = string.Empty;


        /// <summary>
        /// ID отдела (для начальников отделов и сотрудников)
        /// Для начальника службы может быть null
        /// </summary>
        public int? DepartmentId { get; set; }


        // ========== СТАТУСЫ СОТРУДНИКА ==========
        public StatusType CurrentStatus { get; set; } = StatusType.Present;
        public string? AbsenceReason { get; set; }      // Причина отсутствия
        public DateTime? AbsenceStartDate { get; set; } // Дата начала отсутствия
        public DateTime? AbsenceEndDate { get; set; }   // Дата окончания отсутствия
        public DateTime? StatusUpdatedAt { get; set; }  // Когда обновлён статус


        // ========== ВСПОМОГАТЕЛЬНЫЕ СВОЙСТВА (не хранятся в БД) ==========
        public string StatusColor => CurrentStatus switch
        {
            StatusType.Present => "green",
            StatusType.Remote => "green",
            StatusType.Absent => "red",
            StatusType.Vacation => "orange",
            StatusType.Sick => "orange",
            StatusType.BusinessTrip => "orange",
            _ => "gray"
        };

        public string StatusText => CurrentStatus switch
        {
            StatusType.Present => "На работе",
            StatusType.Remote => "Удалённо",
            StatusType.Absent => "Отсутствует",
            StatusType.Vacation => "В отпуске",
            StatusType.Sick => "На больничном",
            StatusType.BusinessTrip => "В командировке",
            _ => "Другая причина"
        };

        public string StatusIcon => CurrentStatus switch
        {
            StatusType.Present => "🟢",
            StatusType.Remote => "🟢💻",
            StatusType.Absent => "🔴",
            StatusType.Vacation => "🟡🏖️",
            StatusType.Sick => "🟡🤒",
            StatusType.BusinessTrip => "🟡✈️",
            _ => "⚪"
        };


        // ========== НАВИГАЦИОННЫЕ СВОЙСТВА ==========

        /// <summary>
        /// Отдел, в котором работает сотрудник (для начальников отделов и сотрудников)
        /// </summary>
        public Department? Department { get; set; }

        /// <summary>
        /// Задания, назначенные этому сотруднику
        /// </summary>
        public List<TaskItem> AssignedTasks { get; set; } = new();

        /// <summary>
        /// Отчёты этого сотрудника
        /// </summary>
        public List<Report> Reports { get; set; } = new();

        /// <summary>
        /// Задания, которые этот сотрудник выдал (как начальник)
        /// </summary>
        public List<TaskItem> IssuedTasks { get; set; } = new();

        /// <summary>
        /// Отчёты, которые этот сотрудник принял (как начальник)
        /// </summary>
        public List<Report> ApprovedReports { get; set; } = new();

        /// <summary>
        /// 
        /// </summary>
        public List<TaskAssignment> TaskAssignments { get; set; } = new();
    }
}

