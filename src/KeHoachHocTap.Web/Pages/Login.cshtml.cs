namespace KeHoachHocTap.Web.Pages;

using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using KeHoachHocTap.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

public class LoginModel : PageModel
{
    private readonly IAdminAuthService _authService;
    private readonly ILogger<LoginModel> _logger;

    public LoginModel(IAdminAuthService authService, ILogger<LoginModel> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [BindProperty]
    [Required(ErrorMessage = "Vui lòng nhập mật khẩu quản trị.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public bool IsPasswordConfigured { get; private set; }

    public void OnGet()
    {
        IsPasswordConfigured = _authService.IsPasswordConfigured();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        IsPasswordConfigured = _authService.IsPasswordConfigured();

        if (!IsPasswordConfigured)
        {
            ModelState.AddModelError(string.Empty, "Hệ thống chưa thiết lập mật khẩu quản trị (Admin:Password). Đăng nhập bị vô hiệu hóa và hệ thống chuyển về chế độ chỉ xem.");
            return Page();
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var isCorrect = _authService.VerifyPassword(Password);

        if (!isCorrect)
        {
            // SPEC mục 12: Chờ 1 giây để chống tấn công vét cạn (brute-force)
            await Task.Delay(1000);
            ModelState.AddModelError(string.Empty, "Mật khẩu quản trị không chính xác. Vui lòng kiểm tra lại.");
            return Page();
        }

        // Tạo danh tính quản trị viên với Cookie hết hạn sau 8 giờ
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, "Quản trị viên"),
            new Claim(ClaimTypes.Role, "Admin")
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        var authProperties = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
        };

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProperties);
        _logger.LogInformation("Quản trị viên đã đăng nhập thành công vào lúc {Time}", DateTimeOffset.UtcNow);

        if (!string.IsNullOrEmpty(ReturnUrl) && Url.IsLocalUrl(ReturnUrl))
        {
            return LocalRedirect(ReturnUrl);
        }

        return RedirectToPage("/Index");
    }

    public async Task<IActionResult> OnPostLogoutAsync()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        _logger.LogInformation("Quản trị viên đã đăng xuất.");
        return RedirectToPage("/Index");
    }

    public async Task<IActionResult> OnGetLogoutAsync()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        _logger.LogInformation("Quản trị viên đã đăng xuất.");
        return RedirectToPage("/Index");
    }
}
