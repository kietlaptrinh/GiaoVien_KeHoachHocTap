using KeHoachHocTap.Web.Data;
using KeHoachHocTap.Web.Export;
using KeHoachHocTap.Web.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages(options =>
{
    // Cấu hình Razor Pages
});

builder.Services.AddMemoryCache();

// Cấu hình Cookie Authentication & Policy Admin theo SPEC mục 12
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login";
        options.LogoutPath = "/Login";
        options.AccessDeniedPath = "/Login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Admin", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireRole("Admin");
    });
});

builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
});

// Đăng ký dịch vụ xác thực mật khẩu quản trị
builder.Services.AddSingleton<IAdminAuthService, AdminAuthService>();

// Đăng ký PlanDbContext với SQL Server
builder.Services.AddDbContext<PlanDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("Default");
    options.UseSqlServer(connectionString);
});

// Đăng ký dịch vụ lõi tính toán và đọc Excel
builder.Services.AddSingleton<IScheduleCalculator, ScheduleCalculator>();
builder.Services.AddTransient<IExcelPlanReader, ExcelPlanReader>();

// Đăng ký dịch vụ dữ liệu và nghiệp vụ kế hoạch
builder.Services.AddScoped<IPlanRepository, PlanRepository>();
builder.Services.AddScoped<IPlanImporter, PlanImporter>();
builder.Services.AddScoped<IPlanService, PlanService>();

// Đăng ký dịch vụ xuất file và quản lý template (SPEC mục 7, 7.3)
builder.Services.AddSingleton<IExporter, DocxBieuMauExporter>();
builder.Services.AddScoped<ITemplateService, TemplateService>();

var app = builder.Build();

// Khởi tạo Database và Seed dữ liệu ban đầu nếu cần
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    var configuration = services.GetRequiredService<IConfiguration>();

    try
    {
        var context = services.GetRequiredService<PlanDbContext>();

        var autoMigrate = configuration.GetValue<bool>("Database:AutoMigrate");
        if (autoMigrate)
        {
            logger.LogInformation("Đang kiểm tra và áp dụng Database Migrations...");
            await context.Database.MigrateAsync();
        }

        // Tự động Seed dữ liệu ban đầu khi cả hai bảng Subjects và Holidays đều trống
        var hasSubjects = await context.Subjects.AnyAsync();
        var hasHolidays = await context.Holidays.AnyAsync();

        if (!hasSubjects && !hasHolidays)
        {
            var seedRelativePath = configuration.GetValue<string>("Plan:SeedExcelPath") ?? "App_Data/ke_hoach_hoc_tap.xlsx";
            var seedPath = Path.Combine(app.Environment.ContentRootPath, seedRelativePath);

            if (File.Exists(seedPath))
            {
                logger.LogInformation("Cơ sở dữ liệu trống. Đang nạp dữ liệu kế hoạch học tập ban đầu từ '{SeedPath}'...", seedPath);
                var importer = services.GetRequiredService<IPlanImporter>();
                await using var stream = File.OpenRead(seedPath);
                var importResult = await importer.ImportAsync(stream, preserveExistingData: false);

                if (importResult.Success)
                {
                    logger.LogInformation("Nạp dữ liệu ban đầu thành công ({Subjects} môn học, {Holidays} ngày nghỉ).",
                        importResult.SubjectsImported, importResult.HolidaysImported);
                }
                else
                {
                    logger.LogWarning("Không thể nạp dữ liệu ban đầu: {Error}", importResult.ErrorMessage);
                }
            }
            else
            {
                logger.LogWarning("Không tìm thấy file nạp dữ liệu ban đầu tại đường dẫn: '{SeedPath}'.", seedPath);
            }
        }
    }
    catch (Exception ex)
    {
        // SPEC mục 9: Bắt lỗi ở biên (database) và trả thông báo tiếng Việt dễ hiểu; không lộ stack trace hay chuỗi kết nối cho người dùng
        logger.LogError(ex, "Không kết nối được cơ sở dữ liệu. Vui lòng kiểm tra lại cấu hình chuỗi kết nối và trạng thái hoạt động của SQL Server.");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.Run();
