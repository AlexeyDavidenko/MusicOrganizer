using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MusicOrganizer.Gui.ViewModels;

/// <summary>
/// Shared shape for the six screens backed by a use case of the form
/// <c>(runId, dryRun, cancellationToken) -&gt; IAsyncEnumerable&lt;TOutcome&gt;</c> — every mutating
/// command except <c>rollback</c> (different input: a run id, not a folder) follows this. The
/// command-specific input (a root folder for most, an age cutoff for <c>clean-journal</c>) lives on
/// the concrete subclass, not here, since its type genuinely differs between screens.
/// </summary>
/// <typeparam name="TOutcome">Per-item outcome type streamed by the wrapped use case.</typeparam>
public abstract partial class StreamingOperationViewModelBase<TOutcome> : ViewModelBase
{
    [ObservableProperty]
    public partial bool Apply { get; set; }

    [ObservableProperty]
    public partial bool IsRunning { get; set; }

    [ObservableProperty]
    public partial Guid? LastRunId { get; set; }

    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    /// <summary>Outcomes streamed by the most recent run, most recent run only (cleared on each run).</summary>
    public ObservableCollection<TOutcome> Results { get; } = [];

    /// <summary>Runs the wrapped use case for the current screen's input.</summary>
    /// <param name="runId">Identifier for this run, used for journaling.</param>
    /// <param name="dryRun">When true, no changes are made and nothing is journaled.</param>
    /// <param name="cancellationToken">Token used to stop the run early.</param>
    /// <returns>One outcome per item processed.</returns>
    protected abstract IAsyncEnumerable<TOutcome> ExecuteAsync(Guid runId, bool dryRun, CancellationToken cancellationToken);

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task RunAsync(CancellationToken cancellationToken)
    {
        IsRunning = true;
        Results.Clear();
        StatusMessage = null;
        var runId = Guid.NewGuid();
        LastRunId = runId;
        var count = 0;

        try
        {
            await foreach (var outcome in ExecuteAsync(runId, dryRun: !Apply, cancellationToken))
            {
                Results.Add(outcome);
                count++;
            }

            StatusMessage = $"Processed {count} item(s).";
        }
        catch (OperationCanceledException)
        {
            StatusMessage = $"Cancelled after {count} item(s).";
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
}
