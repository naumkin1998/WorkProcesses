namespace WorkProcesses.Models
{
    public class WorkType
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty; // "Разработка", "Сопровождение", "Техподдержка"
    }
}