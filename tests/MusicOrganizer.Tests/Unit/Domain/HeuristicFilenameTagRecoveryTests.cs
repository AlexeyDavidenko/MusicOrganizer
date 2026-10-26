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

        proposal.RecoveredFields.Should().BeEquivalentTo(["Artist", "Title", "TrackNumber"]);
        proposal.MergedTags.Artist.Should().Be("Queen");
        proposal.MergedTags.Title.Should().Be("Bohemian Rhapsody");
        proposal.MergedTags.TrackNumber.Should().Be(1);
    }

    [Theory]
    [InlineData("01 - Track", 1)]
    [InlineData("07. Track", 7)]
    [InlineData("12_Track", 12)]
    [InlineData("3-Track", 3)]
    public void Propose_RecoversTrackNumber_EvenWhenArtistAndTitleStayAmbiguous(string fileName, int expected)
    {
        var current = EmptyTags();

        var proposal = HeuristicFilenameTagRecovery.Propose(fileName, current);

        proposal.RecoveredFields.Should().BeEquivalentTo(["TrackNumber"]);
        proposal.MergedTags.TrackNumber.Should().Be(expected);
    }

    [Theory]
    [InlineData("50 Cent - In Da Club")]
    [InlineData("01 Track")]
    [InlineData("00 - Intro")]
    [InlineData("1999 - Song")]
    [InlineData("01 - ")]
    [InlineData("01 - 02")]
    public void Propose_RecoversNoTrackNumber_WithoutAConfidentDelimitedPrefix(string fileName)
    {
        var current = EmptyTags();

        var proposal = HeuristicFilenameTagRecovery.Propose(fileName, current);

        proposal.RecoveredFields.Should().NotContain("TrackNumber");
        proposal.MergedTags.TrackNumber.Should().BeNull();
    }

    [Fact]
    public void Propose_NeverOverwritesAnExistingTrackNumber()
    {
        var current = EmptyTags() with { TrackNumber = 9 };

        var proposal = HeuristicFilenameTagRecovery.Propose("01 - Track", current);

        proposal.RecoveredFields.Should().BeEmpty();
        proposal.MergedTags.TrackNumber.Should().Be(9);
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

        proposal.RecoveredFields.Should().BeEquivalentTo(["Artist", "TrackNumber"]);
        proposal.MergedTags.Title.Should().Be("Existing Title");
        proposal.MergedTags.Artist.Should().Be("Some Artist");
    }

    private static AudioTags EmptyTags() => new(Title: null, Artist: null, Album: null, Year: null, TrackNumber: null, Genre: null);
}
