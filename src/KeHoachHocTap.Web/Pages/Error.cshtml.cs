using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace KeHoachHocTap.Web.Pages;

[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
[IgnoreAntiforgeryToken]
public class ErrorModel : PageModel
{
    public string? RequestId { get; set; }

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

    public bool IsDatabaseError { get; set; }

    public string ErrorMessage { get; set; } = "Đã xảy ra lỗi trong quá trình xử lý yêu cầu.";

    private readonly ILogger<ErrorModel> _logger;

    public ErrorModel(ILogger<ErrorModel> logger)
    {
        _logger = logger;
    }

    public void OnGet()
    {
        RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;

        var exceptionHandlerPathFeature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
        if (exceptionHandlerPathFeature?.Error != null)
        {
            var ex = exceptionHandlerPathFeature.Error;
            _logger.LogError(ex, "Đã xảy ra lỗi không xử lý được tại đường dẫn {Path}", exceptionHandlerPathFeature.Path);

            if (ex is SqlException || ex is DbUpdateException || ex.InnerException is SqlException)
            {
                IsDatabaseError = true;
                ErrorMessage = "Không thể kết nối đến cơ sở dữ liệu hoặc xảy ra lỗi trong thao tác dữ liệu. Vui lòng kiểm tra trạng thái hoạt động của SQL Server.";
            }
        }
    }
}
