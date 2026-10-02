using Avalonia.Controls;
using Avalonia.Interactivity;
using MusicOrganizer.Gui.ViewModels;

namespace MusicOrganizer.Gui.Views;

/// <summary>
/// View for <see cref="CleanJournalViewModel"/>.
/// </summary>
public partial class CleanJournalView : UserControl
{
    /// <summary>
    /// Creates a new <see cref="CleanJournalView"/>.
    /// </summary>
    public CleanJournalView()
    {
        InitializeComponent();
    }

    private async void OnSaveResultsClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not CleanJournalViewModel viewModel)
        {
            return;
        }

        await ResultsExporter.SaveAsync(this, viewModel.Results, outcome =>
        {
            var status = outcome.Applied ? "[DELETED]" : "[WOULD DELETE]";
            return $"{status} {outcome.RunId} — last activity {outcome.LastActivityAtUtc:u}, {outcome.EntryCount} entry(ies)";
        });
    }
}
