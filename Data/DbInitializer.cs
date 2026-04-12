using Microsoft.AspNetCore.Identity;
using WorkProcesses.Models;

namespace WorkProcesses.Data
{
    /// <summary>
    /// Класс для первоначального заполнения базы данных
    /// Создаёт роли и администратора при первом запуске
    /// </summary>
    public static class DbInitializer
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<AppUser>>();
            var context = serviceProvider.GetRequiredService<AppDbContext>();

            // Создаём БД, если её нет
            await context.Database.EnsureCreatedAsync();

            // ========== СОЗДАЁМ РОЛИ ==========
            string[] roles = { RoleNames.Admin, RoleNames.ServiceHead, RoleNames.DepartmentHead, RoleNames.Employee };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // ========== СОЗДАЁМ АДМИНИСТРАТОРА ==========
            var adminEmail = "admin@workprocesses.com";
            if (await userManager.FindByEmailAsync(adminEmail) == null)
            {
                var admin = new AppUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = "Системный Администратор",
                    Position = "Администратор",
                    CurrentStatus = StatusType.Present
                };

                var result = await userManager.CreateAsync(admin, "Admin123!");

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(admin, RoleNames.Admin);
                }
            }
        }
    }
}