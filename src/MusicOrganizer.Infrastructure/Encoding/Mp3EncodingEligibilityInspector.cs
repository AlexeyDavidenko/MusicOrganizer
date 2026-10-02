using Microsoft.Extensions.Logging;
using MusicOrganizer.Application.Encoding;
using MusicOrganizer.Domain.Encoding;

namespace MusicOrganizer.Infrastructure.Encoding;

/// <summary>
/// Determines per-field encoding-fix eligibility by reading each relevant ID3v2 text frame's own
/// declared encoding directly via TagLibSharp. A field with no ID3v2 frame present falls back to
/// ID3v1 (or is simply absent), which has no encoding byte at all and is always Latin1-decoded -
/// see <see cref="TextFieldEligibility"/>.
/// </summary>
public sealed partial class Mp3EncodingEligibilityInspector : IEncodingEligibilityInspector
{
    private static readonly TextFieldEligibility NoneEligible = new(false, false, false, false);

    private readonly ILogger<Mp3EncodingEligibilityInspector> _logger;

    /// <summary>
    /// Creates a new <see cref="Mp3EncodingEligibilityInspector"/>.
    /// </summary>
    /// <param name="logger">Logger.</param>
    public Mp3EncodingEligibilityInspector(ILogger<Mp3EncodingEligibilityInspector> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<TextFieldEligibility> InspectAsync(string filePath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            using var file = TagLib.File.Create(filePath);
            var id3v2 = file.GetTag(TagLib.TagTypes.Id3v2, false) as TagLib.Id3v2.Tag;

            return Task.FromResult(new TextFieldEligibility(
                TitleIsLatin1Sourced: IsLatin1Sourced(id3v2, "TIT2"),
                ArtistIsLatin1Sourced: IsLatin1Sourced(id3v2, "TPE1"),
                AlbumIsLatin1Sourced: IsLatin1Sourced(id3v2, "TALB"),
                GenreIsLatin1Sourced: IsLatin1Sourced(id3v2, "TCON")));
        }
        catch (Exception ex) when (ex is TagLib.CorruptFileException or TagLib.UnsupportedFormatException
            or IOException or UnauthorizedAccessException)
        {
            LogInspectionFailed(filePath, ex);
            return Task.FromResult(NoneEligible);
        }
    }

    private static bool IsLatin1Sourced(TagLib.Id3v2.Tag? id3v2, TagLib.ByteVector frameId)
    {
        var frame = id3v2 is null ? null : TagLib.Id3v2.TextInformationFrame.Get(id3v2, frameId, create: false);
        return frame is null || frame.TextEncoding == TagLib.StringType.Latin1;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to inspect ID3v2 frame encodings in {FilePath}; treating all fields as ineligible for an encoding fix")]
    private partial void LogInspectionFailed(string filePath, Exception exception);
}
