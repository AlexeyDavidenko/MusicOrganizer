using Avalonia.Controls;
using Avalonia.Interactivity;
using MusicOrganizer.Application.Reporting;
using MusicOrganizer.Gui.ViewModels;

namespace MusicOrganizer.Gui.Views;

/// <summary>
/// View for <see cref="RollbackViewModel"/>.
/// </summary>
public partial class RollbackView : UserControl
{
    /// <summary>
    /// Creates a new <see cref="RollbackView"/>.
    /// </summary>
    public RollbackView()
    {
        InitializeComponent();
    }

    private async void OnSaveResultsClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not RollbackViewModel viewModel)
        {
            return;
        }

        await ResultsExporter.SaveAsync(
            this,
            viewModel.Results,
            entry => $"{entry.OperationType} — {entry.OriginalPath}",
            "Rollback",
            ["OperationType", "OriginalPath"],
            entry => new ReportRow([entry.OperationType, entry.OriginalPath]));
    }
}
