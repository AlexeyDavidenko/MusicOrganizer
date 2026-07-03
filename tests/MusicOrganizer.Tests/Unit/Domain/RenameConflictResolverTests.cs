using FluentAssertions;
using MusicOrganizer.Domain.Renaming;

namespace MusicOrganizer.Tests.Unit.Domain;

public class RenameConflictResolverTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void NextCandidate_InsertsAttemptNumber_BeforeTheExtension(int attempt)
    {
        var path = Path.Combine("music", "Artist-Title.mp3");
        var expected = Path.Combine("music", $"Artist-Title ({attempt}).mp3");

        var result = RenameConflictResolver.NextCandidate(path, attempt);

        result.Should().Be(expected);
    }
}
