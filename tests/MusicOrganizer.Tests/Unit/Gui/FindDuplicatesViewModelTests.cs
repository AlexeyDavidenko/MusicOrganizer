using FluentAssertions;
using MusicOrganizer.Application.Deduplication;
using MusicOrganizer.Domain.Deduplication;
using MusicOrganizer.Gui.ViewModels;

namespace MusicOrganizer.Tests.Unit.Gui;

public class FindDuplicatesViewModelTests
{
    [Fact]
    public async Task Run_SplitsGroupsByKind()
    {
        var exact = new DuplicateGroup(DuplicateMatchKind.Exact, "hash", ["a.mp3", "b.mp3"], 100);
        var tagMatch = new DuplicateGroup(DuplicateMatchKind.TagMatch, "artisttitle", ["c.mp3", "d.mp3"], null);
        var sut = new FindDuplicatesViewModel(new FakeDuplicateFinder([exact, tagMatch]));

        await sut.RunCommand.ExecuteAsync(null);

        sut.ExactGroups.Should().ContainSingle().Which.Should().Be(exact);
        sut.TagMatchGroups.Should().ContainSingle().Which.Should().Be(tagMatch);
    }

    [Fact]
    public async Task Run_SumsWastedBytesAcrossExactGroupsOnly()
    {
        // 3 files @ 100 => 2 redundant copies => 200 wasted; 2 files @ 50 => 50 wasted.
        var exactA = new DuplicateGroup(DuplicateMatchKind.Exact, "h1", ["a", "b", "c"], 100);
        var exactB = new DuplicateGroup(DuplicateMatchKind.Exact, "h2", ["d", "e"], 50);
        var tagMatch = new DuplicateGroup(DuplicateMatchKind.TagMatch, "k", ["f", "g"], null);
        var sut = new FindDuplicatesViewModel(new FakeDuplicateFinder([exactA, exactB, tagMatch]));

        await sut.RunCommand.ExecuteAsync(null);

        sut.WastedBytes.Should().Be(250);
    }

    [Fact]
    public async Task Run_ReportsGroupCountsInStatus()
    {
        var exact = new DuplicateGroup(DuplicateMatchKind.Exact, "h", ["a", "b"], 10);
        var sut = new FindDuplicatesViewModel(new FakeDuplicateFinder([exact]));

        await sut.RunCommand.ExecuteAsync(null);

        sut.StatusMessage.Should().Be("Found 1 exact group(s) and 0 tag-match group(s).");
    }

    [Fact]
    public async Task Run_ClearsPreviousResults()
    {
        var finder = new FakeDuplicateFinder([new DuplicateGroup(DuplicateMatchKind.Exact, "h", ["a", "b"], 10)]);
        var sut = new FindDuplicatesViewModel(finder);
        await sut.RunCommand.ExecuteAsync(null);

        finder.Groups = [];
        await sut.RunCommand.ExecuteAsync(null);

        sut.ExactGroups.Should().BeEmpty();
        sut.WastedBytes.Should().Be(0);
    }

    private sealed class FakeDuplicateFinder(IReadOnlyList<DuplicateGroup> groups) : IDuplicateFinder
    {
        public IReadOnlyList<DuplicateGroup> Groups { get; set; } = groups;

        public Task<IReadOnlyList<DuplicateGroup>> FindAsync(string rootPath, CancellationToken cancellationToken = default) =>
            Task.FromResult(Groups);
    }
}
