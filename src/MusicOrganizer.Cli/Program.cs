using System.CommandLine;
using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MusicOrganizer.Application;
using MusicOrganizer.Application.Deduplication;
using MusicOrganizer.Application.Journal;
using MusicOrganizer.Application.Recovery;
using MusicOrganizer.Application.Renaming;
using MusicOrganizer.Application.Scanning;
using MusicOrganizer.Domain.Deduplication;
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

var recoverPathArgument = new Argument<string>("path") { Description = "Root folder to scan for MP3 files" };
recoverPathArgument.Validators.Add(result =>
{
    var path = result.GetValueOrDefault<string>();
    if (path is null || !Directory.Exists(path))
    {
        result.AddError($"Folder not found: {path}");
    }
});

var applyOption = new Option<bool>("--apply") { Description = "Actually write changes (default is dry-run: report only)" };

var recoverTagsCommand = new Command("recover-tags", "Recover missing Artist/Title tags from file names");
recoverTagsCommand.Arguments.Add(recoverPathArgument);
recoverTagsCommand.Options.Add(applyOption);
recoverTagsCommand.SetAction(async (parseResult, cancellationToken) =>
{
    var path = parseResult.GetValue(recoverPathArgument)!;
    var apply = parseResult.GetValue(applyOption);
    var runId = Guid.NewGuid();
    var recoveryService = host.Services.GetRequiredService<TagRecoveryService>();

    var total = 0;
    var recovered = 0;
    var failed = 0;
    var needsManualReview = new List<string>();

    await foreach (var outcome in recoveryService.RecoverAsync(path, runId, dryRun: !apply, cancellationToken))
    {
        total++;
        if (outcome.Error is not null)
        {
            failed++;
            Console.WriteLine($"[ERROR]     {outcome.FilePath} — {outcome.Error}");
        }
        else if (outcome.HasRecovery)
        {
            recovered++;
            var fields = string.Join(", ", outcome.RecoveredFields);
            var verb = apply ? "RECOVERED" : "WOULD RECOVER";
            Console.WriteLine($"[{verb}] {outcome.FilePath} — {fields}");
        }

        if (outcome.NeedsManualReview)
        {
            needsManualReview.Add(outcome.FilePath);
        }
    }

    if (needsManualReview.Count > 0)
    {
        Console.WriteLine($"[MANUAL REVIEW NEEDED] ({needsManualReview.Count} file(s) - no source could recover tags)");
        foreach (var filePath in needsManualReview)
        {
            Console.WriteLine($"  {filePath}");
        }
    }

    Console.WriteLine($"Scanned {total} file(s): {recovered} recovered, {failed} error(s), {needsManualReview.Count} need manual review.");
    if (apply && recovered > 0)
    {
        Console.WriteLine($"Run id (use with 'rollback' to undo): {runId}");
    }

    return failed == 0 ? 0 : 1;
});

var renamePathArgument = new Argument<string>("path") { Description = "Root folder to scan for MP3 files" };
renamePathArgument.Validators.Add(result =>
{
    var path = result.GetValueOrDefault<string>();
    if (path is null || !Directory.Exists(path))
    {
        result.AddError($"Folder not found: {path}");
    }
});

var renameApplyOption = new Option<bool>("--apply") { Description = "Actually rename files (default is dry-run: report only)" };

var renameCommand = new Command("rename", "Rename files to 'Artist-Title.mp3' using their tags");
renameCommand.Arguments.Add(renamePathArgument);
renameCommand.Options.Add(renameApplyOption);
renameCommand.SetAction(async (parseResult, cancellationToken) =>
{
    var path = parseResult.GetValue(renamePathArgument)!;
    var apply = parseResult.GetValue(renameApplyOption);
    var runId = Guid.NewGuid();
    var renameEngine = host.Services.GetRequiredService<RenameEngine>();

    var total = 0;
    var renamed = 0;
    var failed = 0;

    await foreach (var outcome in renameEngine.RenameAsync(path, runId, dryRun: !apply, cancellationToken))
    {
        total++;
        if (outcome.Error is not null)
        {
            failed++;
            Console.WriteLine($"[ERROR]       {outcome.OriginalPath} — {outcome.Error}");
        }
        else if (outcome.NeedsRename)
        {
            renamed++;
            var verb = apply ? "RENAMED" : "WOULD RENAME";
            Console.WriteLine($"[{verb}] {outcome.OriginalPath} -> {outcome.ProposedPath}");
        }
    }

    Console.WriteLine($"Scanned {total} file(s): {renamed} renamed, {failed} error(s).");
    if (apply && renamed > 0)
    {
        Console.WriteLine($"Run id (use with 'rollback' to undo): {runId}");
    }

    return failed == 0 ? 0 : 1;
});

var findDuplicatesPathArgument = new Argument<string>("path") { Description = "Root folder to scan for MP3 files" };
findDuplicatesPathArgument.Validators.Add(result =>
{
    var path = result.GetValueOrDefault<string>();
    if (path is null || !Directory.Exists(path))
    {
        result.AddError($"Folder not found: {path}");
    }
});

