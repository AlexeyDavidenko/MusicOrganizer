using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MusicOrganizer.Application.Deduplication;
using MusicOrganizer.Domain.Deduplication;

namespace MusicOrganizer.Gui.ViewModels;

/// <summary>
/// Screen for <see cref="IDuplicateFinder"/> — the <c>find-duplicates</c> command. Read-only and
/// returns one batch rather than a stream, so it doesn't fit
/// <see cref="StreamingOperationViewModelBase{TOutcome}"/>'s per-item streaming shape.
/// </summary>
public sealed partial class FindDuplicatesViewModel : ViewModelBase
{
    private readonly IDuplicateFinder _duplicateFinder;

    /// <summary>
    /// Creates a new <see cref="FindDuplicatesViewModel"/>.
    /// </summary>
    /// <param name="duplicateFinder">Use case this screen wraps.</param>
    public FindDuplicatesViewModel(IDuplicateFinder duplicateFinder)
    {
        _duplicateFinder = duplicateFinder;
    }

    [ObservableProperty]
    public partial string? RootPath { get; set; }

    [ObservableProperty]
    public partial bool IsRunning { get; set; }

    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    [ObservableProperty]
    public partial long WastedBytes { get; set; }

    /// <summary>Exact-content duplicate groups found by the most recent run.</summary>
    public ObservableCollection<DuplicateGroup> ExactGroups { get; } = [];

    /// <summary>Matching-tags-only duplicate groups found by the most recent run.</summary>
    public ObservableCollection<DuplicateGroup> TagMatchGroups { get; } = [];

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task RunAsync(CancellationToken cancellationToken)
    {
        IsRunning = true;
        ExactGroups.Clear();
        TagMatchGroups.Clear();
        WastedBytes = 0;
        StatusMessage = null;

        try
        {
            var groups = await _duplicateFinder.FindAsync(RootPath ?? string.Empty, cancellationToken);

            foreach (var group in groups)
            {
                if (group.Kind == DuplicateMatchKind.Exact)
                {
                    ExactGroups.Add(group);
                    WastedBytes += (group.FilePaths.Count - 1) * (group.FileSize ?? 0);
                }
                else
                {
                    TagMatchGroups.Add(group);
                }
            }

            StatusMessage = $"Found {ExactGroups.Count} exact group(s) and {TagMatchGroups.Count} tag-match group(s).";
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Cancelled.";
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
