using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WorkProcesses.Models;

namespace WorkProcesses.Data
{
    /// <summary>
    /// Контекст базы данных — главный класс для работы с БД
    /// Наследуется от IdentityDbContext, который содержит таблицы пользователей и ролей
    /// </summary>
    public class AppDbContext : IdentityDbContext<AppUser>
    {
        /// <summary>
        /// Конструктор, принимает настройки подключения к БД
        /// </summary>
        /// <param name="options">Настройки (строка подключения и т.д.)</param>
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        /// <summary>
        /// Таблица "Подразделения" (Divisions)
        /// DbSet<T> — это набор объектов, которые будут храниться в БД
        /// </summary>
        public DbSet<Division> Divisions { get; set; }

        /// <summary>
        /// Таблица "Отделы" (Departments)
        /// </summary>
        public DbSet<Department> Departments { get; set; }

        /// <summary>
        /// Таблица "Задания" (Tasks)
        /// </summary>
        public DbSet<TaskItem> Tasks { get; set; }

        /// <summary>
        /// Таблица "Отчёты" (Reports)
        /// </summary>
        public DbSet<Report> Reports { get; set; }

        /// <summary>
        /// Настройка связей между таблицами (Fluent API)
        /// Этот метод вызывается при создании модели БД
        /// </summary>
        protected override void OnModelCreating(ModelBuilder builder)
        {
            // Сначала вызываем базовую настройку Identity
            base.OnModelCreating(builder);

            // ========== НАСТРОЙКА СВЯЗЕЙ ==========

            // 1. Связь Department -> Division (многие к одному)
            // Один отдел принадлежит одному подразделению, в подразделении много отделов
            builder.Entity<Department>()
                .HasOne(d => d.Division)                    // У отдела есть одно подразделение
                .WithMany(d => d.Departments)               // У подразделения много отделов
                .HasForeignKey(d => d.DivisionId)           // Внешний ключ - DivisionId
                .OnDelete(DeleteBehavior.Cascade);          // При удалении подразделения удалить отделы

            // 2. Связь Department -> Manager (начальник отдела)
            // Начальник отдела — это пользователь (AppUser)
            builder.Entity<Department>()
                .HasOne(d => d.Manager)                     // У отдела есть начальник
                .WithMany()                                 // У пользователя может быть много отделов (но обычно один)
                .HasForeignKey(d => d.ManagerId)            // Внешний ключ - ManagerId
                .OnDelete(DeleteBehavior.SetNull);          // При удалении начальника — оставить отдел, но ManagerId = null

            // 3. Связь Division -> Head (начальник службы)
            builder.Entity<Division>()
                .HasOne(d => d.Head)                        // У подразделения есть начальник
                .WithMany()                                 // У пользователя может быть много подразделений
                .HasForeignKey(d => d.HeadId)               // Внешний ключ - HeadId
                .OnDelete(DeleteBehavior.SetNull);          // При удалении начальника — HeadId = null

            // 4. Связь TaskItem -> AssignedTo (кому назначено)
            builder.Entity<TaskItem>()
                .HasOne(t => t.AssignedTo)                  // Задание назначено одному сотруднику
                .WithMany(u => u.AssignedTasks)             // У сотрудника много заданий
                .HasForeignKey(t => t.AssignedToId)         // Внешний ключ - AssignedToId
                .OnDelete(DeleteBehavior.Restrict);         // Не удалять задание при удалении сотрудника

            // 5. Связь TaskItem -> AssignedBy (кто назначил)
            builder.Entity<TaskItem>()
                .HasOne(t => t.AssignedBy)                  // Задание выдано одним сотрудником
                .WithMany(u => u.IssuedTasks)               // У сотрудника много выданных заданий
                .HasForeignKey(t => t.AssignedById)         // Внешний ключ - AssignedById
                .OnDelete(DeleteBehavior.Restrict);         // Не удалять задание при удалении назначившего

            // 6. Связь Report -> TaskItem (отчёт к заданию)
            builder.Entity<Report>()
                .HasOne(r => r.TaskItem)                    // Отчёт относится к одному заданию
                .WithMany(t => t.Reports)                   // У задания много отчётов
                .HasForeignKey(r => r.TaskItemId)           // Внешний ключ - TaskItemId
                .OnDelete(DeleteBehavior.Cascade);          // При удалении задания удалить отчёты

            // 7. Связь Report -> AppUser (кто сдал отчёт)
            builder.Entity<Report>()
                .HasOne(r => r.AppUser)                     // Отчёт сдан одним сотрудником
                .WithMany(u => u.Reports)                   // У сотрудника много отчётов
                .HasForeignKey(r => r.AppUserId)            // Внешний ключ - AppUserId
                .OnDelete(DeleteBehavior.Restrict);         // Не удалять отчёты при удалении сотрудника

            // 8. Связь Report -> ApprovedBy (кто принял отчёт)
            builder.Entity<Report>()
                .HasOne(r => r.ApprovedBy)                  // Отчёт принят одним начальником
                .WithMany(u => u.ApprovedReports)           // У начальника много принятых отчётов
                .HasForeignKey(r => r.ApprovedById)         // Внешний ключ - ApprovedById
                .OnDelete(DeleteBehavior.SetNull);          // При удалении начальника — ApprovedById = null

            // ========== ИНДЕКСЫ (ускоряют поиск) ==========

            // Индекс для быстрого поиска заданий по исполнителю
            builder.Entity<TaskItem>()
                .HasIndex(t => t.AssignedToId);

            // Составной индекс для поиска просроченных заданий
            builder.Entity<TaskItem>()
                .HasIndex(t => new { t.Deadline, t.IsCompleted });

            // Индекс для поиска отчётов по заданию
            builder.Entity<Report>()
                .HasIndex(r => r.TaskItemId);

            // Индекс для поиска отчётов по сотруднику
            builder.Entity<Report>()
                .HasIndex(r => r.AppUserId);
        }
    }
}