using Microsoft.EntityFrameworkCore;
using WorkProcesses.Data;
using WorkProcesses.Models;

namespace WorkProcesses.Repositories
{
    public class TaskRepository : Repository<TaskItem>, ITaskRepository
    {
        public TaskRepository(AppDbContext context) : base(context) { }

        public async Task<IEnumerable<TaskItem>> GetTasksByAssigneeAsync(string userId)
        {
            return await _dbSet
                .Include(t => t.AssignedTo)
                .Include(t => t.AssignedBy)
                .Where(t => t.AssignedToId == userId)
                .ToListAsync();
        }

        public async Task<IEnumerable<TaskItem>> GetOverdueTasksAsync()
        {
            return await _dbSet
                .Where(t => t.Deadline < DateTime.Now && !t.IsCompleted)
                .ToListAsync();
        }
    }
}