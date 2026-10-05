using System.Globalization;
using Avalonia.Controls;
using Avalonia.Interactivity;
using MusicOrganizer.Application.Reporting;
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

        var groups = viewModel.ExactGroups
            .Select(group => (Kind: "EXACT", Header: $"[EXACT]     {group.FilePaths.Count} file(s):", Group: group))
            .Concat(viewModel.TagMatchGroups.Select(group => (Kind: "TAG MATCH", Header: $"[TAG MATCH] {group.FilePaths.Count} file(s):", Group: group)))
            .ToList();

        await ResultsExporter.SaveAsync(
            this,
            groups,
            item => string.Join('\n', new[] { item.Header }.Concat(item.Group.FilePaths.Select(path => $"            {path}"))),
            "Find Duplicates",
            ["Kind", "FileCount", "Files"],
            item => new ReportRow([item.Kind, item.Group.FilePaths.Count.ToString(CultureInfo.InvariantCulture), string.Join("; ", item.Group.FilePaths)]));
    }
}
