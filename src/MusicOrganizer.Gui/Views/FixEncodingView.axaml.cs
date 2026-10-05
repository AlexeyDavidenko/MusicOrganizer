using Avalonia.Controls;
using Avalonia.Interactivity;
using MusicOrganizer.Application.Reporting;
using MusicOrganizer.Gui.ViewModels;

namespace MusicOrganizer.Gui.Views;

/// <summary>
/// View for <see cref="FixEncodingViewModel"/>.
/// </summary>
public partial class FixEncodingView : UserControl
{
    /// <summary>
    /// Creates a new <see cref="FixEncodingView"/>.
    /// </summary>
    public FixEncodingView()
    {
        InitializeComponent();
    }

    private async void OnBrowseFolderClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not FixEncodingViewModel viewModel)
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
        if (DataContext is not FixEncodingViewModel viewModel)
        {
            return;
        }

        await ResultsExporter.SaveAsync(
            this,
            viewModel.Results,
            outcome =>
            {
                var fields = string.Join(", ", outcome.FixedFields);
                var status = outcome.Error is { } error
                    ? $"[ERROR] {error}"
                    : outcome.Applied ? "[FIXED]" : "[WOULD FIX]";
                var review = outcome.NeedsManualReview ? " [MANUAL REVIEW NEEDED]" : string.Empty;
                return $"{status} {outcome.FilePath} — {fields}{review}";
            },
            "Fix Encoding",
            ["Status", "FilePath", "FixedFields", "Error", "NeedsManualReview"],
            outcome => new ReportRow([
                outcome.Error is not null ? "ERROR" : outcome.Applied ? "FIXED" : outcome.HasFix ? "WOULD FIX" : "UNCHANGED",
                outcome.FilePath,
                string.Join(", ", outcome.FixedFields),
                outcome.Error,
                outcome.NeedsManualReview.ToString()]));
    }
}
