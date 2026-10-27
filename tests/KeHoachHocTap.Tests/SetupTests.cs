using ClosedXML.Excel;
using DocumentFormat.OpenXml.Packaging;

namespace KeHoachHocTap.Tests;

public class SetupTests
{
    [Fact]
    public void TestData_Files_ExistInOutputDirectory()
    {
        var excelPath = Path.Combine(AppContext.BaseDirectory, "TestData", "ke_hoach_hoc_tap.xlsx");
        var docxPath = Path.Combine(AppContext.BaseDirectory, "TestData", "BieuMau_KeHoachGiangDay.docx");

        Assert.True(File.Exists(excelPath), $"Excel test file not found at {excelPath}");
        Assert.True(File.Exists(docxPath), $"Word test template not found at {docxPath}");
    }

    [Fact]
    public void TestData_Excel_CanBeOpenedWithClosedXml()
    {
        var excelPath = Path.Combine(AppContext.BaseDirectory, "TestData", "ke_hoach_hoc_tap.xlsx");
        using var workbook = new XLWorkbook(excelPath);

        Assert.NotNull(workbook.Worksheet("Khung CT"));
        Assert.NotNull(workbook.Worksheet("Ngay nghi"));
    }

    [Fact]
    public void TestData_Word_CanBeOpenedWithOpenXml()
    {
        var docxPath = Path.Combine(AppContext.BaseDirectory, "TestData", "BieuMau_KeHoachGiangDay.docx");
        using var doc = WordprocessingDocument.Open(docxPath, false);

        Assert.NotNull(doc.MainDocumentPart);
        Assert.NotNull(doc.MainDocumentPart?.Document);
        Assert.NotNull(doc.MainDocumentPart?.Document.Body);
    }
}
