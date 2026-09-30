using MusicOrganizer.Domain.Journal;

namespace MusicOrganizer.Application.Journal;

/// <summary>
/// Port for recording and restoring pre-mutation backups of files, so any mutating operation can
/// be rolled back.
/// </summary>
public interface IOperationJournal
{
    /// <summary>
    /// Backs up the file at <paramref name="filePath"/> and durably records a restore point for
    /// it. Must complete before the file's content is mutated in place.
    /// </summary>
    /// <param name="runId">Identifier of the operation run this backup belongs to.</param>
    /// <param name="filePath">Path of the file about to be mutated.</param>
    /// <param name="operationType">Name of the operation performing the mutation.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    public Task<JournalEntry> RecordMutationAsync(Guid runId, string filePath, string operationType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Durably records that <paramref name="originalPath"/> is about to be moved to
    /// <paramref name="newPath"/>. Must complete before the move happens. Unlike
    /// <see cref="RecordMutationAsync"/>, no byte copy is made — the move itself doesn't change
    /// file content, so the entry alone is enough to reverse it.
    /// </summary>
    /// <param name="runId">Identifier of the operation run this entry belongs to.</param>
    /// <param name="originalPath">Current path of the file about to be moved.</param>
    /// <param name="newPath">Path the file is about to be moved to.</param>
    /// <param name="operationType">Name of the operation performing the move.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    public Task<JournalEntry> RecordMoveAsync(Guid runId, string originalPath, string newPath, string operationType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Durably records that the empty directory at <paramref name="directoryPath"/> is about to
    /// be removed. Must complete before the directory is deleted. No byte copy is needed - the
    /// directory is empty by definition when this is called, so recreating it on restore is
    /// enough to reverse the removal.
    /// </summary>
    /// <param name="runId">Identifier of the operation run this entry belongs to.</param>
    /// <param name="directoryPath">Path of the empty directory about to be removed.</param>
    /// <param name="operationType">Name of the operation performing the removal.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    public Task<JournalEntry> RecordDirectoryRemovalAsync(Guid runId, string directoryPath, string operationType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Streams every journal entry recorded for <paramref name="runId"/>.
    /// </summary>
    /// <param name="runId">Identifier of the operation run to look up.</param>
    /// <param name="cancellationToken">Token used to cancel enumeration.</param>
    public IAsyncEnumerable<JournalEntry> GetEntriesAsync(Guid runId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Restores whatever <paramref name="entry"/> describes - a moved file, an in-place content
    /// change, or a removed empty directory - to its original path.
    /// </summary>
    /// <param name="entry">Entry describing the change to restore.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    public Task RestoreAsync(JournalEntry entry, CancellationToken cancellationToken = default);
}
