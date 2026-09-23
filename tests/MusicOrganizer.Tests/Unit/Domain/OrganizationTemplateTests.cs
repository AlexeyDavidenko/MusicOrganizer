using FluentAssertions;
using MusicOrganizer.Domain.Renaming;

namespace MusicOrganizer.Tests.Unit.Domain;

public class OrganizationTemplateTests
{
    [Fact]
    public void BuildTargetDirectory_CombinesSanitizedArtistAndAlbum_WhenAlbumIsKnown()
    {
        var result = OrganizationTemplate.BuildTargetDirectory("root", "Pink Floyd", "The Wall");

        result.Should().Be(Path.Combine("root", "Pink_Floyd", "The_Wall"));
    }

    [Fact]
    public void BuildTargetDirectory_FallsBackToArtistOnly_WhenAlbumIsMissing()
    {
        var result = OrganizationTemplate.BuildTargetDirectory("root", "Pink Floyd", null);

        result.Should().Be(Path.Combine("root", "Pink_Floyd"));
    }

    [Fact]
    public void BuildTargetDirectory_SanitizesForbiddenCharacters_InBothComponents()
    {
        var result = OrganizationTemplate.BuildTargetDirectory("root", "AC/DC", "Back In Black?");

        result.Should().Be(Path.Combine("root", "ACDC", "Back_In_Black"));
    }
}
