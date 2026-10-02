using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MusicOrganizer.Application.Journal;
using MusicOrganizer.Domain.Journal;

namespace MusicOrganizer.Gui.ViewModels;

/// <summary>
/// Screen for <see cref="RollbackRunUseCase"/> — the <c>rollback</c> command. Input is a run id,
/// not a folder, so it doesn't fit either of the other base shapes. GUI-only convenience over the
/// CLI: lists recent runs via <see cref="IJournalMaintenance"/> (already registered for
/// <c>clean-journal</c>) instead of requiring the run id to be pasted from memory.
/// </summary>
public sealed partial class RollbackViewModel : ViewModelBase
{
    private readonly RollbackRunUseCase _rollbackRunUseCase;
    private readonly IJournalMaintenance _journalMaintenance;

    /// <summary>
    /// Creates a new <see cref="RollbackViewModel"/>.
    /// </summary>
    /// <param name="rollbackRunUseCase">Use case that performs the rollback.</param>
    /// <param name="journalMaintenance">Used to list available runs for the picker.</param>
    public RollbackViewModel(RollbackRunUseCase rollbackRunUseCase, IJournalMaintenance journalMaintenance)
    {
        _rollbackRunUseCase = rollbackRunUseCase;
        _journalMaintenance = journalMaintenance;
    }

    [ObservableProperty]
    public partial JournalRunSummary? SelectedRun { get; set; }

    [ObservableProperty]
    public partial string? RunIdText { get; set; }

    [ObservableProperty]
    public partial bool IsRunning { get; set; }

    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    /// <summary>Runs currently recorded in the journal, newest first.</summary>
    public ObservableCollection<JournalRunSummary> AvailableRuns { get; } = [];

    /// <summary>Journal entries restored by the most recent rollback.</summary>
    public ObservableCollection<JournalEntry> Results { get; } = [];

    [RelayCommand]
    private async Task RefreshRunsAsync(CancellationToken cancellationToken)
    {
        AvailableRuns.Clear();
        var runs = new List<JournalRunSummary>();
        await foreach (var run in _journalMaintenance.GetAllRunsAsync(cancellationToken))
        {
            runs.Add(run);
        }

        foreach (var run in runs.OrderByDescending(r => r.LastActivityAtUtc))
        {
            AvailableRuns.Add(run);
        }
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task RunAsync(CancellationToken cancellationToken)
    {
        if (!TryResolveRunId(out var runId))
        {
            StatusMessage = "Select a run or enter a valid run id.";
            return;
        }

        IsRunning = true;
        Results.Clear();
        StatusMessage = null;
        var count = 0;

        try
        {
            await foreach (var entry in _rollbackRunUseCase.RollbackAsync(runId, cancellationToken))
            {
                Results.Add(entry);
                count++;
            }

            StatusMessage = $"Restored {count} entry(ies).";
        }
        catch (OperationCanceledException)
        {
            StatusMessage = $"Cancelled after {count} entry(ies).";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed: {ex.Message}";
        }
        finally
        {
            IsRunning = false;
        }
    }

    private bool TryResolveRunId(out Guid runId)
    {
        if (SelectedRun is { } selected)
        {
            runId = selected.RunId;
            return true;
        }

        return Guid.TryParse(RunIdText, out runId);
    }
}
