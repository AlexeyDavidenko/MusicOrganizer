using FluentAssertions;
using MusicOrganizer.Domain.Renaming;

namespace MusicOrganizer.Tests.Unit.Domain;

public class CyrillicTransliteratorTests
{
    [Theory]
    [InlineData("Максим Фадеев", "Maksim Fadeyev")]
    [InlineData("Лети За Мной", "Leti Za Mnoy")]
    [InlineData("Щука", "Shchuka")]
    [InlineData("Объект", "Obyekt")]
    [InlineData("Вьюга", "Vyuga")]
    public void Transliterate_ProducesKnownCorrectBgnPcgnRomanization(string cyrillic, string expected)
    {
        CyrillicTransliterator.Transliterate(cyrillic).Should().Be(expected);
    }

    [Theory]
    [InlineData("Artist - Title")]
    [InlineData("Hello, World! 123")]
    public void Transliterate_LeavesNonCyrillicTextUnchanged(string value)
    {
        CyrillicTransliterator.Transliterate(value).Should().Be(value);
    }

    [Fact]
    public void Transliterate_OnlyTransliteratesTheCyrillicPortion_OfMixedText()
    {
        CyrillicTransliterator.Transliterate("DJ Максим - Remix").Should().Be("DJ Maksim - Remix");
    }

    [Fact]
    public void Transliterate_ReturnsEmptyString_ForEmptyInput()
    {
        CyrillicTransliterator.Transliterate(string.Empty).Should().BeEmpty();
    }
}
