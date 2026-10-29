using KeHoachHocTap.Web.Data;

namespace KeHoachHocTap.Web.Services;

/// <summary>
/// Kết quả đọc file Excel khung chương trình đào tạo.
/// </summary>
public record CurriculumReadResult(
    IReadOnlyList<Subject> Subjects,
    IReadOnlyList<string> Warnings);

/// <summary>
/// Giao diện đọc file Excel khung chương trình đào tạo.
/// </summary>
public interface ICurriculumReader
{
    CurriculumReadResult Read(Stream stream);
}

