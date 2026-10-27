using KeHoachHocTap.Web.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace KeHoachHocTap.Tests;

/// <summary>
/// Fixture quản lý vòng đời cơ sở dữ liệu tạm thời trên SQL Server cho kiểm thử tự động.
/// </summary>
public class TestDatabaseFixture : IAsyncDisposable
{
    public PlanDbContext Context { get; }
    public string ConnectionString { get; }

    public TestDatabaseFixture()
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var baseConn = config.GetConnectionString("Test")
            ?? "Server=localhost;Database=KeHoachHocTap_Test;User Id=sa;Password=123;TrustServerCertificate=True;";

        var builder = new SqlConnectionStringBuilder(baseConn)
        {
            InitialCatalog = $"KeHoachHocTap_Test_{Guid.NewGuid():N}"
        };

        ConnectionString = builder.ConnectionString;

        var options = new DbContextOptionsBuilder<PlanDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;

        Context = new PlanDbContext(options);
    }

    public async Task InitializeAsync()
    {
        await Context.Database.MigrateAsync();
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            await Context.Database.EnsureDeletedAsync();
        }
        catch
        {
            // Bỏ qua lỗi khi dọn dẹp cơ sở dữ liệu tạm
        }
        await Context.DisposeAsync();
    }
}
