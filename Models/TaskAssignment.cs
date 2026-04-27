using System;

namespace WorkProcesses.Models
{
    public class TaskAssignment
    {
        public int Id { get; set; }

        public int TaskItemId { get; set; }
        public TaskItem TaskItem { get; set; }

        public string AppUserId { get; set; }
        public AppUser AppUser { get; set; }

        // Статус выполнения для этого исполнителя
        public bool IsCompleted { get; set; } = false;

        // Взято ли задание в работу прямо сейчас
        public bool IsInWork { get; set; } = false;

        // Общее время работы (секунды)
        public int WorkSeconds { get; set; } = 0;

        // Время начала текущей рабочей сессии
        public DateTime? WorkStartTime { get; set; }
    }
}