var findDuplicatesCommand = new Command("find-duplicates", "Find duplicate MP3 files by exact content and by matching tags");
findDuplicatesCommand.Arguments.Add(findDuplicatesPathArgument);
findDuplicatesCommand.SetAction(async (parseResult, cancellationToken) =>
{
    var path = parseResult.GetValue(findDuplicatesPathArgument)!;
    var duplicateFinder = host.Services.GetRequiredService<IDuplicateFinder>();

    var groups = await duplicateFinder.FindAsync(path, cancellationToken);
    var exactGroups = groups.Where(g => g.Kind == DuplicateMatchKind.Exact).ToList();
    var tagMatchGroups = groups.Where(g => g.Kind == DuplicateMatchKind.TagMatch).ToList();

    var wastedBytes = 0L;
    foreach (var group in exactGroups)
    {
        var groupWastedBytes = (group.FilePaths.Count - 1) * (group.FileSize ?? 0);
        wastedBytes += groupWastedBytes;
        Console.WriteLine($"[EXACT]     {group.FilePaths.Count} file(s), {FormatBytes(group.FileSize ?? 0)} each ({FormatBytes(groupWastedBytes)} wasted):");
        foreach (var filePath in group.FilePaths)
        {
            Console.WriteLine($"            {filePath}");
        }
    }

    foreach (var group in tagMatchGroups)
    {
        Console.WriteLine($"[TAG MATCH] {group.FilePaths.Count} file(s) with matching Artist/Title:");
        foreach (var filePath in group.FilePaths)
        {
            Console.WriteLine($"            {filePath}");
        }
    }

    Console.WriteLine(
        $"Found {exactGroups.Count} exact group(s) ({FormatBytes(wastedBytes)} wasted) " +
        $"and {tagMatchGroups.Count} tag-match group(s).");

    return 0;
});

static string FormatBytes(long bytes) => (bytes / 1024.0 / 1024.0).ToString("0.00", CultureInfo.InvariantCulture) + " MB";

var removeDuplicatesPathArgument = new Argument<string>("path") { Description = "Root folder to scan for MP3 files" };
removeDuplicatesPathArgument.Validators.Add(result =>
{
    var path = result.GetValueOrDefault<string>();
    if (path is null || !Directory.Exists(path))
    {
        result.AddError($"Folder not found: {path}");
    }
});

var removeDuplicatesApplyOption = new Option<bool>("--apply") { Description = "Actually delete files (default is dry-run: report only)" };

var removeDuplicatesCommand = new Command("remove-duplicates", "Delete exact-duplicate files, keeping the one with the shortest path in each group");
removeDuplicatesCommand.Arguments.Add(removeDuplicatesPathArgument);
removeDuplicatesCommand.Options.Add(removeDuplicatesApplyOption);
removeDuplicatesCommand.SetAction(async (parseResult, cancellationToken) =>
{
    var path = parseResult.GetValue(removeDuplicatesPathArgument)!;
    var apply = parseResult.GetValue(removeDuplicatesApplyOption);
    var runId = Guid.NewGuid();
    var removalService = host.Services.GetRequiredService<DuplicateRemovalService>();

    var total = 0;
    var removed = 0;
    var failed = 0;

    await foreach (var outcome in removalService.RemoveAsync(path, runId, dryRun: !apply, cancellationToken))
    {
        total++;
        if (outcome.Error is not null)
        {
            failed++;
            Console.WriteLine($"[ERROR]        {outcome.FilePath} — {outcome.Error}");
        }
        else
        {
            removed++;
            var verb = apply ? "REMOVED" : "WOULD REMOVE";
            Console.WriteLine($"[{verb}] {outcome.FilePath} (keeping {outcome.KeptFilePath})");
        }
    }

    Console.WriteLine($"Processed {total} duplicate file(s): {removed} removed, {failed} error(s).");
    Console.WriteLine("Tag-match groups (matching tags, different content) are never removed automatically — review with 'find-duplicates'.");
    if (apply && removed > 0)
    {
        Console.WriteLine($"Run id (use with 'rollback' to undo): {runId}");
    }

    return failed == 0 ? 0 : 1;
});

var runIdArgument = new Argument<Guid>("run-id") { Description = "Run id printed by 'recover-tags --apply', 'rename --apply', or 'remove-duplicates --apply'" };

var rollbackCommand = new Command("rollback", "Restore files backed up during a previous run");
rollbackCommand.Arguments.Add(runIdArgument);
rollbackCommand.SetAction(async (parseResult, cancellationToken) =>
{
    var runId = parseResult.GetValue(runIdArgument);
    var rollbackUseCase = host.Services.GetRequiredService<RollbackRunUseCase>();

    var restored = 0;

    await foreach (var entry in rollbackUseCase.RollbackAsync(runId, cancellationToken))
    {
        restored++;
        Console.WriteLine($"[RESTORED] {entry.OriginalPath}");
    }

    Console.WriteLine($"Restored {restored} file(s) from run {runId}.");
    return restored > 0 ? 0 : 1;
});

var rootCommand = new RootCommand("MusicOrganizer - professional MP3 collection manager");
rootCommand.Subcommands.Add(scanCommand);
rootCommand.Subcommands.Add(recoverTagsCommand);
rootCommand.Subcommands.Add(renameCommand);
rootCommand.Subcommands.Add(findDuplicatesCommand);
rootCommand.Subcommands.Add(removeDuplicatesCommand);
rootCommand.Subcommands.Add(rollbackCommand);

return await rootCommand.Parse(args).InvokeAsync();
