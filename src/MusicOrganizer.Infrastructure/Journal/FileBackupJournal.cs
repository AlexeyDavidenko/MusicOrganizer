using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using MusicOrganizer.Application.Journal;
using MusicOrganizer.Domain.Journal;

namespace MusicOrganizer.Infrastructure.Journal;

/// <summary>
/// Journal that backs up a full copy of each in-place-mutated file (or, for a move or an empty
/// directory removal, simply records the path change), and persists journal entries to a
/// manifest file under the user's local application data folder — so a later, separate process
/// invocation (e.g. a `rollback` command) can still find and restore them.
/// </summary>
public sealed partial class FileBackupJournal : IOperationJournal, IJournalMaintenance
{
    private readonly ILogger<FileBackupJournal> _logger;
    private readonly string _journalRoot;

    /// <summary>
    /// Creates a new <see cref="FileBackupJournal"/>.
    /// </summary>
    /// <param name="logger">Logger.</param>
    /// <param name="journalRoot">
    /// Root directory entries and backups are stored under. Defaults to a "journal" folder inside
    /// the user's local application data directory; overridable for testing.
    /// </param>
    public FileBackupJournal(ILogger<FileBackupJournal> logger, string? journalRoot = null)
    {
        _logger = logger;
        _journalRoot = journalRoot ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MusicOrganizer",
            "journal");
    }

    /// <inheritdoc />
    public async Task<JournalEntry> RecordMutationAsync(
        Guid runId,
        string filePath,
        string operationType,
        CancellationToken cancellationToken = default)
    {
        var runDirectory = GetRunDirectory(runId);
        var backupsDirectory = Path.Combine(runDirectory, "backups");
        Directory.CreateDirectory(backupsDirectory);

        var entryId = Guid.NewGuid();
        var backupPath = Path.Combine(backupsDirectory, $"{entryId}.bak");
        File.Copy(filePath, backupPath, overwrite: false);

        var entry = new JournalEntry(entryId, runId, filePath, backupPath, null, operationType, DateTimeOffset.UtcNow);
        await AppendToManifestAsync(runDirectory, entry, cancellationToken);

        LogEntryRecorded(entry.Id, runId, filePath);
        return entry;
    }

    /// <inheritdoc />
    public async Task<JournalEntry> RecordMoveAsync(
        Guid runId,
        string originalPath,
        string newPath,
        string operationType,
        CancellationToken cancellationToken = default)
    {
        var runDirectory = GetRunDirectory(runId);
        Directory.CreateDirectory(runDirectory);

        var entry = new JournalEntry(Guid.NewGuid(), runId, originalPath, null, newPath, operationType, DateTimeOffset.UtcNow);
        await AppendToManifestAsync(runDirectory, entry, cancellationToken);

        LogEntryRecorded(entry.Id, runId, originalPath);
        return entry;
    }

    /// <inheritdoc />
    public async Task<JournalEntry> RecordDirectoryRemovalAsync(
        Guid runId,
        string directoryPath,
        string operationType,
        CancellationToken cancellationToken = default)
    {
        var runDirectory = GetRunDirectory(runId);
        Directory.CreateDirectory(runDirectory);

        var entry = new JournalEntry(Guid.NewGuid(), runId, directoryPath, null, null, operationType, DateTimeOffset.UtcNow);
        await AppendToManifestAsync(runDirectory, entry, cancellationToken);

        LogEntryRecorded(entry.Id, runId, directoryPath);
        return entry;
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<JournalEntry> GetEntriesAsync(
        Guid runId,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var manifestPath = GetManifestPath(GetRunDirectory(runId));
        if (!File.Exists(manifestPath))
        {
            yield break;
        }

        using var reader = new StreamReader(manifestPath);
        while (await reader.ReadLineAsync(cancellationToken) is { Length: > 0 } line)
        {
            yield return JsonSerializer.Deserialize<JournalEntry>(line)!;
        }
    }

    /// <inheritdoc />
    public Task RestoreAsync(JournalEntry entry, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (entry.NewPath is not null)
        {
            File.Move(entry.NewPath, entry.OriginalPath, overwrite: true);
        }
        else if (entry.BackupPath is not null)
        {
            File.Copy(entry.BackupPath, entry.OriginalPath, overwrite: true);
        }
        else
        {
            // Neither a move nor a content backup - this is an empty directory removal; it held
            // nothing when removed, so recreating it (a no-op if something already occupies that
            // path) is a complete, lossless undo.
            Directory.CreateDirectory(entry.OriginalPath);
        }

        LogEntryRestored(entry.Id, entry.RunId, entry.OriginalPath);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<JournalRunSummary> GetAllRunsAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(_journalRoot))
        {
            yield break;
        }

        foreach (var runDirectory in Directory.EnumerateDirectories(_journalRoot))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!Guid.TryParse(Path.GetFileName(runDirectory), out var runId))
            {
                continue;
            }

            var entryCount = 0;
            var lastActivity = DateTimeOffset.MinValue;
            await foreach (var entry in GetEntriesAsync(runId, cancellationToken))
            {
                entryCount++;
                if (entry.TimestampUtc > lastActivity)
                {
                    lastActivity = entry.TimestampUtc;
                }
            }

            if (entryCount == 0)
            {
                continue;
            }

            yield return new JournalRunSummary(runId, lastActivity, entryCount);
        }
    }

    /// <inheritdoc />
    public Task DeleteRunAsync(Guid runId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var runDirectory = GetRunDirectory(runId);
        if (Directory.Exists(runDirectory))
        {
            Directory.Delete(runDirectory, recursive: true);
        }

        return Task.CompletedTask;
    }

    private static string GetManifestPath(string runDirectory) => Path.Combine(runDirectory, "manifest.jsonl");

    private static async Task AppendToManifestAsync(string runDirectory, JournalEntry entry, CancellationToken cancellationToken)
    {
        var line = JsonSerializer.Serialize(entry) + Environment.NewLine;
        await File.AppendAllTextAsync(GetManifestPath(runDirectory), line, cancellationToken);
    }

    private string GetRunDirectory(Guid runId) => Path.Combine(_journalRoot, runId.ToString());

    [LoggerMessage(Level = LogLevel.Debug, Message = "Recorded journal entry {EntryId} for run {RunId} ({FilePath})")]
    private partial void LogEntryRecorded(Guid entryId, Guid runId, string filePath);

    [LoggerMessage(Level = LogLevel.Information, Message = "Restored journal entry {EntryId} for run {RunId} ({OriginalPath})")]
    private partial void LogEntryRestored(Guid entryId, Guid runId, string originalPath);
}
