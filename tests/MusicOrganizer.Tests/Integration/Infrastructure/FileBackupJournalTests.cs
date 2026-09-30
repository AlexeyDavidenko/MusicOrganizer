using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using MusicOrganizer.Domain.Journal;
using MusicOrganizer.Infrastructure.Journal;

namespace MusicOrganizer.Tests.Integration.Infrastructure;

public class FileBackupJournalTests : IDisposable
{
    private readonly string _root = Directory.CreateTempSubdirectory("musicorganizer-journal-").FullName;
    private string JournalRoot => Path.Combine(_root, "journal-root");

    [Fact]
    public async Task RecordAndRestore_RoundTripsTheOriginalFileContent()
    {
        var filePath = Path.Combine(_root, "song.mp3");
        await File.WriteAllTextAsync(filePath, "original content");

        var runId = Guid.NewGuid();
        var journal = new FileBackupJournal(NullLogger<FileBackupJournal>.Instance, JournalRoot);

        var entry = await journal.RecordMutationAsync(runId, filePath, "tag-recovery");

        await File.WriteAllTextAsync(filePath, "mutated content");
        await journal.RestoreAsync(entry);

        var restoredContent = await File.ReadAllTextAsync(filePath);
        restoredContent.Should().Be("original content");
    }

    [Fact]
    public async Task RecordMoveAndRestore_MovesTheFileBackToItsOriginalPath()
    {
        var originalPath = Path.Combine(_root, "Original Name.mp3");
        var newPath = Path.Combine(_root, "New Name.mp3");
        await File.WriteAllTextAsync(originalPath, "content");

        var runId = Guid.NewGuid();
        var journal = new FileBackupJournal(NullLogger<FileBackupJournal>.Instance, JournalRoot);

        var entry = await journal.RecordMoveAsync(runId, originalPath, newPath, "rename");
        File.Move(originalPath, newPath);

        await journal.RestoreAsync(entry);

        File.Exists(originalPath).Should().BeTrue();
        File.Exists(newPath).Should().BeFalse();
    }

    [Fact]
    public async Task RecordDirectoryRemovalAndRestore_RecreatesTheEmptyDirectory()
    {
        var directoryPath = Path.Combine(_root, "Pink Floyd");
        Directory.CreateDirectory(directoryPath);

        var runId = Guid.NewGuid();
        var journal = new FileBackupJournal(NullLogger<FileBackupJournal>.Instance, JournalRoot);

        var entry = await journal.RecordDirectoryRemovalAsync(runId, directoryPath, "organize-prune-folder");
        Directory.Delete(directoryPath);

        await journal.RestoreAsync(entry);

        Directory.Exists(directoryPath).Should().BeTrue();
    }

    [Fact]
    public async Task GetAllRunsAsync_SummarizesEveryRecordedRun()
    {
        var filePath = Path.Combine(_root, "song.mp3");
        await File.WriteAllTextAsync(filePath, "content");
        var runId = Guid.NewGuid();
        var journal = new FileBackupJournal(NullLogger<FileBackupJournal>.Instance, JournalRoot);
        await journal.RecordMutationAsync(runId, filePath, "tag-recovery");
        await journal.RecordMutationAsync(runId, filePath, "tag-recovery");

        var runs = new List<JournalRunSummary>();
        await foreach (var run in journal.GetAllRunsAsync())
        {
            runs.Add(run);
        }

        runs.Should().ContainSingle(r => r.RunId == runId && r.EntryCount == 2);
    }

    [Fact]
    public async Task DeleteRunAsync_RemovesTheRunFromSubsequentEnumeration()
    {
        var filePath = Path.Combine(_root, "song.mp3");
        await File.WriteAllTextAsync(filePath, "content");
        var runId = Guid.NewGuid();
        var journal = new FileBackupJournal(NullLogger<FileBackupJournal>.Instance, JournalRoot);
        await journal.RecordMutationAsync(runId, filePath, "tag-recovery");

        await journal.DeleteRunAsync(runId);

        var runs = new List<JournalRunSummary>();
        await foreach (var run in journal.GetAllRunsAsync())
        {
            runs.Add(run);
        }

        runs.Should().NotContain(r => r.RunId == runId);
    }

    [Fact]
    public async Task GetEntriesAsync_ReadsEntriesRecordedByAPreviousJournalInstance()
    {
        var filePath = Path.Combine(_root, "song.mp3");
        await File.WriteAllTextAsync(filePath, "original content");

        var runId = Guid.NewGuid();
        var firstJournal = new FileBackupJournal(NullLogger<FileBackupJournal>.Instance, JournalRoot);
        var recorded = await firstJournal.RecordMutationAsync(runId, filePath, "tag-recovery");

        var secondJournal = new FileBackupJournal(NullLogger<FileBackupJournal>.Instance, JournalRoot);
        var entries = new List<MusicOrganizer.Domain.Journal.JournalEntry>();
        await foreach (var entry in secondJournal.GetEntriesAsync(runId))
        {
            entries.Add(entry);
        }

        entries.Should().ContainSingle(e => e.Id == recorded.Id && e.OriginalPath == filePath);
    }

    public void Dispose()
    {
        Directory.Delete(_root, recursive: true);
        GC.SuppressFinalize(this);
    }
}
