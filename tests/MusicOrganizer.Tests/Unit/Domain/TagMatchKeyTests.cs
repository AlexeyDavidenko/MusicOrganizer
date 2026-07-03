using FluentAssertions;
using MusicOrganizer.Domain.Deduplication;

namespace MusicOrganizer.Tests.Unit.Domain;

public class TagMatchKeyTests
{
    [Fact]
    public void Build_IsCaseAndWhitespaceInsensitive()
    {
        var key1 = TagMatchKey.Build("Artist", "Title");
        var key2 = TagMatchKey.Build(" artist ", " TITLE ");

        key1.Should().Be(key2);
    }

    [Fact]
    public void Build_DiffersForDifferentArtistOrTitle()
    {
        var key1 = TagMatchKey.Build("Artist", "Title");
        var key2 = TagMatchKey.Build("Other Artist", "Title");

        key1.Should().NotBe(key2);
    }
}
