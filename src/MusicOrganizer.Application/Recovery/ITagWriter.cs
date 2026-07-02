using MusicOrganizer.Domain;
using MusicOrganizer.Domain.TagRecovery;

namespace MusicOrganizer.Application.Recovery;

/// <summary>
/// Port for writing tags to a single audio file. Implementations must never throw for a
/// corrupted or unsupported file — such cases are reported as a failed <see cref="TagWriteResult"/>
/// so that processing the rest of a large collection can continue.
/// </summary>
public interface ITagWriter
{
    /// <summary>
    /// Writes <paramref name="tags"/> to the file at <paramref name="filePath"/>. Only non-null
    /// fields of <paramref name="tags"/> are written; existing tag data for other fields is left
    /// untouched.
    /// </summary>
    /// <param name="filePath">Path of the file to write to.</param>
    /// <param name="tags">Tags to write.</param>
    /// <param name="cancellationToken">Token used to cancel the write.</param>
    public Task<TagWriteResult> WriteTagsAsync(string filePath, AudioTags tags, CancellationToken cancellationToken = default);
}
