using FluentAssertions;
using MusicOrganizer.Domain.Renaming;

namespace MusicOrganizer.Tests.Unit.Domain;

public class DefaultRenameTemplateTests
{
    [Fact]
    public void BuildFileName_JoinsSanitizedArtistAndTitle_WithASingleDash()
    {
        var result = DefaultRenameTemplate.BuildFileName("Максим Фадеев", "Лети За Мной");

        result.Should().Be("Максим_Фадеев-Лети_За_Мной.mp3");
    }
}
