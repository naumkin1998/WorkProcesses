namespace WorkProcesses.Models
{
    public class ResourceDepartment
    {
        public int Id { get; set; }
        public int ResourceId { get; set; }
        public Resource Resource { get; set; } = null!;
        public int DepartmentId { get; set; }
        public Department Department { get; set; } = null!;
    }
}