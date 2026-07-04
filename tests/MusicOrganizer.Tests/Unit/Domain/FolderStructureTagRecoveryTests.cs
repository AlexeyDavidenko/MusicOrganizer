using FluentAssertions;
using MusicOrganizer.Domain;
using MusicOrganizer.Domain.TagRecovery;

namespace MusicOrganizer.Tests.Unit.Domain;

public class FolderStructureTagRecoveryTests
{
    [Fact]
    public void Propose_RecoversArtistAndAlbum_WhenFileIsNestedTwoLevelsUnderRoot()
    {
        var root = Path.Combine("root");
        var filePath = Path.Combine(root, "Some Artist", "Some Album", "track.mp3");
        var current = EmptyTags();

        var proposal = FolderStructureTagRecovery.Propose(filePath, root, current);

        proposal.RecoveredFields.Should().BeEquivalentTo(["Album", "Artist"]);
        proposal.MergedTags.Album.Should().Be("Some Album");
        proposal.MergedTags.Artist.Should().Be("Some Artist");
    }

    [Fact]
    public void Propose_RecoversArtistOnly_WhenFileIsExactlyOneLevelUnderRoot()
    {
        var root = Path.Combine("root");
        var filePath = Path.Combine(root, "Some Artist", "track.mp3");
        var current = EmptyTags();

        var proposal = FolderStructureTagRecovery.Propose(filePath, root, current);

        proposal.RecoveredFields.Should().BeEquivalentTo(["Artist"]);
        proposal.MergedTags.Artist.Should().Be("Some Artist");
        proposal.MergedTags.Album.Should().BeNull();
    }

    [Fact]
    public void Propose_RecoversNothing_WhenFileIsDirectlyUnderRoot()
    {
        var root = Path.Combine("root");
        var filePath = Path.Combine(root, "track.mp3");
        var current = EmptyTags();

        var proposal = FolderStructureTagRecovery.Propose(filePath, root, current);

        proposal.RecoveredFields.Should().BeEmpty();
        proposal.MergedTags.Should().Be(current);
    }

    [Fact]
    public void Propose_NeverOverwritesExistingArtistOrAlbum()
    {
        var root = Path.Combine("root");
        var filePath = Path.Combine(root, "Folder Artist", "Folder Album", "track.mp3");
        var current = new AudioTags(Title: null, Artist: "Existing Artist", Album: "Existing Album", Year: null, TrackNumber: null, Genre: null);

        var proposal = FolderStructureTagRecovery.Propose(filePath, root, current);

        proposal.RecoveredFields.Should().BeEmpty();
        proposal.MergedTags.Artist.Should().Be("Existing Artist");
        proposal.MergedTags.Album.Should().Be("Existing Album");
    }

    private static AudioTags EmptyTags() => new(Title: null, Artist: null, Album: null, Year: null, TrackNumber: null, Genre: null);
}
