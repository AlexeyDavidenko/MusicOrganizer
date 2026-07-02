namespace MusicOrganizer.Domain;

/// <summary>
/// Immutable snapshot of the metadata tags read from an audio file.
/// Any field may be absent, since tags are often only partially present.
/// </summary>
/// <param name="Title">Track title, if present.</param>
/// <param name="Artist">Performing artist(s), if present.</param>
/// <param name="Album">Album name, if present.</param>
/// <param name="Year">Release year, if present.</param>
/// <param name="TrackNumber">Track number within the album, if present.</param>
/// <param name="Genre">Genre(s), if present.</param>
public sealed record AudioTags(
    string? Title,
    string? Artist,
    string? Album,
    int? Year,
    int? TrackNumber,
    string? Genre);
