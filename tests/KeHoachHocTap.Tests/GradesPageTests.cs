using System.Security.Claims;
using KeHoachHocTap.Web.Data;
using KeHoachHocTap.Web.Pages;
using KeHoachHocTap.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;

namespace KeHoachHocTap.Tests;

public class GradesPageTests : IAsyncLifetime
{
    private readonly TestDatabaseFixture _fixture;
    private PlanRepository _repository = null!;
    private IGradeService _gradeService = null!;
    private IExcelGradeImporter _gradeImporter = null!;
    private IAdminAuthService _authService = null!;
    private IAuthorizationService _authorizationService = null!;
    private IWebHostEnvironment _environment = null!;

    public GradesPageTests()
    {
        _fixture = new TestDatabaseFixture();
    }

    public async Task InitializeAsync()
    {
        await _fixture.InitializeAsync();
        _repository = new PlanRepository(_fixture.Context);

        var wordsConverter = new VietnameseNumberToWordsConverter();
        var classifier = new GradeRankClassifier();
        _gradeService = new GradeService(wordsConverter, classifier);
        _gradeImporter = new ExcelGradeImporter();

        _authService = new StubAdminAuthService();
        _authorizationService = new StubAuthorizationService(true);
        _environment = new StubWebHostEnvironment();
    }

    public async Task DisposeAsync()
    {
        await _fixture.DisposeAsync();
    }

    private GradesModel CreatePageModel(ClaimsPrincipal user)
    {
        var httpContext = new DefaultHttpContext { User = user };
        var modelState = new ModelStateDictionary();
        var actionContext = new ActionContext(httpContext, new Microsoft.AspNetCore.Routing.RouteData(), new PageActionDescriptor(), modelState);
        var modelMetadataProvider = new EmptyModelMetadataProvider();
        var viewData = new ViewDataDictionary(modelMetadataProvider, modelState);
        var tempData = new TempDataDictionary(httpContext, new StubTempDataProvider());
        var pageContext = new PageContext(actionContext)
        {
            ViewData = viewData
        };

        return new GradesModel(
            _repository,
            _gradeService,
            _gradeImporter,
            _authService,
            _authorizationService,
            _environment,
            NullLogger<GradesModel>.Instance)
        {
            PageContext = pageContext,
            TempData = tempData
        };
    }

    [Fact]
    public async Task OnGetAsync_LoadsStudentsAndCalculatesGradesSheet()
    {
        // Setup môn học
        var sub1 = new Subject { Name = "Triết học Mác - Lênin", Periods = 60, SortOrder = 1 };
        var sub2 = new Subject { Name = "Kinh tế chính trị", Periods = 55, SortOrder = 2 };
        await _repository.AddSubjectAsync(sub1);
        await _repository.AddSubjectAsync(sub2);
        var subjects = await _repository.GetSubjectsAsync();

        // Setup học viên
        var st1 = new Student
        {
            SortOrder = 1,
            StudentCode = "K75A56.0101",
            LastName = "Đặng Thị Hội",
            FirstName = "An",
            DateOfBirth = new DateOnly(1982, 4, 14),
            Gender = "Nữ"
        };
        await _repository.AddStudentAsync(st1);
        var students = await _repository.GetStudentsWithGradesAsync();

        // Lưu điểm
        await _repository.UpdateSingleGradeAsync(students[0].Id, subjects[0].Id, 8.00m);
        await _repository.UpdateSingleGradeAsync(students[0].Id, subjects[1].Id, 8.50m);

        var adminUser = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, "Admin") }, "Cookie"));
        var page = CreatePageModel(adminUser);

        await page.OnGetAsync();

        Assert.NotNull(page.GradeSheet);
        Assert.Equal(1, page.TotalStudents);
        Assert.Equal(2, page.Subjects.Count);
        Assert.Single(page.DisplayStudents);

        var first = page.DisplayStudents[0];
        Assert.Equal("Đặng Thị Hội An", first.Student.FullName);
        Assert.Equal(2, first.CompletedSubjectsCount);
        Assert.Equal(8.25m, first.AverageScore);
        Assert.Equal("Giỏi", first.Classification);
        Assert.Equal("Tám phẩy hai mươi lăm", first.AverageScoreInWords);
    }

    [Fact]
    public async Task OnGetAsync_SearchAndFilter_WorksAccurately()
    {
        var st1 = new Student { SortOrder = 1, StudentCode = "K75A56.0101", LastName = "Đặng Thị Hội", FirstName = "An" };
        var st2 = new Student { SortOrder = 2, StudentCode = "K75A56.0102", LastName = "Nguyễn Văn", FirstName = "Bình" };
        await _repository.AddStudentAsync(st1);
        await _repository.AddStudentAsync(st2);

        var adminUser = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, "Admin") }, "Cookie"));
        var page = CreatePageModel(adminUser);

        page.Search = "Bình";
        await page.OnGetAsync();

        Assert.Single(page.DisplayStudents);
        Assert.Equal("Nguyễn Văn Bình", page.DisplayStudents[0].Student.FullName);
    }

    [Fact]
    public async Task OnPostUpdateGradeAsync_UpdatesSingleScore_ReturnsJsonResult()
    {
        var sub = new Subject { Name = "Xây dựng Đảng", Periods = 45, SortOrder = 1 };
        await _repository.AddSubjectAsync(sub);
        var savedSub = (await _repository.GetSubjectsAsync())[0];

        var st = new Student { SortOrder = 1, StudentCode = "K75A56.0101", LastName = "Trần", FirstName = "Đức" };
        await _repository.AddStudentAsync(st);
        var savedSt = (await _repository.GetStudentsWithGradesAsync())[0];

        var adminUser = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, "Admin") }, "Cookie"));
        var page = CreatePageModel(adminUser);

        var actionResult = await page.OnPostUpdateGradeAsync(savedSt.Id, savedSub.Id, 7.85m);

        var json = Assert.IsType<JsonResult>(actionResult);
        Assert.NotNull(json.Value);

        var students = await _repository.GetStudentsWithGradesAsync();
        var grade = Assert.Single(students[0].Grades);
        Assert.Equal(7.85m, grade.Score);
    }

    [Fact]
    public async Task OnGetExportExcelAsync_GeneratesValidExcelFile()
    {
        var sub = new Subject { Name = "CNXHKH", Periods = 40, SortOrder = 1 };
        await _repository.AddSubjectAsync(sub);

        var st = new Student { SortOrder = 1, StudentCode = "K75A56.0101", LastName = "Lê", FirstName = "Hoa" };
        await _repository.AddStudentAsync(st);

        var adminUser = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, "Admin") }, "Cookie"));
        var page = CreatePageModel(adminUser);

        var result = await page.OnGetExportExcelAsync();

        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileResult.ContentType);
        Assert.True(fileResult.FileContents.Length > 0);
    }

    private class StubAdminAuthService : IAdminAuthService
    {
        public bool IsPasswordConfigured() => true;
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

    private class StubWebHostEnvironment : IWebHostEnvironment
    {
        public string WebRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider WebRootFileProvider { get; set; } = null!;
        public string ApplicationName { get; set; } = "KeHoachHocTap.Web";
        public IFileProvider ContentRootFileProvider { get; set; } = null!;
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public string EnvironmentName { get; set; } = "Development";
    }

    private class StubTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
    }
}
