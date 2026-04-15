using OfficeOpenXml;
using WorkProcesses.Data;
using WorkProcesses.Models;
using Microsoft.EntityFrameworkCore;

namespace WorkProcesses.Services
{
    public class ExcelExportService
    {
        private readonly AppDbContext _context;

        public ExcelExportService(AppDbContext context)
        {
            _context = context;
            // Устанавливаем лицензию EPPlus (для бесплатного использования)
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        /// <summary>
        /// Экспорт всех заданий с отчётами в Excel
        /// </summary>
        public async Task<byte[]> ExportAllTasksToExcel()
        {
            var tasks = await _context.Tasks
                .Include(t => t.AssignedTo)
                .Include(t => t.AssignedBy)
                .Include(t => t.Reports)
                .ThenInclude(r => r.AppUser)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Задания");

            // Заголовки
            worksheet.Cells[1, 1].Value = "ID";
            worksheet.Cells[1, 2].Value = "Название";
            worksheet.Cells[1, 3].Value = "Описание";
            worksheet.Cells[1, 4].Value = "Кому";
            worksheet.Cells[1, 5].Value = "Кто выдал";
            worksheet.Cells[1, 6].Value = "Срок";
            worksheet.Cells[1, 7].Value = "Тип";
            worksheet.Cells[1, 8].Value = "Статус";
            worksheet.Cells[1, 9].Value = "Кол-во отчётов";
            worksheet.Cells[1, 10].Value = "Последний отчёт";

            // Стиль заголовка
            using (var range = worksheet.Cells[1, 1, 1, 10])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
            }

            int row = 2;
            foreach (var task in tasks)
            {
                worksheet.Cells[row, 1].Value = task.Id;
                worksheet.Cells[row, 2].Value = task.Title;
                worksheet.Cells[row, 3].Value = task.Description;
                worksheet.Cells[row, 4].Value = task.AssignedTo?.FullName ?? "—";
                worksheet.Cells[row, 5].Value = task.AssignedBy?.FullName ?? "—";
                worksheet.Cells[row, 6].Value = task.Deadline.ToString("dd.MM.yyyy HH:mm");
                worksheet.Cells[row, 7].Value = task.TaskType switch
                {
                    TaskType.Single => "Разовое",
                    TaskType.Daily => "Ежедневное",
                    TaskType.Weekly => "Еженедельное",
                    _ => ""
                };
                worksheet.Cells[row, 8].Value = task.IsCompleted ? "Выполнено" : (task.IsOverdue ? "Просрочено" : "В работе");
                worksheet.Cells[row, 9].Value = task.Reports.Count;
                worksheet.Cells[row, 10].Value = task.Reports.Any() ? task.Reports.Max(r => r.ReportedAt).ToString("dd.MM.yyyy HH:mm") : "—";
                row++;
            }

            worksheet.Cells[1, 1, row - 1, 10].AutoFitColumns();

            return await package.GetAsByteArrayAsync();
        }

        /// <summary>
        /// Экспорт отчётов по заданию
        /// </summary>
        public async Task<byte[]> ExportReportsByTask(int taskId)
        {
            var task = await _context.Tasks
                .Include(t => t.AssignedTo)
                .Include(t => t.Reports)
                .ThenInclude(r => r.AppUser)
                .FirstOrDefaultAsync(t => t.Id == taskId);

            if (task == null) return Array.Empty<byte>();

            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add($"Отчёты_задания_{taskId}");

            worksheet.Cells[1, 1].Value = "ID отчёта";
            worksheet.Cells[1, 2].Value = "Сотрудник";
            worksheet.Cells[1, 3].Value = "Дата сдачи";
            worksheet.Cells[1, 4].Value = "За какую дату";
            worksheet.Cells[1, 5].Value = "Текст отчёта";
            worksheet.Cells[1, 6].Value = "Принят";
            worksheet.Cells[1, 7].Value = "Кто принял";
            worksheet.Cells[1, 8].Value = "Дата принятия";

            using (var range = worksheet.Cells[1, 1, 1, 8])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
            }

            int row = 2;
            foreach (var report in task.Reports.OrderByDescending(r => r.ReportedAt))
            {
                worksheet.Cells[row, 1].Value = report.Id;
                worksheet.Cells[row, 2].Value = report.AppUser?.FullName ?? "—";
                worksheet.Cells[row, 3].Value = report.ReportedAt.ToString("dd.MM.yyyy HH:mm");
                worksheet.Cells[row, 4].Value = report.ForDate.ToString("dd.MM.yyyy");
                worksheet.Cells[row, 5].Value = report.Text;
                worksheet.Cells[row, 6].Value = report.IsApproved ? "Да" : "Нет";
                worksheet.Cells[row, 7].Value = report.ApprovedBy?.FullName ?? "—";
                worksheet.Cells[row, 8].Value = report.ApprovedAt?.ToString("dd.MM.yyyy HH:mm") ?? "—";
                row++;
            }

            worksheet.Cells[1, 1, row - 1, 8].AutoFitColumns();
            return await package.GetAsByteArrayAsync();
        }
    }
}