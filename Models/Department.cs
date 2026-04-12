namespace WorkProcesses.Models
{
    public class Department
    {
        public int Id { get; set; }

        /// <summary>
        /// Название отдела
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// ID подразделения (службы)
        /// </summary>
        public int DivisionId { get; set; }

        /// <summary>
        /// ID начальника отдела (ссылка на AppUser)
        /// </summary>
        public string? ManagerId { get; set; }

        // ========== НАВИГАЦИОННЫЕ СВОЙСТВА ==========

        /// <summary>
        /// Подразделение (служба)
        /// </summary>
        public Division? Division { get; set; }

        /// <summary>
        /// Начальник отдела
        /// </summary>
        public AppUser? Manager { get; set; }

        /// <summary>
        /// Список сотрудников отдела (кроме начальника)
        /// </summary>
        public List<AppUser> Employees { get; set; } = new();

    }
}
