namespace MusicOrganizer.Domain.Encoding;

/// <summary>
/// Per-field flag for whether an <see cref="AudioTags"/> value's current text came from a source
/// that decodes as Latin1 with no other information (ID3v1, which has no encoding field at all; or
/// an ID3v2 text frame whose own declared encoding byte is Latin1) and is therefore eligible for
/// <see cref="TextEncodingDetector"/>'s reversible-byte re-decoding. A field sourced from an ID3v2
/// frame explicitly declared UTF-8/UTF-16/UTF-16BE was already decoded correctly per its own
/// declared encoding and must never be run through the Latin1-reversal trick, which would corrupt
/// genuine multi-byte Unicode text.
/// </summary>
public sealed record TextFieldEligibility(
    bool TitleIsLatin1Sourced,
    bool ArtistIsLatin1Sourced,
    bool AlbumIsLatin1Sourced,
    bool GenreIsLatin1Sourced);
