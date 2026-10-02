using Avalonia.Controls;
using Avalonia.Interactivity;
using MusicOrganizer.Gui.ViewModels;

namespace MusicOrganizer.Gui.Views;

/// <summary>
/// View for <see cref="FindDuplicatesViewModel"/>.
/// </summary>
public partial class FindDuplicatesView : UserControl
{
    /// <summary>
    /// Creates a new <see cref="FindDuplicatesView"/>.
    /// </summary>
    public FindDuplicatesView()
    {
        InitializeComponent();
    }

    private async void OnBrowseFolderClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not FindDuplicatesViewModel viewModel)
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
        if (DataContext is not FindDuplicatesViewModel viewModel)
        {
            return;
        }

        var lines = new List<string>();
        foreach (var group in viewModel.ExactGroups)
        {
            lines.Add($"[EXACT]     {group.FilePaths.Count} file(s):");
            lines.AddRange(group.FilePaths.Select(path => $"            {path}"));
        }

        foreach (var group in viewModel.TagMatchGroups)
        {
            lines.Add($"[TAG MATCH] {group.FilePaths.Count} file(s):");
            lines.AddRange(group.FilePaths.Select(path => $"            {path}"));
        }

        await ResultsExporter.SaveAsync(this, lines, line => line);
    }
}
