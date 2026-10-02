using Avalonia.Controls;

namespace MusicOrganizer.Gui.Views;

/// <summary>
/// Main application window: a sidebar of commands and the selected screen's content.
/// </summary>
public partial class MainWindow : Window
{
    /// <summary>
    /// Creates a new <see cref="MainWindow"/>.
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();
    }
}
