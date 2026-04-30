using System.Security.AccessControl;

namespace WorkProcesses.Models
{
    /// <summary>
    /// Ресурс – например, «Сервер», «ПО», «Оборудование», «ИУС» и т.п.
    /// </summary>
    public class Resource
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }

        // Связь с типами ресурсов (опционально)
        public int? ResourceTypeId { get; set; }
        public ResourceType? ResourceType { get; set; }

        // Связь многие-ко-многим с подразделениями (через таблицу ResourceDepartment)
        public List<ResourceDepartment> ResourceDepartments { get; set; } = new();
    }
}