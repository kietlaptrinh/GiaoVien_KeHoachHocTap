namespace KeHoachHocTap.Web.Services;

/// <summary>
/// Giao diện dịch vụ kiểm tra mật khẩu và xác thực quyền Quản trị viên.
/// </summary>
public interface IAdminAuthService
{
    /// <summary>
    /// Kiểm tra mật khẩu quản trị đã được cấu hình trong hệ thống hay chưa.
    /// Nếu chưa cấu hình, hệ thống chuyển về chế độ chỉ xem.
    /// </summary>
    bool IsPasswordConfigured();

    /// <summary>
    /// Xác thực mật khẩu quản trị bằng so sánh an toàn CryptographicOperations.FixedTimeEquals.
    /// </summary>
    /// <param name="inputPassword">Mật khẩu do người dùng nhập vào.</param>
    /// <returns>True nếu mật khẩu trùng khớp; ngược lại False.</returns>
    bool VerifyPassword(string? inputPassword);
}
