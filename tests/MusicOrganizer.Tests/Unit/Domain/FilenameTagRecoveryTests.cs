using FluentAssertions;
using MusicOrganizer.Domain;
using MusicOrganizer.Domain.TagRecovery;

namespace MusicOrganizer.Tests.Unit.Domain;

public class FilenameTagRecoveryTests
{
    [Theory]
    [InlineData("Maxim Fadeev - Leti Za Mnoy")]
    [InlineData("Maxim Fadeev-Leti Za Mnoy")]
    public void Propose_RecoversArtistAndTitle_FromRecognizedFileName(string fileName)
    {
        var current = new AudioTags(Title: null, Artist: null, Album: null, Year: null, TrackNumber: null, Genre: null);

        var proposal = FilenameTagRecovery.Propose(fileName, current);

        proposal.RecoveredFields.Should().BeEquivalentTo(["Artist", "Title"]);
        proposal.MergedTags.Artist.Should().Be("Maxim Fadeev");
        proposal.MergedTags.Title.Should().Be("Leti Za Mnoy");
    }

    [Theory]
    [InlineData("01 Track")]
    [InlineData("01 - 02")]
    [InlineData("Artist - Title - Remix")]
    [InlineData("NoSeparatorAtAll")]
    public void Propose_RecoversNothing_WhenFileNameIsAmbiguous(string fileName)
    {
        var current = new AudioTags(Title: null, Artist: null, Album: null, Year: null, TrackNumber: null, Genre: null);

        var proposal = FilenameTagRecovery.Propose(fileName, current);

        proposal.RecoveredFields.Should().BeEmpty();
        proposal.MergedTags.Should().Be(current);
    }

    [Fact]
    public void Propose_NeverOverwritesAnExistingField_EvenIfFileNameDisagrees()
    {
        var current = new AudioTags(Title: "Existing Title", Artist: null, Album: null, Year: null, TrackNumber: null, Genre: null);

        var proposal = FilenameTagRecovery.Propose("Some Artist - Different Title", current);

        proposal.RecoveredFields.Should().BeEquivalentTo(["Artist"]);
        proposal.MergedTags.Title.Should().Be("Existing Title");
        proposal.MergedTags.Artist.Should().Be("Some Artist");
    }

    [Fact]
    public void Propose_RecoversNothing_WhenBothFieldsAlreadyPresent()
    {
        var current = new AudioTags(Title: "Title", Artist: "Artist", Album: null, Year: null, TrackNumber: null, Genre: null);

        var proposal = FilenameTagRecovery.Propose("Other Artist - Other Title", current);

        proposal.RecoveredFields.Should().BeEmpty();
    }
}
