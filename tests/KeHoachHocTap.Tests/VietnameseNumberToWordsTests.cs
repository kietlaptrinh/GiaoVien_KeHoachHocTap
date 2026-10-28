using KeHoachHocTap.Web.Services;
using Xunit;

namespace KeHoachHocTap.Tests;

public class VietnameseNumberToWordsTests
{
    private readonly VietnameseNumberToWordsConverter _converter = new();

    [Theory]
    [InlineData(7.81, "Bảy phẩy tám mươi mốt")]
    [InlineData(7.26, "Bảy phẩy hai mươi sáu")]
    [InlineData(7.35, "Bảy phẩy ba mươi lăm")]
    [InlineData(7.28, "Bảy phẩy hai mươi tám")]
    [InlineData(7.11, "Bảy phẩy mười một")]
    [InlineData(7.84, "Bảy phẩy tám mươi bốn")]
    [InlineData(7.60, "Bảy phẩy sáu")]
    [InlineData(7.68, "Bảy phẩy sáu mươi tám")]
    [InlineData(7.38, "Bảy phẩy ba mươi tám")]
    [InlineData(8.34, "Tám phẩy ba mươi bốn")]
    [InlineData(7.50, "Bảy phẩy năm")]
    [InlineData(8.23, "Tám phẩy hai mươi ba")]
    [InlineData(8.00, "Tám phẩy không")]
    [InlineData(10.00, "Mười phẩy không")]
    [InlineData(0.00, "Không phẩy không")]
    [InlineData(7.05, "Bảy phẩy không năm")]
    [InlineData(6.15, "Sáu phẩy mười lăm")]
    [InlineData(9.21, "Chín phẩy hai mươi mốt")]
    [InlineData(7.00, "Bảy phẩy không")]
    public void ConvertScoreToWords_StandardK56Scores_ReturnsCorrectVietnameseWords(double scoreDouble, string expectedWords)
    {
        var score = (decimal)scoreDouble;
        var result = _converter.ConvertScoreToWords(score);
        Assert.Equal(expectedWords, result);
    }

    [Fact]
    public void ConvertScoreToWords_Null_ReturnsEmDash()
    {
        var result = _converter.ConvertScoreToWords(null);
        Assert.Equal("—", result);
    }

    [Fact]
    public void ConvertScoreToWords_FullTwoDigitsOption_ReturnsExpandedTens()
    {
        Assert.Equal("Bảy phẩy sáu mươi", _converter.ConvertScoreToWords(7.60m, fullTwoDigits: true));
        Assert.Equal("Bảy phẩy năm mươi", _converter.ConvertScoreToWords(7.50m, fullTwoDigits: true));
        Assert.Equal("Tám phẩy mười", _converter.ConvertScoreToWords(8.10m, fullTwoDigits: true));
    }

    [Theory]
    [InlineData(-1.0)]
    [InlineData(10.5)]
    [InlineData(100.0)]
    public void ConvertScoreToWords_OutOfRange_ReturnsInvalid(double invalidScore)
    {
        var result = _converter.ConvertScoreToWords((decimal)invalidScore);
        Assert.Equal("Không hợp lệ", result);
    }
}
