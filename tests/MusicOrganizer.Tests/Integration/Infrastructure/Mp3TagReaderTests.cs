using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using MusicOrganizer.Infrastructure.Tags;
using MusicOrganizer.Tests.TestSupport;

namespace MusicOrganizer.Tests.Integration.Infrastructure;

public class Mp3TagReaderTests : IDisposable
{
    private readonly string _root = Directory.CreateTempSubdirectory("musicorganizer-tags-").FullName;

    [Fact]
    public async Task ReadTagsAsync_ReturnsTagsWrittenByTagLib()
    {
        var path = Path.Combine(_root, "tagged.mp3");
        MinimalMp3Fixture.CreateAt(path);

        using (var file = TagLib.File.Create(path))
        {
            file.Tag.Title = "Test Title";
            file.Tag.Performers = ["Test Artist"];
            file.Tag.Album = "Test Album";
            file.Tag.Year = 2020;
            file.Tag.Track = 5;
            file.Tag.Genres = ["Rock"];
            file.Save();
        }

        var sut = new Mp3TagReader(NullLogger<Mp3TagReader>.Instance);

        var entry = await sut.ReadTagsAsync(path);

        entry.Succeeded.Should().BeTrue();
        entry.Tags.Should().NotBeNull();
        entry.Tags!.Title.Should().Be("Test Title");
        entry.Tags.Artist.Should().Be("Test Artist");
        entry.Tags.Album.Should().Be("Test Album");
        entry.Tags.Year.Should().Be(2020);
        entry.Tags.TrackNumber.Should().Be(5);
        entry.Tags.Genre.Should().Be("Rock");
    }

    [Fact]
    public async Task ReadTagsAsync_ReturnsFailure_WhenFileIsNotAValidMp3()
    {
        var path = Path.Combine(_root, "corrupted.mp3");
        File.WriteAllText(path, "this is not an mp3 file");

        var sut = new Mp3TagReader(NullLogger<Mp3TagReader>.Instance);

        var entry = await sut.ReadTagsAsync(path);

        entry.Succeeded.Should().BeFalse();
        entry.Error.Should().NotBeNullOrWhiteSpace();
        entry.Tags.Should().BeNull();
    }

    public void Dispose()
    {
        Directory.Delete(_root, recursive: true);
        GC.SuppressFinalize(this);
    }
}
