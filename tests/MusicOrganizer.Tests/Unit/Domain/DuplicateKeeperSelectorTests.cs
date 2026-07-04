using FluentAssertions;
using MusicOrganizer.Domain.Deduplication;

namespace MusicOrganizer.Tests.Unit.Domain;

public class DuplicateKeeperSelectorTests
{
    [Fact]
    public void SelectKeeper_ReturnsTheShortestPath()
    {
        var paths = new[]
        {
            Path.Combine("music", "Artist", "Album", "track.mp3"),
            Path.Combine("music", "track.mp3"),
            Path.Combine("music", "backup", "old", "track.mp3"),
        };

        var keeper = DuplicateKeeperSelector.SelectKeeper(paths);

        keeper.Should().Be(paths[1]);
    }

    [Fact]
    public void SelectKeeper_BreaksTiesDeterministically_WhenPathsAreTheSameLength()
    {
        var paths = new[] { "b.mp3", "a.mp3" };

        var keeper = DuplicateKeeperSelector.SelectKeeper(paths);

        keeper.Should().Be("a.mp3");
    }
}
