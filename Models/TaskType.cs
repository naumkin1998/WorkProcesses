namespace WorkProcesses.Models
{
        public enum TaskType
        {
            Single,   // 0 — Разовое задание (сдал отчёт один раз и закрыл)
            Daily,    // 1 — Ежедневное (каждый день нужно сдавать отчёт)
            Weekly    // 2 — Еженедельное (раз в неделю отчёт)
        }
}
