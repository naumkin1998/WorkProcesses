namespace WorkProcesses.Models
{
    public class Project
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        // Включить ли проект в месячный/недельный план
        public bool IncludeInMonthlyPlan { get; set; }
        public bool IncludeInWeeklyPlan { get; set; }
        // Связь с заданиями
        public List<TaskItem> Tasks { get; set; } = new();
    }
}