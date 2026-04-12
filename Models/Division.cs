namespace WorkProcesses.Models
{
    public class Division
    {
        public int Id { get; set; }

        /// <summary>
        /// Название службы/подразделения
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// ID начальника службы (ссылка на AppUser)
        /// </summary>
        public string? HeadId { get; set; }

        // ========== НАВИГАЦИОННЫЕ СВОЙСТВА ==========

        /// <summary>
        /// Начальник службы
        /// </summary>
        public AppUser? Head { get; set; }

        /// <summary>
        /// Список отделов в службе
        /// </summary>
        public List<Department> Departments { get; set; } = new();
    }
}
