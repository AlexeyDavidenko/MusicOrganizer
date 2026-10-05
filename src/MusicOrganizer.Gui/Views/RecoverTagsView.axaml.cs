using Avalonia.Controls;
using Avalonia.Interactivity;
using MusicOrganizer.Application.Reporting;
using MusicOrganizer.Gui.ViewModels;

namespace MusicOrganizer.Gui.Views;

/// <summary>
/// View for <see cref="RecoverTagsViewModel"/>.
/// </summary>
public partial class RecoverTagsView : UserControl
{
    /// <summary>
    /// Creates a new <see cref="RecoverTagsView"/>.
    /// </summary>
    public RecoverTagsView()
    {
        InitializeComponent();
    }

    private async void OnBrowseFolderClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not RecoverTagsViewModel viewModel)
        {
            return;
        }

        var path = await FolderPicker.PickFolderAsync(this);
        if (path is not null)
        {
            viewModel.RootPath = path;
        }
    }

    private async void OnSaveResultsClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not RecoverTagsViewModel viewModel)
        {
            return;
        }

        await ResultsExporter.SaveAsync(
            this,
            viewModel.Results,
            outcome =>
            {
                var fields = string.Join(", ", outcome.RecoveredFields);
                var status = outcome.Error is { } error
                    ? $"[ERROR] {error}"
                    : outcome.Applied ? "[RECOVERED]" : "[WOULD RECOVER]";
                var review = outcome.NeedsManualReview ? " [MANUAL REVIEW NEEDED]" : string.Empty;
                return $"{status} {outcome.FilePath} — {fields}{review}";
            },
            "Recover Tags",
            ["Status", "FilePath", "RecoveredFields", "Error", "NeedsManualReview"],
            outcome => new ReportRow([
                outcome.Error is not null ? "ERROR" : outcome.Applied ? "RECOVERED" : outcome.HasRecovery ? "WOULD RECOVER" : "UNCHANGED",
                outcome.FilePath,
                string.Join(", ", outcome.RecoveredFields),
                outcome.Error,
                outcome.NeedsManualReview.ToString()]));
    }
}
