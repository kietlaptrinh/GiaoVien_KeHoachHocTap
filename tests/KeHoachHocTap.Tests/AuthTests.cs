namespace KeHoachHocTap.Tests;

using System.Security.Claims;
using KeHoachHocTap.Web.Data;
using KeHoachHocTap.Web.Domain;
using KeHoachHocTap.Web.Pages;
using KeHoachHocTap.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

public class AuthTests
{
    [Fact]
    public void IsPasswordConfigured_ShouldReturnTrue_WhenPasswordIsSet()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Admin:Password"] = "SecretAdmin123"
            })
            .Build();

        var authService = new AdminAuthService(config);

        Assert.True(authService.IsPasswordConfigured());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void IsPasswordConfigured_ShouldReturnFalse_WhenPasswordIsEmptyOrWhitespace(string? emptyPassword)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Admin:Password"] = emptyPassword
            })
            .Build();

        var authService = new AdminAuthService(config);

        Assert.False(authService.IsPasswordConfigured());
    }

    [Fact]
    public void VerifyPassword_ShouldReturnTrue_WhenPasswordMatches()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Admin:Password"] = "MatKhauChuan123!"
            })
            .Build();

        var authService = new AdminAuthService(config);

        Assert.True(authService.VerifyPassword("MatKhauChuan123!"));
    }

    [Theory]
    [InlineData("SaiMatKhau")]
    [InlineData("matkhauchuan123!")] // phân biệt hoa thường
    [InlineData("")]
    [InlineData(null)]
    [InlineData("MatKhauChuan123! ")] // có khoảng trắng thừa
    public void VerifyPassword_ShouldReturnFalse_WhenPasswordDoesNotMatch(string? input)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Admin:Password"] = "MatKhauChuan123!"
            })
            .Build();

        var authService = new AdminAuthService(config);

        Assert.False(authService.VerifyPassword(input));
    }

    [Fact]
    public void VerifyPassword_ShouldReturnFalse_WhenConfiguredPasswordIsEmpty()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Admin:Password"] = ""
            })
            .Build();

        var authService = new AdminAuthService(config);

        Assert.False(authService.VerifyPassword("BatKyMatKhauNao"));
    }

    [Fact]
    public async Task IndexModel_AddSubject_ShouldReturnForbid_WhenUserIsNotAdmin()
    {
        // Giả lập người dùng khách chưa đăng nhập
        var anonymousUser = new ClaimsPrincipal(new ClaimsIdentity());

        var authServiceMock = new StubAdminAuthService(isConfigured: true);
        var authService = new StubAuthorizationService(allow: false);

        var pageContext = CreatePageContext(anonymousUser);
        var model = new IndexModel(
            new StubPlanService(),
            new StubPlanRepository(),
            authServiceMock,
            authService,
            NullLogger<IndexModel>.Instance)
        {
            PageContext = pageContext
        };

        var input = new IndexModel.SubjectInputModel
        {
            Name = "Môn Test",
            Periods = 30
        };

        var result = await model.OnPostAddSubjectAsync(input);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task IndexModel_AddSubject_ShouldSucceed_WhenUserIsAdmin()
    {
        // Giả lập người dùng quản trị viên
        var adminClaims = new List<Claim>
        {
            new(ClaimTypes.Name, "Quản trị"),
            new(ClaimTypes.Role, "Admin")
        };
        var adminUser = new ClaimsPrincipal(new ClaimsIdentity(adminClaims, "Cookies"));

        var stubRepo = new StubPlanRepository();
        var pageContext = CreatePageContext(adminUser);
        var model = new IndexModel(
            new StubPlanService(),
            stubRepo,
            new StubAdminAuthService(isConfigured: true),
            new StubAuthorizationService(allow: true),
            NullLogger<IndexModel>.Instance)
        {
            PageContext = pageContext,
            TempData = new TempDataDictionary(pageContext.HttpContext, new StubTempDataProvider())
        };

        var input = new IndexModel.SubjectInputModel
        {
            Name = "Môn Học Hợp Lệ",
            Periods = 45,
            Grade = 8.5m
        };

        var result = await model.OnPostAddSubjectAsync(input);

        Assert.IsType<RedirectToPageResult>(result);
        Assert.Single(stubRepo.AddedSubjects);
        Assert.Equal("Môn Học Hợp Lệ", stubRepo.AddedSubjects[0].Name);
        Assert.Equal(8.5m, stubRepo.AddedSubjects[0].Grade);
    }

    [Fact]
    public void TemplatesModel_ShouldRequireAdminPolicy()
    {
        var authorizeAttr = typeof(TemplatesModel)
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Cast<AuthorizeAttribute>()
            .FirstOrDefault();

        Assert.NotNull(authorizeAttr);
        Assert.Equal("Admin", authorizeAttr.Policy);
    }

    // Helper stubs
    private static PageContext CreatePageContext(ClaimsPrincipal user)
    {
        var httpContext = new DefaultHttpContext { User = user };
        var modelState = new ModelStateDictionary();
        var actionContext = new ActionContext(httpContext, new(), new(), modelState);
        return new PageContext(actionContext);
    }

    private class StubAdminAuthService : IAdminAuthService
    {
        private readonly bool _isConfigured;
        public StubAdminAuthService(bool isConfigured) => _isConfigured = isConfigured;
        public bool IsPasswordConfigured() => _isConfigured;
        public bool VerifyPassword(string? inputPassword) => true;
    }

    private class StubAuthorizationService : IAuthorizationService
    {
        private readonly bool _allow;
        public StubAuthorizationService(bool allow) => _allow = allow;

        public Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user, object? resource, IEnumerable<IAuthorizationRequirement> requirements)
            => Task.FromResult(_allow ? AuthorizationResult.Success() : AuthorizationResult.Failed());

        public Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user, object? resource, string policyName)
            => Task.FromResult(_allow ? AuthorizationResult.Success() : AuthorizationResult.Failed());
    }

    private class StubPlanService : IPlanService
    {
        public Task<ScheduleResult> GetScheduleAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new ScheduleResult(new DateOnly(2025, 9, 11), Array.Empty<Holiday>(), Array.Empty<ScheduledSubject>(), Array.Empty<string>()));
        public Task<IReadOnlyList<Subject>> GetSubjectsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Subject>>(Array.Empty<Subject>());
        public Task<IReadOnlyList<Holiday>> GetHolidaysAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Holiday>>(Array.Empty<Holiday>());
        public Task<Holiday?> GetOpeningDayAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<Holiday?>(null);
    }

    private class StubPlanRepository : IPlanRepository
    {
        public List<Subject> AddedSubjects { get; } = new();
        public Task<IReadOnlyList<Subject>> GetSubjectsAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Subject>>(AddedSubjects);
        public Task<Subject?> GetSubjectByIdAsync(int id, CancellationToken cancellationToken = default) => Task.FromResult<Subject?>(null);
        public Task AddSubjectAsync(Subject subject, CancellationToken cancellationToken = default) { AddedSubjects.Add(subject); return Task.CompletedTask; }
        public Task UpdateSubjectAsync(Subject subject, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DeleteSubjectAsync(int id, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task MoveSubjectAsync(int subjectId, int direction, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IReadOnlyList<Holiday>> GetHolidaysAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Holiday>>(Array.Empty<Holiday>());
        public Task<Holiday?> GetHolidayByIdAsync(int id, CancellationToken cancellationToken = default) => Task.FromResult<Holiday?>(null);
        public Task<Holiday?> GetOpeningDayAsync(CancellationToken cancellationToken = default) => Task.FromResult<Holiday?>(null);
        public Task AddHolidayAsync(Holiday holiday, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task UpdateHolidayAsync(Holiday holiday, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DeleteHolidayAsync(int id, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<string?> GetSettingAsync(string key, string? defaultValue = null, CancellationToken cancellationToken = default) => Task.FromResult(defaultValue);
        public Task SetSettingAsync(string key, string value, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IReadOnlyDictionary<string, string>> GetAllSettingsAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyDictionary<string, string>>(new Dictionary<string, string>());
    }

    private class StubTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
    }
}
