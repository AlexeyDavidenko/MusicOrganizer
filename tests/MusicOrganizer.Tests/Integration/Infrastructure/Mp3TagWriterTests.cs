using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using MusicOrganizer.Domain;
using MusicOrganizer.Infrastructure.Tags;
using MusicOrganizer.Tests.TestSupport;

namespace MusicOrganizer.Tests.Integration.Infrastructure;

public class Mp3TagWriterTests : IDisposable
{
    private readonly string _root = Directory.CreateTempSubdirectory("musicorganizer-tagwrite-").FullName;

    [Fact]
    public async Task WriteTagsAsync_WritesOnlyTheProvidedFields_LeavingOthersUntouched()
    {
        var path = Path.Combine(_root, "tagged.mp3");
        MinimalMp3Fixture.CreateAt(path);

        using (var file = TagLib.File.Create(path))
        {
            file.Tag.Title = "Original Title";
            file.Tag.Album = "Original Album";
            file.Save();
        }

        var writer = new Mp3TagWriter(NullLogger<Mp3TagWriter>.Instance);
        var newTags = new AudioTags(Title: null, Artist: "Recovered Artist", Album: null, Year: null, TrackNumber: null, Genre: null);

        var result = await writer.WriteTagsAsync(path, newTags);

        result.Succeeded.Should().BeTrue();

        var reader = new Mp3TagReader(NullLogger<Mp3TagReader>.Instance);
        var entry = await reader.ReadTagsAsync(path);

        entry.Tags!.Artist.Should().Be("Recovered Artist");
        entry.Tags.Title.Should().Be("Original Title");
        entry.Tags.Album.Should().Be("Original Album");
    }

    [Fact]
    public async Task WriteTagsAsync_ReturnsFailure_WhenFileIsNotAValidMp3()
    {
        var path = Path.Combine(_root, "corrupted.mp3");
        File.WriteAllText(path, "this is not an mp3 file");

        var writer = new Mp3TagWriter(NullLogger<Mp3TagWriter>.Instance);

        var result = await writer.WriteTagsAsync(path, new AudioTags(null, "Artist", null, null, null, null));

        result.Succeeded.Should().BeFalse();
        result.Error.Should().NotBeNullOrWhiteSpace();
    }

    public void Dispose()
    {
        Directory.Delete(_root, recursive: true);
        GC.SuppressFinalize(this);
    }
}
