using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using MusicOrganizer.Gui.ViewModels;
using MusicOrganizer.Gui.Views;

namespace MusicOrganizer.Gui;

/// <summary>
/// Avalonia application entry point.
/// </summary>
public partial class App : global::Avalonia.Application
{
    /// <summary>
    /// The composition root's service provider, set by <see cref="Program.Main"/> before Avalonia's
    /// lifetime starts. Avalonia constructs <see cref="App"/> itself (no constructor injection
    /// available), so this static handoff is the simplest way to reach the DI container from
    /// <see cref="OnFrameworkInitializationCompleted"/>.
    /// </summary>
    public static IServiceProvider Services { get; set; } = null!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    /// <inheritdoc/>
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = Services.GetRequiredService<MainWindowViewModel>(),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
