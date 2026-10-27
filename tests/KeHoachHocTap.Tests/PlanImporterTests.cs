using KeHoachHocTap.Web.Data;
using KeHoachHocTap.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace KeHoachHocTap.Tests;

public class PlanImporterTests : IAsyncLifetime
{
    private readonly TestDatabaseFixture _fixture;
    private PlanImporter _importer = null!;
    private readonly string _sampleExcelPath;

    public PlanImporterTests()
    {
        _fixture = new TestDatabaseFixture();
        _sampleExcelPath = Path.Combine(AppContext.BaseDirectory, "TestData", "ke_hoach_hoc_tap.xlsx");
    }

    public async Task InitializeAsync()
    {
        await _fixture.InitializeAsync();
        var reader = new ExcelPlanReader();
        var logger = NullLogger<PlanImporter>.Instance;
        _importer = new PlanImporter(_fixture.Context, reader, logger);
    }

    public async Task DisposeAsync()
    {
        await _fixture.DisposeAsync();
    }

    [Fact]
    public async Task ImportAsync_SampleExcel_ImportsAll19SubjectsAnd6Holidays()
    {
        await using var stream = File.OpenRead(_sampleExcelPath);
        var result = await _importer.ImportAsync(stream);

        Assert.True(result.Success);
        Assert.Equal(19, result.SubjectsImported);
        Assert.Equal(6, result.HolidaysImported);

        var subjects = await _fixture.Context.Subjects.OrderBy(s => s.SortOrder).ToListAsync();
        Assert.Equal(19, subjects.Count);
        for (int i = 0; i < 19; i++)
        {
            Assert.Equal(i + 1, subjects[i].SortOrder);
        }

        var holidays = await _fixture.Context.Holidays.ToListAsync();
        Assert.Equal(6, holidays.Count);
        Assert.Single(holidays, h => h.IsOpeningDay);
    }

    [Fact]
    public async Task ImportAsync_PreserveExistingData_PreservesGradeReserveAndNote()
    {
        // 1. Initial import
        await using (var stream = File.OpenRead(_sampleExcelPath))
        {
            await _importer.ImportAsync(stream);
        }

        // 2. Modify grade, reserve periods, and note on some subjects
        var sub1 = await _fixture.Context.Subjects.FirstAsync(s => s.SortOrder == 1);
        sub1.Grade = 9.5m;
        sub1.ReservePeriods = 4;
        sub1.Note = "Ghi chú môn 1";

        var sub2 = await _fixture.Context.Subjects.FirstAsync(s => s.SortOrder == 2);
        sub2.Grade = 8.0m;

        await _fixture.Context.SaveChangesAsync();

        // 3. Import again with preserveExistingData = true
        await using (var stream2 = File.OpenRead(_sampleExcelPath))
        {
            var result = await _importer.ImportAsync(stream2, preserveExistingData: true);
            Assert.True(result.Success);
        }

        // 4. Verify preserved values
        var reloadedSub1 = await _fixture.Context.Subjects.FirstAsync(s => s.SortOrder == 1);
        Assert.Equal(9.5m, reloadedSub1.Grade);
        Assert.Equal(4, reloadedSub1.ReservePeriods);
        Assert.Equal("Ghi chú môn 1", reloadedSub1.Note);

        var reloadedSub2 = await _fixture.Context.Subjects.FirstAsync(s => s.SortOrder == 2);
        Assert.Equal(8.0m, reloadedSub2.Grade);

        // 5. Import with preserveExistingData = false resets customized fields
        await using (var stream3 = File.OpenRead(_sampleExcelPath))
        {
            var result = await _importer.ImportAsync(stream3, preserveExistingData: false);
            Assert.True(result.Success);
        }

        var resetSub1 = await _fixture.Context.Subjects.FirstAsync(s => s.SortOrder == 1);
        Assert.Null(resetSub1.Grade);
        Assert.Null(resetSub1.ReservePeriods);
        Assert.Null(resetSub1.Note);
    }

    [Fact]
    public async Task ImportAsync_InvalidExcelStream_RollsBackTransaction_PreservesOldData()
    {
        // 1. Seed initial data
        await using (var stream = File.OpenRead(_sampleExcelPath))
        {
            await _importer.ImportAsync(stream);
        }

        var originalCount = await _fixture.Context.Subjects.CountAsync();
        Assert.Equal(19, originalCount);

        // 2. Try importing an invalid/corrupted stream
        using var corruptedStream = new MemoryStream(new byte[] { 0x01, 0x02, 0x03, 0x04 });
        var result = await _importer.ImportAsync(corruptedStream);

        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);

        // 3. Verify original data was preserved and not deleted or partially changed
        var countAfterFailedImport = await _fixture.Context.Subjects.CountAsync();
        Assert.Equal(19, countAfterFailedImport);
    }
}
