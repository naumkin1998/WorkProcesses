using System;  // Для DateTime

namespace WorkProcesses.Models
{
    /// <summary>
    /// Отчёт сотрудника по заданию
    /// </summary>
    public class Report
    {
        /// <summary>
        /// Уникальный идентификатор отчёта
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Текст отчёта (что сделано, результат)
        /// </summary>
        public string Text { get; set; } = string.Empty;

        /// <summary>
        /// Дата и время сдачи отчёта
        /// Автоматически устанавливается текущая дата
        /// </summary>
        public DateTime ReportedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// Принят ли отчёт начальником?
        /// false — ещё не проверен/отклонён, true — принят
        /// </summary>
        public bool IsApproved { get; set; } = false;

        /// <summary>
        /// Дата и время, когда начальник принял отчёт
        /// DateTime? — знак вопроса значит, что может быть NULL (ещё не принят)
        /// </summary>
        public DateTime? ApprovedAt { get; set; }

        /// <summary>
        /// Для ежедневных/еженедельных заданий — за какую дату этот отчёт
        /// Например: отчёт за 31.03.2026
        /// </summary>
        public DateTime ForDate { get; set; }

        // ========== ВНЕШНИЕ КЛЮЧИ ==========

        /// <summary>
        /// ID задания, по которому сдан отчёт
        /// </summary>
        public int TaskItemId { get; set; }

        /// <summary>
        /// ID сотрудника, который сдал отчёт
        /// </summary>
        public string AppUserId { get; set; } = string.Empty;

        /// <summary>
        /// ID начальника, который принял отчёт
        /// string? — может быть NULL (ещё не принят)
        /// </summary>
        public string? ApprovedById { get; set; }

        // ========== НАВИГАЦИОННЫЕ СВОЙСТВА ==========

        /// <summary>
        /// Ссылка на объект задания
        /// </summary>
        public TaskItem? TaskItem { get; set; }

        /// <summary>
        /// Ссылка на объект сотрудника-исполнителя
        /// </summary>
        public AppUser? AppUser { get; set; }

        /// <summary>
        /// Ссылка на объект начальника, принявшего отчёт
        /// </summary>
        public AppUser? ApprovedBy { get; set; }
    }
}
