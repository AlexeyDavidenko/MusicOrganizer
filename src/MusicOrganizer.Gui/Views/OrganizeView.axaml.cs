using Avalonia.Controls;
using Avalonia.Interactivity;
using MusicOrganizer.Application.Reporting;
using MusicOrganizer.Gui.ViewModels;

namespace MusicOrganizer.Gui.Views;

/// <summary>
/// View for <see cref="OrganizeViewModel"/>.
/// </summary>
public partial class OrganizeView : UserControl
{
    /// <summary>
    /// Creates a new <see cref="OrganizeView"/>.
    /// </summary>
    public OrganizeView()
    {
        InitializeComponent();
    }

    private async void OnBrowseFolderClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not OrganizeViewModel viewModel)
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
        if (DataContext is not OrganizeViewModel viewModel)
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
                    : outcome.Applied ? "[MOVED]" : outcome.NeedsRename ? "[WOULD MOVE]" : "[SKIPPED]";
                return $"{status} {outcome.OriginalPath} -> {outcome.ProposedPath}";
            },
            "Organize",
            ["Status", "OriginalPath", "ProposedPath", "Error"],
            outcome => new ReportRow([
                outcome.Error is not null ? "ERROR" : outcome.Applied ? "MOVED" : outcome.NeedsRename ? "WOULD MOVE" : "SKIPPED",
                outcome.OriginalPath,
                outcome.ProposedPath,
                outcome.Error]));
    }
}
