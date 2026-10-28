namespace KeHoachHocTap.Web.Services;

/// <summary>
/// Dịch vụ chuyển đổi điểm số sang chữ tiếng Việt chuẩn quy chế đào tạo, văn bằng, chứng chỉ và học bạ.
/// </summary>
public class VietnameseNumberToWordsConverter : IVietnameseNumberToWordsConverter
{
    private static readonly string[] SingleDigitWords =
    [
        "không", "một", "hai", "ba", "bốn", "năm", "sáu", "bảy", "tám", "chín"
    ];

    private static readonly string[] WholeNumberWords =
    [
        "Không", "Một", "Hai", "Ba", "Bốn", "Năm", "Sáu", "Bảy", "Tám", "Chín", "Mười"
    ];

    public string ConvertScoreToWords(decimal? score, bool fullTwoDigits = false)
    {
        if (!score.HasValue)
        {
            return "—";
        }

        var val = score.Value;
        if (val < 0 || val > 10)
        {
            return "Không hợp lệ";
        }

        var rounded = Math.Round(val, 2, MidpointRounding.AwayFromZero);
        var whole = (int)Math.Truncate(rounded);
        var frac = (int)Math.Round((rounded - whole) * 100m, MidpointRounding.AwayFromZero);

        // Trường hợp làm tròn lên 100 (vd: 9.996 -> 10.00)
        if (frac >= 100)
        {
            whole += 1;
            frac = 0;
        }

        var wholeWord = whole >= 0 && whole <= 10 ? WholeNumberWords[whole] : whole.ToString();

        // Không có phần thập phân hoặc phần thập phân bằng 0: ví dụ 8.00 -> "Tám phẩy không"
        if (frac == 0)
        {
            return $"{wholeWord} phẩy không";
        }

        string fracWord;

        // Trường hợp tròn chục: ví dụ 7.50 -> 50, 7.60 -> 60
        if (frac % 10 == 0)
        {
            if (fullTwoDigits)
            {
                var tens = frac / 10;
                fracWord = tens == 1 ? "mười" : $"{SingleDigitWords[tens]} mươi";
            }
            else
            {
                var single = frac / 10;
                fracWord = SingleDigitWords[single];
            }

            return $"{wholeWord} phẩy {fracWord}";
        }

        // Trường hợp phần thập phân có 2 chữ số (1..99, không chia hết cho 10)
        if (frac < 10)
        {
            // 01..09: ví dụ 7.05 -> "không năm"
            fracWord = $"không {SingleDigitWords[frac]}";
        }
        else if (frac < 20)
        {
            // 11..19: ví dụ 7.11 -> "mười một", 7.15 -> "mười lăm"
            var units = frac % 10;
            var unitWord = units switch
            {
                5 => "lăm",
                _ => SingleDigitWords[units]
            };
            fracWord = $"mười {unitWord}";
        }
        else
        {
            // 21..99 (trừ tròn chục)
            var tens = frac / 10;
            var units = frac % 10;

            var tensWord = $"{SingleDigitWords[tens]} mươi";
            var unitWord = units switch
            {
                1 => "mốt",
                4 => "bốn",
                5 => "lăm",
                _ => SingleDigitWords[units]
            };

            fracWord = $"{tensWord} {unitWord}";
        }

        return $"{wholeWord} phẩy {fracWord}";
    }
}
