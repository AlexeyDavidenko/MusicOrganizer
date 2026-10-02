namespace MusicOrganizer.Gui.ViewModels;

/// <summary>
/// One entry in the main window's sidebar: a display title paired with the screen it navigates to.
/// </summary>
/// <param name="Title">Text shown in the sidebar.</param>
/// <param name="ViewModel">Screen shown when this entry is selected.</param>
public sealed record NavigationItem(string Title, ViewModelBase ViewModel);
