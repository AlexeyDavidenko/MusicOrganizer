using Microsoft.Extensions.DependencyInjection;
using MusicOrganizer.Application.Deduplication;
using MusicOrganizer.Application.Journal;
using MusicOrganizer.Application.Recovery;
using MusicOrganizer.Application.Renaming;
using MusicOrganizer.Application.Scanning;
using MusicOrganizer.Infrastructure.Deduplication;
using MusicOrganizer.Infrastructure.FileSystem;
using MusicOrganizer.Infrastructure.Journal;
using MusicOrganizer.Infrastructure.Renaming;
using MusicOrganizer.Infrastructure.Tags;

namespace MusicOrganizer.Infrastructure;

/// <summary>
/// Registers Infrastructure layer services with the dependency injection container.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds Infrastructure layer services (file system access, tag readers/writers, journal storage) to the service collection.
    /// </summary>
    /// <param name="services">The service collection to register services into.</param>
    /// <returns>The same service collection, for chaining.</returns>
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        services.AddTransient<IFileSystemScanner, FileSystemScanner>();
        services.AddTransient<IAudioTagReader, Mp3TagReader>();
        services.AddTransient<ITagWriter, Mp3TagWriter>();
        services.AddTransient<IOperationJournal, FileBackupJournal>();
        services.AddTransient<IFileRenamer, FileRenamer>();
        services.AddTransient<IDuplicateFileInspector, FileContentInspector>();
        services.AddTransient<IFileRemover, FileRemover>();
        return services;
    }
}
