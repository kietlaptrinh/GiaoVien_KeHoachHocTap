using KeHoachHocTap.Web.Data;
using KeHoachHocTap.Web.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace KeHoachHocTap.Tests;

public class PlanServiceTests : IAsyncLifetime
{
    private readonly TestDatabaseFixture _fixture;
    private PlanService _planService = null!;
    private PlanRepository _repository = null!;
    private readonly string _sampleExcelPath;

    public PlanServiceTests()
    {
        _fixture = new TestDatabaseFixture();
        _sampleExcelPath = Path.Combine(AppContext.BaseDirectory, "TestData", "ke_hoach_hoc_tap.xlsx");
    }

    public async Task InitializeAsync()
    {
        await _fixture.InitializeAsync();
        _repository = new PlanRepository(_fixture.Context);
        var calculator = new ScheduleCalculator();
        _planService = new PlanService(_repository, calculator);

        // Nạp dữ liệu mẫu ban đầu vào DB
        var reader = new ExcelPlanReader();
        var importer = new PlanImporter(_fixture.Context, reader, NullLogger<PlanImporter>.Instance);
        await using var stream = File.OpenRead(_sampleExcelPath);
        var importResult = await importer.ImportAsync(stream);
        Assert.True(importResult.Success);
    }

    public async Task DisposeAsync()
    {
        await _fixture.DisposeAsync();
    }

    [Fact]
    public async Task GetScheduleAsync_MatchesGoldenScheduleExactly()
    {
        var result = await _planService.GetScheduleAsync();

        Assert.Equal(new DateOnly(2025, 9, 10), result.OpeningDay);
        Assert.Equal(19, result.Items.Count);

        var expectedGolden = new[]
        {
            (1, "Tên môn học 01", 60, 12, "S 11/9/2025", "C 18/9/2025", "S 19/9/2025"),
            (2, "Tên môn học 02", 55, 11, "C 19/9/2025", "C 26/9/2025", "S 29/9/2025"),
            (3, "Tên môn học 03", 55, 11, "C 29/9/2025", "C 6/10/2025", "S 7/10/2025"),
            (4, "Tên môn học 04", 40, 8, "C 7/10/2025", "S 13/10/2025", "C 13/10/2025"),
            (5, "Tên môn học 05", 60, 12, "S 14/10/2025", "C 21/10/2025", "S 22/10/2025"),
            (6, "Tên môn học 06", 70, 14, "C 22/10/2025", "S 31/10/2025", "C 31/10/2025"),
            (7, "Tên môn học 07", 45, 9, "S 3/11/2025", "S 7/11/2025", "C 7/11/2025"),
            (8, "Tên môn học 08", 45, 9, "S 10/11/2025", "S 14/11/2025", "C 14/11/2025"),
            (9, "Tên môn học 09", 40, 8, "S 17/11/2025", "C 20/11/2025", "S 21/11/2025"),
            (10, "Tên môn học 10", 45, 9, "C 21/11/2025", "C 27/11/2025", "S 28/11/2025"),
            (11, "Tên môn học 11", 70, 14, "C 28/11/2025", "S 9/12/2025", "C 9/12/2025"),
            (12, "Tên môn học 12", 35, 7, "S 10/12/2025", "S 15/12/2025", "C 15/12/2025"),
            (13, "Tên môn học 13", 50, 10, "S 16/12/2025", "C 22/12/2025", "S 23/12/2025"),
            (14, "Tên môn học 14", 45, 9, "C 23/12/2025", "C 29/12/2025", "S 30/12/2025"),
            (15, "Tên môn học 15", 40, 8, "C 30/12/2025", "S 6/1/2026", "C 6/1/2026"),
            (16, "Tên môn học 16", 35, 7, "S 7/1/2026", "S 12/1/2026", "C 12/1/2026"),
            (17, "Tên môn học 17", 30, 6, "S 13/1/2026", "C 15/1/2026", "S 16/1/2026"),
            (18, "Tên môn học 18", 35, 7, "C 16/1/2026", "C 21/1/2026", "S 22/1/2026"),
            (19, "Tên môn học 19", 40, 8, "C 22/1/2026", "S 28/1/2026", "C 28/1/2026")
        };

        for (int i = 0; i < expectedGolden.Length; i++)
        {
            var exp = expectedGolden[i];
            var actual = result.Items[i];

            Assert.Equal(exp.Item1, actual.Number);
            Assert.Equal(exp.Item2, actual.Subject.Name);
            Assert.Equal(exp.Item3, actual.Subject.Periods);
            Assert.Equal(exp.Item4, actual.Sessions);
            Assert.Equal(exp.Item5, actual.Start.ToString());
            Assert.Equal(exp.Item6, actual.End.ToString());
            Assert.Equal(exp.Item7, actual.Exam.ToString());
        }
    }

    [Fact]
    public async Task ModifyingSubjectOrHolidays_DynamicallyUpdatesSchedule()
    {
        // Thêm một ngày nghỉ đột xuất vào ngày 12/9/2025
        await _repository.AddHolidayAsync(new Holiday
        {
            Name = "Nghỉ đột xuất",
            From = new DateOnly(2025, 9, 12),
            To = new DateOnly(2025, 9, 12),
            IsOpeningDay = false
        });

        // Tính lại lịch
        var updatedSchedule = await _planService.GetScheduleAsync();

        // Môn 1 trước đây kết thúc vào C 18/9/2025, sau khi nghỉ 1 ngày (2 buổi) phải dịch sang C 19/9/2025
        var sub1 = updatedSchedule.Items[0];
        Assert.Equal("S 11/9/2025", sub1.Start.ToString());
        Assert.Equal("C 19/9/2025", sub1.End.ToString());
        Assert.Equal("S 22/9/2025", sub1.Exam.ToString());
    }

    [Fact]
    public async Task GetScheduleAsync_WithFirstSessionHalfAfternoon_StartsSubject1InAfternoon()
    {
        // Cấu hình buổi bắt đầu môn 1 là Buổi Chiều (C)
        await _repository.SetSettingAsync("Plan:FirstSessionHalf", "C");

        var schedule = await _planService.GetScheduleAsync();

        var sub1 = schedule.Items[0];
        // Khai giảng: 10/9/2025 (T4)
        // Ngày làm việc kế tiếp: 11/9/2025 (T5)
        // Với firstSessionHalf = C: Bắt đầu vào C 11/9/2025
        Assert.Equal("C 11/9/2025", sub1.Start.ToString());
    }
}
