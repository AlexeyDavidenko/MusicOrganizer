using Avalonia.Controls;
using Avalonia.Interactivity;
using MusicOrganizer.Gui.ViewModels;

namespace MusicOrganizer.Gui.Views;

/// <summary>
/// View for <see cref="ScanViewModel"/>.
/// </summary>
public partial class ScanView : UserControl
{
    /// <summary>
    /// Creates a new <see cref="ScanView"/>.
    /// </summary>
    public ScanView()
    {
        InitializeComponent();
    }

    private async void OnBrowseFolderClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not ScanViewModel viewModel)
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
        if (DataContext is not ScanViewModel viewModel)
        {
            return;
        }

        await ResultsExporter.SaveAsync(this, viewModel.Results, entry =>
            entry.Succeeded
                ? $"[OK]    {entry.FilePath} — {entry.Tags!.Artist ?? "?"} - {entry.Tags.Title ?? "?"}"
                : $"[ERROR] {entry.FilePath} — {entry.Error}");
    }
}
