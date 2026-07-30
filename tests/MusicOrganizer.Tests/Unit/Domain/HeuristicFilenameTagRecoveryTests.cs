using FluentAssertions;
using MusicOrganizer.Domain;
using MusicOrganizer.Domain.TagRecovery;

namespace MusicOrganizer.Tests.Unit.Domain;

public class HeuristicFilenameTagRecoveryTests
{
    [Theory]
    [InlineData("01 - Queen - Bohemian Rhapsody")]
    [InlineData("01. Queen - Bohemian Rhapsody")]
    [InlineData("01_Queen - Bohemian Rhapsody")]
    public void Propose_RecoversArtistAndTitle_AfterStrippingATrackNumberPrefix(string fileName)
    {
        var current = EmptyTags();

        var proposal = HeuristicFilenameTagRecovery.Propose(fileName, current);

        proposal.RecoveredFields.Should().BeEquivalentTo(["Artist", "Title"]);
        proposal.MergedTags.Artist.Should().Be("Queen");
        proposal.MergedTags.Title.Should().Be("Bohemian Rhapsody");
    }

    [Theory]
    [InlineData("Queen - Bohemian Rhapsody (Live)")]
    [InlineData("Queen - Bohemian Rhapsody (Live) [Remastered]")]
    [InlineData("Queen - Bohemian Rhapsody [Remastered]")]
    public void Propose_RecoversArtistAndTitle_AfterStrippingTrailingNoiseSuffixes(string fileName)
    {
        var current = EmptyTags();

        var proposal = HeuristicFilenameTagRecovery.Propose(fileName, current);

        proposal.RecoveredFields.Should().BeEquivalentTo(["Artist", "Title"]);
        proposal.MergedTags.Artist.Should().Be("Queen");
        proposal.MergedTags.Title.Should().Be("Bohemian Rhapsody");
    }

    [Fact]
    public void Propose_ConvertsUnderscoresToSpaces_ReversingTheProjectsOwnRenameConvention()
    {
        var current = EmptyTags();

        var proposal = HeuristicFilenameTagRecovery.Propose("Maxim_Fadeev-Leti_Za_Mnoy", current);

        proposal.MergedTags.Artist.Should().Be("Maxim Fadeev");
        proposal.MergedTags.Title.Should().Be("Leti Za Mnoy");
    }

    [Theory]
    [InlineData("01 Track")]
    [InlineData("01 - 02")]
    [InlineData("Artist - Title - Remix")]
    [InlineData("NoSeparatorAtAll")]
    [InlineData("Queen_Bohemian_Rhapsody")]
    public void Propose_RecoversNothing_WhenStillAmbiguousAfterCleanup(string fileName)
    {
        var current = EmptyTags();

        var proposal = HeuristicFilenameTagRecovery.Propose(fileName, current);

        proposal.RecoveredFields.Should().BeEmpty();
        proposal.MergedTags.Should().Be(current);
    }

    [Fact]
    public void Propose_NeverOverwritesAnExistingField()
    {
        var current = new AudioTags(Title: "Existing Title", Artist: null, Album: null, Year: null, TrackNumber: null, Genre: null);

        var proposal = HeuristicFilenameTagRecovery.Propose("01 - Some Artist - Different Title (Live)", current);

        proposal.RecoveredFields.Should().BeEquivalentTo(["Artist"]);
        proposal.MergedTags.Title.Should().Be("Existing Title");
        proposal.MergedTags.Artist.Should().Be("Some Artist");
    }

    private static AudioTags EmptyTags() => new(Title: null, Artist: null, Album: null, Year: null, TrackNumber: null, Genre: null);
}
