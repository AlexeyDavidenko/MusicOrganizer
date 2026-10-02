using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MusicOrganizer.Application.Scanning;
using MusicOrganizer.Domain;

namespace MusicOrganizer.Gui.ViewModels;

/// <summary>
/// Screen for <see cref="CollectionScanner"/> — the <c>scan</c> command. Read-only (no apply/run id),
/// so it doesn't fit <see cref="StreamingOperationViewModelBase{TOutcome}"/>'s dry-run shape.
/// </summary>
public sealed partial class ScanViewModel : ViewModelBase
{
    private readonly CollectionScanner _collectionScanner;

    /// <summary>
    /// Creates a new <see cref="ScanViewModel"/>.
    /// </summary>
    /// <param name="collectionScanner">Use case this screen wraps.</param>
    public ScanViewModel(CollectionScanner collectionScanner)
    {
        _collectionScanner = collectionScanner;
    }

    [ObservableProperty]
    public partial string? RootPath { get; set; }

    [ObservableProperty]
    public partial bool IsRunning { get; set; }

    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    /// <summary>Entries found by the most recent scan (cleared on each run).</summary>
    public ObservableCollection<ScanEntry> Results { get; } = [];

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task RunAsync(CancellationToken cancellationToken)
    {
        IsRunning = true;
        Results.Clear();
        StatusMessage = null;
        var count = 0;

        try
        {
            await foreach (var entry in _collectionScanner.ScanAsync(RootPath ?? string.Empty, cancellationToken))
            {
                Results.Add(entry);
                count++;
            }

            StatusMessage = $"Scanned {count} file(s).";
        }
        catch (OperationCanceledException)
        {
            StatusMessage = $"Cancelled after {count} file(s).";
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
