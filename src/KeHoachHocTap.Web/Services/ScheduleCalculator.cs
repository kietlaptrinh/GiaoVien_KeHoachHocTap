using KeHoachHocTap.Web.Data;
using KeHoachHocTap.Web.Domain;
using Half = KeHoachHocTap.Web.Domain.Half;

namespace KeHoachHocTap.Web.Services;

/// <summary>
/// Triển khai thuật toán xếp lịch học thuần túy (Pure Function, không I/O).
/// </summary>
public class ScheduleCalculator : IScheduleCalculator
{
    private const int MaxSafetyYears = 5;
    private const int MaxSafetyDays = 365 * MaxSafetyYears;
    private const int MaxSafetySlots = MaxSafetyDays * 2;

    public ScheduleResult Calculate(IEnumerable<Subject> subjects, IEnumerable<Holiday> holidays)
    {
        var subjectList = subjects?.ToList() ?? new List<Subject>();
        var holidayList = holidays?.ToList() ?? new List<Holiday>();
        var warnings = new List<string>();

        // 1. Tìm ngày khai giảng
        var openingHoliday = holidayList.FirstOrDefault(h =>
            h.IsOpeningDay ||
            h.Name.Contains("khai giảng", StringComparison.OrdinalIgnoreCase) ||
            h.Name.Contains("khai giang", StringComparison.OrdinalIgnoreCase));

        if (openingHoliday == null)
        {
            warnings.Add("Chưa thiết lập ngày khai giảng trong danh sách ngày nghỉ.");
            return new ScheduleResult(DateOnly.MinValue, holidayList, Array.Empty<ScheduledSubject>(), warnings);
        }

        var openingDay = openingHoliday.From;

        // 2. Bung tập ngày nghỉ (bao gồm cả ngày khai giảng là ngày không học)
        var holidayDates = new HashSet<DateOnly> { openingDay };

        foreach (var h in holidayList)
        {
            if (h.To < h.From)
            {
                warnings.Add($"Kỳ nghỉ '{h.Name}' có Đến ngày ({h.To:d/M/yyyy}) trước Từ ngày ({h.From:d/M/yyyy}). Đã bỏ qua khoảng nghỉ này.");
                continue;
            }

            var current = h.From;
            while (current <= h.To)
            {
                holidayDates.Add(current);
                if (current == DateOnly.MaxValue) break;
                current = current.AddDays(1);
            }
        }

        // Helper: Kiểm tra ngày làm việc hợp lệ (Thứ 2 - Thứ 6 và không phải ngày nghỉ)
        bool IsWorkday(DateOnly date)
        {
            if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
                return false;
            return !holidayDates.Contains(date);
        }

        // Helper: Tìm ngày làm việc kế tiếp sau date
        DateOnly NextWorkday(DateOnly date)
        {
            var cur = date.AddDays(1);
            int safetyCount = 0;
            while (!IsWorkday(cur))
            {
                cur = cur.AddDays(1);
                safetyCount++;
                if (safetyCount > MaxSafetyDays)
                {
                    throw new InvalidOperationException("Lịch học kéo dài quá 5 năm hoặc không tìm thấy ngày học hợp lệ tiếp theo.");
                }
            }
            return cur;
        }

        // Helper: Tiến n buổi học hợp lệ (S rồi C, hết C sang S của NextWorkday)
        Slot Advance(Slot slot, int n)
        {
            if (n < 0) throw new ArgumentOutOfRangeException(nameof(n), "Số buổi dịch chuyển phải >= 0.");
            if (n == 0) return slot;

            var curDate = slot.Date;
            var curHalf = slot.Half;
            int safetyCount = 0;

            for (int i = 0; i < n; i++)
            {
                safetyCount++;
                if (safetyCount > MaxSafetySlots)
                {
                    throw new InvalidOperationException("Lịch học kéo dài quá 5 năm (vượt giới hạn an toàn chống lặp vô tận).");
                }

                if (curHalf == Half.S)
                {
                    curHalf = Half.C;
                }
                else
                {
                    curDate = NextWorkday(curDate);
                    curHalf = Half.S;
                }
            }

            return new Slot(curDate, curHalf);
        }

        // 3. Xếp lịch từng môn theo SortOrder
        var orderedSubjects = subjectList.OrderBy(s => s.SortOrder).ToList();
        var scheduledItems = new List<ScheduledSubject>();
        Slot? previousExam = null;
        int displayNumber = 1;

        foreach (var subject in orderedSubjects)
        {
            if (subject.Periods <= 0)
            {
                warnings.Add($"Môn học '{subject.Name}' (Stt {displayNumber}) có số tiết không hợp lệ ({subject.Periods} <= 0). Đã bỏ qua môn này.");
                displayNumber++;
                continue;
            }

            if (subject.Periods % 5 != 0)
            {
                warnings.Add($"Môn học '{subject.Name}' có số tiết ({subject.Periods}) không chia hết cho 5 (được làm tròn lên buổi).");
            }

            int sessions = (int)Math.Ceiling(subject.Periods / 5.0);

            try
            {
                Slot start;
                if (previousExam == null)
                {
                    // Môn đầu tiên: Bắt đầu từ buổi Sáng của ngày làm việc đầu tiên sau ngày khai giảng
                    var firstDay = NextWorkday(openingDay);
                    start = new Slot(firstDay, Half.S);
                }
                else
                {
                    // Môn tiếp theo: Bắt đầu ngay buổi kế tiếp sau buổi thi môn trước
                    start = Advance(previousExam.Value, 1);
                }

                var end = Advance(start, sessions - 1);
                var exam = Advance(end, 1);

                scheduledItems.Add(new ScheduledSubject(subject, displayNumber, sessions, start, end, exam));
                previousExam = exam;
            }
            catch (InvalidOperationException ex)
            {
                warnings.Add($"Đã dừng xếp lịch do vượt giới hạn an toàn: {ex.Message}");
                break;
            }

            displayNumber++;
        }

        return new ScheduleResult(openingDay, holidayList, scheduledItems, warnings);
    }
}
