using KeHoachHocTap.Web.Data;
using KeHoachHocTap.Web.Domain;
using Half = KeHoachHocTap.Web.Domain.Half;

namespace KeHoachHocTap.Web.Services;

/// <summary>
/// Triển khai dịch vụ kế hoạch học tập, kết nối Repository và ScheduleCalculator.
/// </summary>
public class PlanService : IPlanService
{
    private readonly IPlanRepository _repository;
    private readonly IScheduleCalculator _calculator;

    public PlanService(
        IPlanRepository repository,
        IScheduleCalculator calculator)
    {
        _repository = repository;
        _calculator = calculator;
    }

    public async Task<ScheduleResult> GetScheduleAsync(CancellationToken cancellationToken = default)
    {
        var subjects = await _repository.GetSubjectsAsync(cancellationToken);
        var holidays = await _repository.GetHolidaysAsync(cancellationToken);
        var firstHalfSetting = await _repository.GetSettingAsync("Plan:FirstSessionHalf", "S", cancellationToken);
        var firstSessionHalf = "C".Equals(firstHalfSetting, StringComparison.OrdinalIgnoreCase) ? Half.C : Half.S;

        return _calculator.Calculate(subjects, holidays, firstSessionHalf);
    }

    public async Task<IReadOnlyList<Subject>> GetSubjectsAsync(CancellationToken cancellationToken = default)
    {
        return await _repository.GetSubjectsAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Holiday>> GetHolidaysAsync(CancellationToken cancellationToken = default)
    {
        return await _repository.GetHolidaysAsync(cancellationToken);
    }

    public async Task<Holiday?> GetOpeningDayAsync(CancellationToken cancellationToken = default)
    {
        return await _repository.GetOpeningDayAsync(cancellationToken);
    }
}
