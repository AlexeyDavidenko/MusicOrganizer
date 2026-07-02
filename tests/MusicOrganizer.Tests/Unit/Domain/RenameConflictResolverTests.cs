using FluentAssertions;
using MusicOrganizer.Domain.Renaming;

namespace MusicOrganizer.Tests.Unit.Domain;

public class RenameConflictResolverTests
{
    [Theory]
    [InlineData(1, "/music/Artist-Title (1).mp3")]
    [InlineData(2, "/music/Artist-Title (2).mp3")]
    public void NextCandidate_InsertsAttemptNumber_BeforeTheExtension(int attempt, string expected)
    {
        var result = RenameConflictResolver.NextCandidate("/music/Artist-Title.mp3", attempt);

        result.Should().Be(expected);
    }
}
