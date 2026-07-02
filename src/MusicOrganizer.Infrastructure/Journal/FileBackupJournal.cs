using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using MusicOrganizer.Application.Journal;
using MusicOrganizer.Domain.Journal;

namespace MusicOrganizer.Infrastructure.Journal;

/// <summary>
/// Journal that backs up a full copy of each file before it is mutated, and persists journal
/// entries to a manifest file under the user's local application data folder — so a later,
/// separate process invocation (e.g. a `rollback` command) can still find and restore them.
/// </summary>
public sealed partial class FileBackupJournal : IOperationJournal
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
    public async Task<JournalEntry> RecordAsync(
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

        var entry = new JournalEntry(entryId, runId, filePath, backupPath, operationType, DateTimeOffset.UtcNow);
        await AppendToManifestAsync(runDirectory, entry, cancellationToken);

        LogEntryRecorded(entry.Id, runId, filePath);
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
        File.Copy(entry.BackupPath, entry.OriginalPath, overwrite: true);
        LogEntryRestored(entry.Id, entry.RunId, entry.OriginalPath);
        return Task.CompletedTask;
    }

    private string GetRunDirectory(Guid runId) => Path.Combine(_journalRoot, runId.ToString());

    private static string GetManifestPath(string runDirectory) => Path.Combine(runDirectory, "manifest.jsonl");

    private static async Task AppendToManifestAsync(string runDirectory, JournalEntry entry, CancellationToken cancellationToken)
    {
        var line = JsonSerializer.Serialize(entry) + Environment.NewLine;
        await File.AppendAllTextAsync(GetManifestPath(runDirectory), line, cancellationToken);
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Recorded journal entry {EntryId} for run {RunId} ({FilePath})")]
    private partial void LogEntryRecorded(Guid entryId, Guid runId, string filePath);

    [LoggerMessage(Level = LogLevel.Information, Message = "Restored journal entry {EntryId} for run {RunId} ({OriginalPath})")]
    private partial void LogEntryRestored(Guid entryId, Guid runId, string originalPath);
}
