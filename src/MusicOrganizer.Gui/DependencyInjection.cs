using Microsoft.Extensions.DependencyInjection;
using MusicOrganizer.Gui.ViewModels;

namespace MusicOrganizer.Gui;

/// <summary>
/// Registers GUI layer services (view models) with the dependency injection container.
/// </summary>
internal static class DependencyInjection
{
    /// <summary>
    /// Adds every view model to the service collection.
    /// </summary>
    /// <param name="services">The service collection to register services into.</param>
    /// <returns>The same service collection, for chaining.</returns>
    public static IServiceCollection AddGuiViewModels(this IServiceCollection services)
    {
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<ScanViewModel>();
        services.AddSingleton<RecoverTagsViewModel>();
        services.AddSingleton<FixEncodingViewModel>();
        services.AddSingleton<RenameViewModel>();
        services.AddSingleton<OrganizeViewModel>();
        services.AddSingleton<FindDuplicatesViewModel>();
        services.AddSingleton<RemoveDuplicatesViewModel>();
        services.AddSingleton<CleanJournalViewModel>();
        services.AddSingleton<RollbackViewModel>();
        return services;
    }
}
