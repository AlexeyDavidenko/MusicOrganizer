using Avalonia.Controls;
using Avalonia.Interactivity;
using MusicOrganizer.Application.Reporting;
using MusicOrganizer.Gui.ViewModels;

namespace MusicOrganizer.Gui.Views;

/// <summary>
/// View for <see cref="RenameViewModel"/>.
/// </summary>
public partial class RenameView : UserControl
{
    /// <summary>
    /// Creates a new <see cref="RenameView"/>.
    /// </summary>
    public RenameView()
    {
        InitializeComponent();
    }

    private async void OnBrowseFolderClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not RenameViewModel viewModel)
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
        if (DataContext is not RenameViewModel viewModel)
        {
            return;
        }

        await ResultsExporter.SaveAsync(
            this,
            viewModel.Results,
            outcome =>
            {
                var status = outcome.Error is { } error
                    ? $"[ERROR] {error}"
                    : outcome.Applied ? "[RENAMED]" : outcome.NeedsRename ? "[WOULD RENAME]" : "[SKIPPED]";
                return $"{status} {outcome.OriginalPath} -> {outcome.ProposedPath}";
            },
            "Rename",
            ["Status", "OriginalPath", "ProposedPath", "Error"],
            outcome => new ReportRow([
                outcome.Error is not null ? "ERROR" : outcome.Applied ? "RENAMED" : outcome.NeedsRename ? "WOULD RENAME" : "SKIPPED",
                outcome.OriginalPath,
                outcome.ProposedPath,
                outcome.Error]));
    }
}
