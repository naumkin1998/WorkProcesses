namespace WorkProcesses.Models
{
    public class Priority
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty; // "Низкий", "Средний", "Высокий", "Критический"
        public int Level { get; set; } // числовой уровень для сортировки
    }
}