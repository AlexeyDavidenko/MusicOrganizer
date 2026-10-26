using FluentAssertions;
using MusicOrganizer.Domain;
using MusicOrganizer.Domain.TagRecovery;

namespace MusicOrganizer.Tests.Unit.Domain;

public class CollectionStatisticsTagRecoveryTests
{
    [Fact]
    public void Build_ComputesConsensus_WhenAtLeastTwoSiblingsAgreeOnAValue()
    {
        var folder = Path.Combine("root", "folder");
        var tagsByPath = new Dictionary<string, AudioTags>
        {
            [Path.Combine(folder, "a.mp3")] = EmptyTags() with { Artist = "Radiohead" },
            [Path.Combine(folder, "b.mp3")] = EmptyTags() with { Artist = "Radiohead" },
            [Path.Combine(folder, "c.mp3")] = EmptyTags(),
        };

        var context = CollectionStatisticsTagRecovery.Build(tagsByPath);

        context.FolderStatistics[folder].Artist.Should().Be("Radiohead");
    }

    [Fact]
    public void Build_NoConsensus_WhenSiblingsDisagree()
    {
        var folder = Path.Combine("root", "folder");
        var tagsByPath = new Dictionary<string, AudioTags>
        {
            [Path.Combine(folder, "a.mp3")] = EmptyTags() with { Artist = "Radiohead" },
            [Path.Combine(folder, "b.mp3")] = EmptyTags() with { Artist = "Pink Floyd" },
        };

        var context = CollectionStatisticsTagRecovery.Build(tagsByPath);

        context.FolderStatistics[folder].Artist.Should().BeNull();
    }

    [Fact]
    public void Build_NoConsensus_WhenOnlyOneFileHasAValue()
    {
        var folder = Path.Combine("root", "folder");
        var tagsByPath = new Dictionary<string, AudioTags>
        {
            [Path.Combine(folder, "a.mp3")] = EmptyTags() with { Artist = "Radiohead" },
            [Path.Combine(folder, "b.mp3")] = EmptyTags(),
        };

        var context = CollectionStatisticsTagRecovery.Build(tagsByPath);

        context.FolderStatistics[folder].Artist.Should().BeNull();
    }

    [Fact]
    public void Build_ComputesYearAndGenreConsensus_ButNeverTrackNumber()
    {
        var folder = Path.Combine("root", "folder");
        var tagsByPath = new Dictionary<string, AudioTags>
        {
            [Path.Combine(folder, "a.mp3")] = EmptyTags() with { Year = 1997, Genre = "Alternative Rock", TrackNumber = 1 },
            [Path.Combine(folder, "b.mp3")] = EmptyTags() with { Year = 1997, Genre = "Alternative Rock", TrackNumber = 1 },
            [Path.Combine(folder, "c.mp3")] = EmptyTags(),
        };

        var context = CollectionStatisticsTagRecovery.Build(tagsByPath);

        context.FolderStatistics[folder].Year.Should().Be(1997);
        context.FolderStatistics[folder].Genre.Should().Be("Alternative Rock");
        CollectionStatisticsTagRecovery.Propose(EmptyTags(), context.FolderStatistics[folder])
            .MergedTags.TrackNumber.Should().BeNull();
    }

    [Fact]
    public void Build_NoYearConsensus_WhenSiblingsDisagree()
    {
        var folder = Path.Combine("root", "folder");
        var tagsByPath = new Dictionary<string, AudioTags>
        {
            [Path.Combine(folder, "a.mp3")] = EmptyTags() with { Year = 1997 },
            [Path.Combine(folder, "b.mp3")] = EmptyTags() with { Year = 2000 },
        };

        var context = CollectionStatisticsTagRecovery.Build(tagsByPath);

        context.FolderStatistics[folder].Year.Should().BeNull();
    }

    [Fact]
    public void Propose_FillsArtistAlbumYearAndGenre_FromConsensusStatistics()
    {
        var current = EmptyTags();
        var statistics = new FolderTagStatistics(Artist: "Radiohead", Album: "OK Computer", Year: 1997, Genre: "Alternative Rock");

        var proposal = CollectionStatisticsTagRecovery.Propose(current, statistics);

        proposal.RecoveredFields.Should().BeEquivalentTo(["Artist", "Album", "Year", "Genre"]);
        proposal.MergedTags.Artist.Should().Be("Radiohead");
        proposal.MergedTags.Album.Should().Be("OK Computer");
        proposal.MergedTags.Year.Should().Be(1997);
        proposal.MergedTags.Genre.Should().Be("Alternative Rock");
    }

    [Fact]
    public void Propose_NeverOverwritesExistingFields()
    {
        var current = new AudioTags(Title: null, Artist: "Existing Artist", Album: "Existing Album", Year: 2001, TrackNumber: null, Genre: "Existing Genre");
        var statistics = new FolderTagStatistics(Artist: "Radiohead", Album: "OK Computer", Year: 1997, Genre: "Alternative Rock");

        var proposal = CollectionStatisticsTagRecovery.Propose(current, statistics);

        proposal.RecoveredFields.Should().BeEmpty();
        proposal.MergedTags.Artist.Should().Be("Existing Artist");
        proposal.MergedTags.Album.Should().Be("Existing Album");
        proposal.MergedTags.Year.Should().Be(2001);
        proposal.MergedTags.Genre.Should().Be("Existing Genre");
    }

    [Fact]
    public void Propose_RecoversNothing_WhenStatisticsIsNull()
    {
        var current = EmptyTags();

        var proposal = CollectionStatisticsTagRecovery.Propose(current, statistics: null);

        proposal.RecoveredFields.Should().BeEmpty();
        proposal.MergedTags.Should().Be(current);
    }

    private static AudioTags EmptyTags() => new(Title: null, Artist: null, Album: null, Year: null, TrackNumber: null, Genre: null);
}
