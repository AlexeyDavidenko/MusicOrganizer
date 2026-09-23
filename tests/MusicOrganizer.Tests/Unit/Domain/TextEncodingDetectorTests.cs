using System.Text;
using FluentAssertions;
using MusicOrganizer.Domain.Encoding;

namespace MusicOrganizer.Tests.Unit.Domain;

public class TextEncodingDetectorTests
{
    static TextEncodingDetectorTests()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    [Theory]
    [InlineData("Максим Фадеев")]
    [InlineData("Лети За Мной")]
    [InlineData("Виктор Цой")]
    [InlineData("Группа крови")]
    [InlineData("Щука")]
    public void Detect_FixesWindows1251MisreadAsLatin1(string real)
    {
        var mojibake = Mojibake(real, 1251);

        var result = TextEncodingDetector.Detect(mojibake);

        result.CorrectedText.Should().Be(real);
        result.HadNonAsciiBytes.Should().BeTrue();
    }

    [Theory]
    [InlineData("Максим Фадеев")]
    [InlineData("Кино")]
    [InlineData("Аквариум")]
    [InlineData("Ленинград")]
    public void Detect_FixesCp866MisreadAsLatin1(string real)
    {
        var mojibake = Mojibake(real, 866);

        var result = TextEncodingDetector.Detect(mojibake);

        result.CorrectedText.Should().Be(real);
        result.HadNonAsciiBytes.Should().BeTrue();
    }

    [Theory]
    [InlineData("Максим Фадеев")]
    [InlineData("Лети За Мной")]
    [InlineData("Сплин")]
    public void Detect_FixesUtf8MisreadAsLatin1(string real)
    {
        var mojibake = Encoding.Latin1.GetString(Encoding.UTF8.GetBytes(real));

        var result = TextEncodingDetector.Detect(mojibake);

        result.CorrectedText.Should().Be(real);
        result.HadNonAsciiBytes.Should().BeTrue();
    }

    [Fact]
    public void Detect_FixesWindows1252PunctuationMisreadAsLatin1_WhenRestOfTextIsAscii()
    {
        var real = "Guns N’ Roses";
        var mojibake = Mojibake(real, 1252);

        var result = TextEncodingDetector.Detect(mojibake);

        result.CorrectedText.Should().Be(real);
    }

    [Fact]
    public void Detect_DoesNotChangeGenuineWesternLatin1Text()
    {
        var result = TextEncodingDetector.Detect("Mötley Crüe");

        result.CorrectedText.Should().BeNull();
        result.HadNonAsciiBytes.Should().BeTrue();
    }

    [Fact]
    public void Detect_DoesNotChangePureAsciiText()
    {
        var result = TextEncodingDetector.Detect("Pink Floyd");

        result.CorrectedText.Should().BeNull();
        result.HadNonAsciiBytes.Should().BeFalse();
    }

    [Fact]
    public void Detect_LeavesAmbiguousShortTextUnchanged_RatherThanGuessing()
    {
        // Contains one C1-range byte (like the Windows-1252 case) but also other non-ASCII
        // Cyrillic-range bytes that rule out the narrow Windows-1252 fallback, and is too short
        // for the Russian letter-frequency scorer to confidently pick 1251 vs 866.
        var mojibake = Mojibake("Ддт", 866);

        var result = TextEncodingDetector.Detect(mojibake);

        result.CorrectedText.Should().BeNull();
        result.HadNonAsciiBytes.Should().BeTrue();
    }

    [Fact]
    public void Detect_ReturnsNullCorrectionAndNoNonAsciiFlag_ForEmptyString()
    {
        var result = TextEncodingDetector.Detect(string.Empty);

        result.CorrectedText.Should().BeNull();
        result.HadNonAsciiBytes.Should().BeFalse();
    }

    private static string Mojibake(string real, int codePage) =>
        Encoding.Latin1.GetString(Encoding.GetEncoding(codePage).GetBytes(real));
}
