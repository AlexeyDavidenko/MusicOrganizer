namespace MusicOrganizer.Domain.Renaming;

/// <summary>
/// Builds the target folder for a track under the collection's Artist/Album tree: "root/Artist/
/// Album", falling back to "root/Artist" (flat) when Album is unknown. Mirrors the depth
/// convention <see cref="TagRecovery.FolderStructureTagRecovery"/> already reads back out of an
/// existing collection, applied the other way: writing tags into folder structure instead of
/// reading folder structure into tags.
/// </summary>
public static class OrganizationTemplate
{
    /// <summary>
    /// Builds the target directory for a track with the given Artist and (optional) Album.
    /// </summary>
    /// <param name="rootPath">Root folder the collection is organized under.</param>
    /// <param name="artist">Artist tag value. Required - callers must not invoke this without one.</param>
    /// <param name="album">Album tag value, if known.</param>
    public static string BuildTargetDirectory(string rootPath, string artist, string? album)
    {
        var sanitizedArtist = FileNameSanitizer.SanitizeComponent(artist);

        if (album is null)
        {
            return Path.Combine(rootPath, sanitizedArtist);
        }

        var sanitizedAlbum = FileNameSanitizer.SanitizeComponent(album);
        return Path.Combine(rootPath, sanitizedArtist, sanitizedAlbum);
    }
}
