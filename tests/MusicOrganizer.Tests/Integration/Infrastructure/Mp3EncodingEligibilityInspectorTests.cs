using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using MusicOrganizer.Infrastructure.Encoding;
using MusicOrganizer.Tests.TestSupport;

namespace MusicOrganizer.Tests.Integration.Infrastructure;

public class Mp3EncodingEligibilityInspectorTests : IDisposable
{
    private readonly string _root = Directory.CreateTempSubdirectory("musicorganizer-encoding-inspect-").FullName;

    [Fact]
    public async Task InspectAsync_IsEligible_WhenId3v2FrameDeclaresLatin1()
    {
        var path = Path.Combine(_root, "latin1.mp3");
        MinimalMp3Fixture.CreateAt(path);

        using (var file = TagLib.File.Create(path))
        {
            var frame = TagLib.Id3v2.TextInformationFrame.Get(
                (TagLib.Id3v2.Tag)file.GetTag(TagLib.TagTypes.Id3v2, true)!, "TPE1", TagLib.StringType.Latin1, true);
            frame.Text = ["Artist"];
            file.Save();
        }

        var sut = new Mp3EncodingEligibilityInspector(NullLogger<Mp3EncodingEligibilityInspector>.Instance);

        var result = await sut.InspectAsync(path);

        result.ArtistIsLatin1Sourced.Should().BeTrue();
    }

    [Fact]
    public async Task InspectAsync_IsNotEligible_WhenId3v2FrameDeclaresUtf8()
    {
        var path = Path.Combine(_root, "utf8.mp3");
        MinimalMp3Fixture.CreateAt(path);

        using (var file = TagLib.File.Create(path))
        {
            var frame = TagLib.Id3v2.TextInformationFrame.Get(
                (TagLib.Id3v2.Tag)file.GetTag(TagLib.TagTypes.Id3v2, true)!, "TPE1", TagLib.StringType.UTF8, true);
            frame.Text = ["Максим Фадеев"];
            file.Save();
        }

        var sut = new Mp3EncodingEligibilityInspector(NullLogger<Mp3EncodingEligibilityInspector>.Instance);

        var result = await sut.InspectAsync(path);

        result.ArtistIsLatin1Sourced.Should().BeFalse();
    }

    [Fact]
    public async Task InspectAsync_IsEligible_WhenNoId3v2FrameIsPresentForTheField()
    {
        var path = Path.Combine(_root, "no-frame.mp3");
        MinimalMp3Fixture.CreateAt(path);

        using (var file = TagLib.File.Create(path))
        {
            // Only Title is set via ID3v2 - Artist has no frame at all, so it (or an ID3v1
            // fallback) is always Latin1-sourced.
            var frame = TagLib.Id3v2.TextInformationFrame.Get(
                (TagLib.Id3v2.Tag)file.GetTag(TagLib.TagTypes.Id3v2, true)!, "TIT2", TagLib.StringType.UTF8, true);
            frame.Text = ["Title"];
            file.Save();
        }

        var sut = new Mp3EncodingEligibilityInspector(NullLogger<Mp3EncodingEligibilityInspector>.Instance);

        var result = await sut.InspectAsync(path);

        result.TitleIsLatin1Sourced.Should().BeFalse();
        result.ArtistIsLatin1Sourced.Should().BeTrue();
    }

    [Fact]
    public async Task InspectAsync_ReturnsAllIneligible_ForACorruptedFile()
    {
        var path = Path.Combine(_root, "corrupted.mp3");
        File.WriteAllText(path, "this is not an mp3 file");

        var sut = new Mp3EncodingEligibilityInspector(NullLogger<Mp3EncodingEligibilityInspector>.Instance);

        var result = await sut.InspectAsync(path);

        result.TitleIsLatin1Sourced.Should().BeFalse();
        result.ArtistIsLatin1Sourced.Should().BeFalse();
        result.AlbumIsLatin1Sourced.Should().BeFalse();
        result.GenreIsLatin1Sourced.Should().BeFalse();
    }

    public void Dispose()
    {
        Directory.Delete(_root, recursive: true);
        GC.SuppressFinalize(this);
    }
}
