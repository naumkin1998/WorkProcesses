using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using WorkProcesses.Data;
using WorkProcesses.Models;
using WorkProcesses.Services;

var builder = WebApplication.CreateBuilder(args);

// Добавляем DbContext с подключением к SQL Server
// AppDbContext будет использоваться для доступа к БД
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Добавляем Identity (систему пользователей) с нашим AppUser
// AddEntityFrameworkStores говорит, что пользователи хранятся в БД через AppDbContext
builder.Services.AddIdentity<AppUser, IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders()
     .AddDefaultUI(); //<-- ЭТА СТРОКА добавляет стандартные страницы входа

builder.Services.AddRazorPages(); // для Identity UI

// Настройка требований к паролю (для разработки можно упростить)
builder.Services.Configure<IdentityOptions>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 3;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
});

// Настройка cookies (страницы входа/выхода)
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
});

// Добавляем контроллеры с представлениями
builder.Services.AddControllersWithViews();
builder.Services.AddScoped<ExcelExportService>();

var app = builder.Build();

// Настройка конвейера HTTP
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();      // Для CSS, JS, изображений
app.UseRouting();

app.UseAuthentication();   // Аутентификация (кто пользователь)
app.UseAuthorization();    // Авторизация (что пользователь может)


app.MapRazorPages(); // для страниц Identity (Login, Register и т.д.)

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Инициализация базы данных (создание ролей и админа)
using (var scope = app.Services.CreateScope())
{
    await DbInitializer.InitializeAsync(scope.ServiceProvider);
}

app.Run();




/*var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();*/
