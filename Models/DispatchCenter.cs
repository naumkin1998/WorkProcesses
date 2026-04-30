namespace WorkProcesses.Models
{
    public class DispatchCenter
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        // Связь с подразделениями
        public List<Division> Divisions { get; set; } = new();
    }
}