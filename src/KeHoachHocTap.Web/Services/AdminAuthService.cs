namespace KeHoachHocTap.Web.Services;

using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;

/// <summary>
/// Dịch vụ xác thực quản trị viên sử dụng CryptographicOperations.FixedTimeEquals chống tấn công định thời.
/// </summary>
public class AdminAuthService : IAdminAuthService
{
    private readonly IConfiguration _configuration;

    public AdminAuthService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public bool IsPasswordConfigured()
    {
        var password = _configuration["Admin:Password"];
        return !string.IsNullOrWhiteSpace(password);
    }

    public bool VerifyPassword(string? inputPassword)
    {
        if (string.IsNullOrEmpty(inputPassword))
        {
            return false;
        }

        var configuredPassword = _configuration["Admin:Password"];
        if (string.IsNullOrWhiteSpace(configuredPassword))
        {
            return false;
        }

        // Băm SHA-256 để cả 2 mảng byte luôn có độ dài cố định 32 bytes trước khi gọi FixedTimeEquals.
        // Điều này ngăn chặn kẻ tấn công suy đoán được độ dài của mật khẩu thông qua timing attacks.
        var inputHash = SHA256.HashData(Encoding.UTF8.GetBytes(inputPassword));
        var configHash = SHA256.HashData(Encoding.UTF8.GetBytes(configuredPassword));

        return CryptographicOperations.FixedTimeEquals(inputHash, configHash);
    }
}
