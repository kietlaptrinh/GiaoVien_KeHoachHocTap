using ClosedXML.Excel;
using KeHoachHocTap.Web.Data;

namespace KeHoachHocTap.Web.Services;

/// <summary>
/// Trình đọc file Excel khung chương trình đào tạo.
/// Hỗ trợ đọc các cột: Stt, Học phần / Tên môn, Số tiết, Hệ số, Khối kiến thức.
/// </summary>
public class CurriculumReader : ICurriculumReader
{
    public CurriculumReadResult Read(Stream stream)
    {
        var subjects = new List<Subject>();
        var warnings = new List<string>();

        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheets.FirstOrDefault();

        if (worksheet == null)
        {
            warnings.Add("File Excel không có bất kỳ sheet dữ liệu nào.");
            return new CurriculumReadResult(subjects, warnings);
        }

        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 0;
        if (lastRow < 2)
        {
            warnings.Add("Sheet dữ liệu không có dòng nội dung.");
            return new CurriculumReadResult(subjects, warnings);
        }

        // Tìm dòng tiêu đề
        int headerRow = 1;
        int colStt = 1;
        int colName = 2;
        int colPeriods = 3;
        int colCoeff = 4;
        int colBlock = 5;

        for (int r = 1; r <= Math.Min(10, lastRow); r++)
        {
            for (int c = 1; c <= 10; c++)
            {
                var txt = worksheet.Cell(r, c).GetString().Trim().ToLowerInvariant();
                if (txt.Contains("học phần") || txt.Contains("môn học") || txt.Contains("tên môn"))
                {
                    headerRow = r;
                    colName = c;
                    break;
                }
            }
            if (headerRow > 1 || colName != 2) break;
        }

        // Quét lại vị trí các cột theo dòng tiêu đề
        for (int c = 1; c <= 15; c++)
        {
            var txt = worksheet.Cell(headerRow, c).GetString().Trim().ToLowerInvariant();
            if (txt == "stt" || txt.Contains("thứ tự")) colStt = c;
            else if (txt.Contains("học phần") || txt.Contains("môn học") || txt.Contains("tên môn")) colName = c;
            else if (txt.Contains("số tiết") || txt == "tiết") colPeriods = c;
            else if (txt.Contains("hệ số")) colCoeff = c;
            else if (txt.Contains("khối kiến thức") || txt.Contains("nhóm")) colBlock = c;
        }

        int currentSortOrder = 1;
        for (int r = headerRow + 1; r <= lastRow; r++)
        {
            var name = worksheet.Cell(r, colName).GetString().Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            // Đọc số tiết
            int periods = 30; // fallback mặc định
            var periodCell = worksheet.Cell(r, colPeriods);
            if (periodCell.TryGetValue<int>(out var pVal) && pVal > 0)
            {
                periods = pVal;
            }
            else if (int.TryParse(periodCell.GetString().Trim(), out var pParsed) && pParsed > 0)
            {
                periods = pParsed;
            }
            else
            {
                warnings.Add($"Dòng {r} (môn '{name}'): Không đọc được số tiết hợp lệ, tạm gán {periods} tiết.");
            }

            // Đọc hệ số
            decimal coeff = 1.0m;
            var coeffCell = worksheet.Cell(r, colCoeff);
            if (coeffCell.TryGetValue<decimal>(out var cVal))
            {
                coeff = cVal;
            }
            else if (decimal.TryParse(coeffCell.GetString().Trim(), out var cParsed))
            {
                coeff = cParsed;
            }

            // Đọc khối kiến thức
            var block = worksheet.Cell(r, colBlock).GetString().Trim();
            if (string.IsNullOrWhiteSpace(block)) block = null;

            // Đọc STT nếu có
            int sortOrder = currentSortOrder;
            var sttCell = worksheet.Cell(r, colStt);
            if (sttCell.TryGetValue<int>(out var sVal) && sVal > 0)
            {
                sortOrder = sVal;
            }

            subjects.Add(new Subject
            {
                SortOrder = sortOrder,
                Name = name,
                Periods = periods,
                Coefficient = coeff,
                KnowledgeBlock = block,
                ExamMode = "Immediately"
            });

            currentSortOrder++;
        }

        // Đảm bảo SortOrder liên tục 1..n
        for (int i = 0; i < subjects.Count; i++)
        {
            subjects[i].SortOrder = i + 1;
        }

        return new CurriculumReadResult(subjects, warnings);
    }
}
