using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MusicOrganizer.Application;
using MusicOrganizer.Application.Scanning;
using MusicOrganizer.Infrastructure;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices();

using var host = builder.Build();

var pathArgument = new Argument<string>("path") { Description = "Root folder to scan for MP3 files" };
pathArgument.Validators.Add(result =>
{
    var path = result.GetValueOrDefault<string>();
    if (path is null || !Directory.Exists(path))
    {
        result.AddError($"Folder not found: {path}");
    }
});

var scanCommand = new Command("scan", "Recursively scan a folder for MP3 files and report their tags");
scanCommand.Arguments.Add(pathArgument);
scanCommand.SetAction(async (parseResult, cancellationToken) =>
{
    var path = parseResult.GetValue(pathArgument)!;
    var scanner = host.Services.GetRequiredService<CollectionScanner>();

    var total = 0;
    var tagged = 0;
    var failed = 0;

    await foreach (var entry in scanner.ScanAsync(path, cancellationToken))
    {
        total++;
        if (entry.Succeeded)
        {
            tagged++;
            Console.WriteLine($"[OK]    {entry.FilePath} — {entry.Tags!.Artist ?? "?"} - {entry.Tags.Title ?? "?"}");
        }
        else
        {
            failed++;
            Console.WriteLine($"[ERROR] {entry.FilePath} — {entry.Error}");
        }
    }

    Console.WriteLine($"Scanned {total} file(s): {tagged} with tags, {failed} error(s).");
    return failed == 0 ? 0 : 1;
});

var rootCommand = new RootCommand("MusicOrganizer - professional MP3 collection manager");
rootCommand.Subcommands.Add(scanCommand);

return await rootCommand.Parse(args).InvokeAsync();
