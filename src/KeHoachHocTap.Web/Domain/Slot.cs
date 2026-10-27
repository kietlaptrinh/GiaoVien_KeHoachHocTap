using System.Globalization;

namespace KeHoachHocTap.Web.Domain;

/// <summary>
/// Đại diện cho một buổi học cụ thể (Ngày + Buổi Sáng/Chiều).
/// Định dạng chuẩn: "S 11/9/2025" hoặc "C 18/9/2025" (không đệm số 0).
/// </summary>
public readonly record struct Slot(DateOnly Date, Half Half) : IComparable<Slot>, IComparable
{
    public static string Format(DateOnly date, Half half) =>
        $"{half} {date.Day}/{date.Month}/{date.Year}";

    public override string ToString() => Format(Date, Half);

    public int CompareTo(Slot other)
    {
        var dateComparison = Date.CompareTo(other.Date);
        if (dateComparison != 0) return dateComparison;
        return Half.CompareTo(other.Half);
    }

    public int CompareTo(object? obj)
    {
        if (obj is null) return 1;
        if (obj is Slot other) return CompareTo(other);
        throw new ArgumentException("Đối tượng so sánh phải có kiểu Slot.", nameof(obj));
    }

    public static bool operator <(Slot left, Slot right) => left.CompareTo(right) < 0;
    public static bool operator <=(Slot left, Slot right) => left.CompareTo(right) <= 0;
    public static bool operator >(Slot left, Slot right) => left.CompareTo(right) > 0;
    public static bool operator >=(Slot left, Slot right) => left.CompareTo(right) >= 0;

    public static Slot Parse(string text)
    {
        if (TryParse(text, out var slot))
            return slot;
        throw new FormatException($"Định dạng buổi không hợp lệ: '{text}'. Kỳ vọng dạng 'S 11/9/2025' hoặc 'C 18/9/2025'.");
    }

    public static bool TryParse(string? text, out Slot slot)
    {
        slot = default;
        if (string.IsNullOrWhiteSpace(text)) return false;

        var parts = text.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2) return false;

        if (!Enum.TryParse<Half>(parts[0], ignoreCase: true, out var half))
            return false;

        string[] formats = ["d/M/yyyy", "dd/MM/yyyy", "d-M-yyyy", "dd-MM-yyyy", "yyyy-MM-dd"];
        if (!DateOnly.TryParseExact(parts[1], formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) &&
            !DateOnly.TryParse(parts[1], CultureInfo.InvariantCulture, out date))
        {
            return false;
        }

        slot = new Slot(date, half);
        return true;
    }
}
