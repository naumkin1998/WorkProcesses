namespace WorkProcesses.Models
{
    public class WorkBasis
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty; // "План", "Заявка", "Инструкция"
        public string? Description { get; set; }
    }
}