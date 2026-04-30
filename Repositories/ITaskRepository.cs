using WorkProcesses.Models;

namespace WorkProcesses.Repositories
{
    public interface ITaskRepository : IRepository<TaskItem>
    {
        Task<IEnumerable<TaskItem>> GetTasksByAssigneeAsync(string userId);
        Task<IEnumerable<TaskItem>> GetOverdueTasksAsync();
    }
}