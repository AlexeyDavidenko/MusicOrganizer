using FluentAssertions;
using MusicOrganizer.Domain.Renaming;

namespace MusicOrganizer.Tests.Unit.Domain;

public class FileNameSanitizerTests
{
    [Fact]
    public void SanitizeComponent_ReplacesInternalWhitespaceRuns_WithASingleUnderscore()
    {
        var result = FileNameSanitizer.SanitizeComponent("Maxim   Fadeev");

        result.Should().Be("Maxim_Fadeev");
    }

    [Fact]
    public void SanitizeComponent_PreservesNonLatinScripts_WithoutTransliteration()
    {
        var result = FileNameSanitizer.SanitizeComponent("Максим Фадеев");

        result.Should().Be("Максим_Фадеев");
    }

    [Theory]
    [InlineData("Artist/Title", "ArtistTitle")]
    [InlineData("Artist\\Title", "ArtistTitle")]
    [InlineData("../../etc/passwd", "etcpasswd")]
    [InlineData("Artist:Title", "ArtistTitle")]
    [InlineData("Artist*?\"<>|Title", "ArtistTitle")]
    public void SanitizeComponent_StripsForbiddenAndPathCharacters(string input, string expected)
    {
        var result = FileNameSanitizer.SanitizeComponent(input);

        result.Should().Be(expected);
        result.Should().NotContain("/").And.NotContain("\\");
    }

    [Fact]
    public void SanitizeComponent_CollapsesRepeatedSeparators()
    {
        var result = FileNameSanitizer.SanitizeComponent("Artist---Name");

        result.Should().Be("Artist-Name");
    }

    [Fact]
    public void SanitizeComponent_TruncatesExcessivelyLongInput()
    {
        var longValue = new string('a', 500);

        var result = FileNameSanitizer.SanitizeComponent(longValue);

        result.Length.Should().BeLessThanOrEqualTo(100);
    }
}
