using Avalonia.Controls;
using Avalonia.Interactivity;
using MusicOrganizer.Application.Reporting;
using MusicOrganizer.Gui.ViewModels;

namespace MusicOrganizer.Gui.Views;

/// <summary>
/// View for <see cref="RemoveDuplicatesViewModel"/>.
/// </summary>
public partial class RemoveDuplicatesView : UserControl
{
    /// <summary>
    /// Creates a new <see cref="RemoveDuplicatesView"/>.
    /// </summary>
    public RemoveDuplicatesView()
    {
        InitializeComponent();
    }

    private async void OnBrowseFolderClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not RemoveDuplicatesViewModel viewModel)
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
        if (DataContext is not RemoveDuplicatesViewModel viewModel)
        {
            return;
        }

        await ResultsExporter.SaveAsync(
            this,
            viewModel.Results,
            outcome =>
            {
                var status = outcome.Error is { } error ? $"[ERROR] {error}" : outcome.Applied ? "[DELETED]" : "[WOULD DELETE]";
                return $"{status} {outcome.FilePath} (kept: {outcome.KeptFilePath})";
            },
            "Remove Duplicates",
            ["Status", "FilePath", "KeptFilePath", "Error"],
            outcome => new ReportRow([
                outcome.Error is not null ? "ERROR" : outcome.Applied ? "DELETED" : "WOULD DELETE",
                outcome.FilePath,
                outcome.KeptFilePath,
                outcome.Error]));
    }
}
