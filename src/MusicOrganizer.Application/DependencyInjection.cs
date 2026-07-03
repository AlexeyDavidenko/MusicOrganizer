using Microsoft.Extensions.DependencyInjection;
using MusicOrganizer.Application.Deduplication;
using MusicOrganizer.Application.Journal;
using MusicOrganizer.Application.Recovery;
using MusicOrganizer.Application.Renaming;
using MusicOrganizer.Application.Scanning;

namespace MusicOrganizer.Application;

/// <summary>
/// Registers Application layer services with the dependency injection container.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds Application layer services (use cases, validators, mapping) to the service collection.
    /// </summary>
    /// <param name="services">The service collection to register services into.</param>
    /// <returns>The same service collection, for chaining.</returns>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddTransient<CollectionScanner>();
        services.AddTransient<TagRecoveryService>();
        services.AddTransient<RollbackRunUseCase>();
        services.AddTransient<RenameEngine>();
        services.AddTransient<DuplicateFinder>();
        return services;
    }
}
