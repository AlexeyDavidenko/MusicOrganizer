using System.CommandLine;
using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MusicOrganizer.Application;
using MusicOrganizer.Application.Deduplication;
using MusicOrganizer.Application.Encoding;
using MusicOrganizer.Application.Journal;
using MusicOrganizer.Application.Recovery;
using MusicOrganizer.Application.Renaming;
using MusicOrganizer.Application.Reporting;
using MusicOrganizer.Application.Scanning;
using MusicOrganizer.Domain.Deduplication;
using MusicOrganizer.Infrastructure;
using MusicOrganizer.Shared;

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

var reportOption = new Option<string?>("--report") { Description = "Also write this command's output to a plain-text report file (overwritten each run)" };
reportOption.Validators.Add(result =>
{
    var reportPath = result.GetValueOrDefault<string?>();
    if (reportPath is null)
    {
        return;
    }

    var directory = Path.GetDirectoryName(reportPath);
    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
    {
        result.AddError($"Report file directory not found: {directory}");
    }
});

static (TextWriter Output, StreamWriter? ReportWriter) CreateOutputWriter(string? reportPath)
{
    if (reportPath is null)
    {
        return (Console.Out, null);
    }

    var reportWriter = new StreamWriter(reportPath, append: false) { AutoFlush = true };
    return (new TeeTextWriter(Console.Out, reportWriter), reportWriter);
}

var formatOption = new Option<ReportFormat>("--format") { Description = "Report file format when --report is used: plain (default, live console tee), json, csv, or html" };
formatOption.DefaultValueFactory = _ => ReportFormat.PlainText;

static bool TryValidateReportFormat(string? reportPath, ReportFormat format)
{
    if (reportPath is null && format != ReportFormat.PlainText)
    {
        Console.Error.WriteLine("--format requires --report <path>.");
        return false;
    }

    return true;
}

// For ReportFormat.PlainText, reports are written incrementally by CreateOutputWriter's live
// TeeTextWriter (unchanged ADR-0014 behavior) and this always returns null. For any other format,
// returns the list rows should be appended to during the command's loop, flushed as one
// ReportDocument in FlushStructuredReportAsync.
static List<ReportRow>? CreateRowSink(string? reportPath, ReportFormat format) =>
    reportPath is not null && format != ReportFormat.PlainText ? [] : null;

// Runs from a `finally` block, so it executes even when the command's loop was cancelled midway -
// whatever rows were collected before cancellation are still written, matching the partial-progress
// guarantee the live plain-text tee already gives. Uses CancellationToken.None deliberately: the
// caller's token may already be cancelled by the time this runs, and a cancelled write here would
// silently drop that partial progress.
static async Task FlushStructuredReportAsync(
    string? reportPath,
    ReportFormat format,
    List<ReportRow>? rows,
    string title,
    IReadOnlyList<string> columns,
    IReadOnlyList<string> summary,
    IEnumerable<IReportWriter> writers)
{
    if (reportPath is null || rows is null)
    {
        return;
    }

    var document = new ReportDocument { Title = title, Columns = columns, Rows = rows, Summary = summary };
    var writer = writers.Single(w => w.Format == format);
    await using var stream = new FileStream(reportPath, FileMode.Create, FileAccess.Write);
    await writer.WriteAsync(document, stream, CancellationToken.None);
}

var scanCommand = new Command("scan", "Recursively scan a folder for MP3 files and report their tags");
scanCommand.Arguments.Add(pathArgument);
scanCommand.Options.Add(reportOption);
scanCommand.Options.Add(formatOption);
scanCommand.SetAction(async (parseResult, cancellationToken) =>
{
    var path = parseResult.GetValue(pathArgument)!;
    var reportPath = parseResult.GetValue(reportOption);
    var format = parseResult.GetValue(formatOption);
    if (!TryValidateReportFormat(reportPath, format))
    {
        return 1;
    }

    var (output, reportWriter) = CreateOutputWriter(format == ReportFormat.PlainText ? reportPath : null);
    var rows = CreateRowSink(reportPath, format);
    try
    {
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
                output.WriteLine($"[OK]    {entry.FilePath} — {entry.Tags!.Artist ?? "?"} - {entry.Tags.Title ?? "?"}");
                rows?.Add(new ReportRow(["OK", entry.FilePath, entry.Tags!.Artist, entry.Tags.Title, null]));
            }
            else
            {
                failed++;
                output.WriteLine($"[ERROR] {entry.FilePath} — {entry.Error}");
                rows?.Add(new ReportRow(["ERROR", entry.FilePath, null, null, entry.Error]));
            }
        }

        output.WriteLine($"Scanned {total} file(s): {tagged} with tags, {failed} error(s).");
        await FlushStructuredReportAsync(
            reportPath,
            format,
            rows,
            "Scan",
            ["Status", "FilePath", "Artist", "Title", "Error"],
            [$"Scanned {total} file(s): {tagged} with tags, {failed} error(s)."],
            host.Services.GetRequiredService<IEnumerable<IReportWriter>>());
        return failed == 0 ? 0 : 1;
    }
    finally
    {
        reportWriter?.Dispose();
    }
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
recoverTagsCommand.Options.Add(reportOption);
recoverTagsCommand.Options.Add(formatOption);
recoverTagsCommand.SetAction(async (parseResult, cancellationToken) =>
{
    var path = parseResult.GetValue(recoverPathArgument)!;
    var apply = parseResult.GetValue(applyOption);
    var reportPath = parseResult.GetValue(reportOption);
    var format = parseResult.GetValue(formatOption);
    if (!TryValidateReportFormat(reportPath, format))
    {
        return 1;
    }

    var (output, reportWriter) = CreateOutputWriter(format == ReportFormat.PlainText ? reportPath : null);
    var rows = CreateRowSink(reportPath, format);
    try
    {
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
                output.WriteLine($"[ERROR]     {outcome.FilePath} — {outcome.Error}");
                rows?.Add(new ReportRow(["ERROR", outcome.FilePath, null, outcome.Error, outcome.NeedsManualReview.ToString()]));
            }
            else if (outcome.HasRecovery)
            {
                recovered++;
                var fields = string.Join(", ", outcome.RecoveredFields);
                var verb = apply ? "RECOVERED" : "WOULD RECOVER";
                output.WriteLine($"[{verb}] {outcome.FilePath} — {fields}");
                rows?.Add(new ReportRow([apply ? "RECOVERED" : "WOULD RECOVER", outcome.FilePath, fields, null, outcome.NeedsManualReview.ToString()]));
            }
            else
            {
                rows?.Add(new ReportRow(["UNCHANGED", outcome.FilePath, null, null, outcome.NeedsManualReview.ToString()]));
            }

            if (outcome.NeedsManualReview)
            {
                needsManualReview.Add(outcome.FilePath);
            }
        }

        if (needsManualReview.Count > 0)
        {
            output.WriteLine($"[MANUAL REVIEW NEEDED] ({needsManualReview.Count} file(s) - no source could recover tags)");
            foreach (var filePath in needsManualReview)
            {
                output.WriteLine($"  {filePath}");
            }
        }

        output.WriteLine($"Scanned {total} file(s): {recovered} recovered, {failed} error(s), {needsManualReview.Count} need manual review.");
        if (apply && recovered > 0)
        {
            output.WriteLine($"Run id (use with 'rollback' to undo): {runId}");
        }

        await FlushStructuredReportAsync(
            reportPath,
            format,
            rows,
            "Recover Tags",
            ["Status", "FilePath", "RecoveredFields", "Error", "NeedsManualReview"],
            [$"Scanned {total} file(s): {recovered} recovered, {failed} error(s), {needsManualReview.Count} need manual review."],
            host.Services.GetRequiredService<IEnumerable<IReportWriter>>());
        return failed == 0 ? 0 : 1;
    }
    finally
    {
        reportWriter?.Dispose();
    }
});

var fixEncodingPathArgument = new Argument<string>("path") { Description = "Root folder to scan for MP3 files" };
fixEncodingPathArgument.Validators.Add(result =>
{
    var path = result.GetValueOrDefault<string>();
    if (path is null || !Directory.Exists(path))
    {
        result.AddError($"Folder not found: {path}");
    }
});

var fixEncodingApplyOption = new Option<bool>("--apply") { Description = "Actually write corrected tags (default is dry-run: report only)" };

var fixEncodingCommand = new Command("fix-encoding", "Detect and correct tag text mis-decoded as Latin1 (legacy Cyrillic encodings, Windows-1252 punctuation)");
fixEncodingCommand.Arguments.Add(fixEncodingPathArgument);
fixEncodingCommand.Options.Add(fixEncodingApplyOption);
fixEncodingCommand.Options.Add(reportOption);
fixEncodingCommand.Options.Add(formatOption);
fixEncodingCommand.SetAction(async (parseResult, cancellationToken) =>
{
    var path = parseResult.GetValue(fixEncodingPathArgument)!;
    var apply = parseResult.GetValue(fixEncodingApplyOption);
    var reportPath = parseResult.GetValue(reportOption);
    var format = parseResult.GetValue(formatOption);
    if (!TryValidateReportFormat(reportPath, format))
    {
        return 1;
    }

    var (output, reportWriter) = CreateOutputWriter(format == ReportFormat.PlainText ? reportPath : null);
    var rows = CreateRowSink(reportPath, format);
    try
    {
        var runId = Guid.NewGuid();
        var encodingFixService = host.Services.GetRequiredService<EncodingFixService>();

        var total = 0;
        var fixedCount = 0;
        var failed = 0;
        var needsManualReview = new List<string>();

        await foreach (var outcome in encodingFixService.FixAsync(path, runId, dryRun: !apply, cancellationToken))
        {
            total++;
            if (outcome.Error is not null)
            {
                failed++;
                output.WriteLine($"[ERROR]  {outcome.FilePath} — {outcome.Error}");
                rows?.Add(new ReportRow(["ERROR", outcome.FilePath, null, outcome.Error, outcome.NeedsManualReview.ToString()]));
            }
            else if (outcome.HasFix)
            {
                fixedCount++;
                var fields = string.Join(", ", outcome.FixedFields);
                var verb = apply ? "FIXED" : "WOULD FIX";
                output.WriteLine($"[{verb}] {outcome.FilePath} — {fields}");
                rows?.Add(new ReportRow([apply ? "FIXED" : "WOULD FIX", outcome.FilePath, fields, null, outcome.NeedsManualReview.ToString()]));
            }
            else
            {
                rows?.Add(new ReportRow(["UNCHANGED", outcome.FilePath, null, null, outcome.NeedsManualReview.ToString()]));
            }

            if (outcome.NeedsManualReview)
            {
                needsManualReview.Add(outcome.FilePath);
            }
        }

        if (needsManualReview.Count > 0)
        {
            output.WriteLine($"[MANUAL REVIEW NEEDED] ({needsManualReview.Count} file(s) - suspicious text, no encoding matched confidently)");
            foreach (var filePath in needsManualReview)
            {
                output.WriteLine($"  {filePath}");
            }
        }

        output.WriteLine($"Scanned {total} file(s): {fixedCount} fixed, {failed} error(s), {needsManualReview.Count} need manual review.");
        if (apply && fixedCount > 0)
        {
            output.WriteLine($"Run id (use with 'rollback' to undo): {runId}");
        }

        await FlushStructuredReportAsync(
            reportPath,
            format,
            rows,
            "Fix Encoding",
            ["Status", "FilePath", "FixedFields", "Error", "NeedsManualReview"],
            [$"Scanned {total} file(s): {fixedCount} fixed, {failed} error(s), {needsManualReview.Count} need manual review."],
            host.Services.GetRequiredService<IEnumerable<IReportWriter>>());
        return failed == 0 ? 0 : 1;
    }
    finally
    {
        reportWriter?.Dispose();
    }
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
var renameTransliterateOption = new Option<bool>("--transliterate") { Description = "Transliterate Cyrillic Artist/Title to Latin (BGN/PCGN) before building the file name" };

var renameCommand = new Command("rename", "Rename files to 'Artist-Title.mp3' using their tags");
renameCommand.Arguments.Add(renamePathArgument);
renameCommand.Options.Add(renameApplyOption);
renameCommand.Options.Add(renameTransliterateOption);
renameCommand.Options.Add(reportOption);
renameCommand.Options.Add(formatOption);
renameCommand.SetAction(async (parseResult, cancellationToken) =>
{
    var path = parseResult.GetValue(renamePathArgument)!;
    var apply = parseResult.GetValue(renameApplyOption);
    var transliterate = parseResult.GetValue(renameTransliterateOption);
    var reportPath = parseResult.GetValue(reportOption);
    var format = parseResult.GetValue(formatOption);
    if (!TryValidateReportFormat(reportPath, format))
    {
        return 1;
    }

    var (output, reportWriter) = CreateOutputWriter(format == ReportFormat.PlainText ? reportPath : null);
    var rows = CreateRowSink(reportPath, format);
    try
    {
        var runId = Guid.NewGuid();
        var renameEngine = host.Services.GetRequiredService<RenameEngine>();

        var total = 0;
        var renamed = 0;
        var failed = 0;

        await foreach (var outcome in renameEngine.RenameAsync(path, runId, dryRun: !apply, transliterate, cancellationToken))
        {
            total++;
            if (outcome.Error is not null)
            {
                failed++;
                output.WriteLine($"[ERROR]       {outcome.OriginalPath} — {outcome.Error}");
                rows?.Add(new ReportRow(["ERROR", outcome.OriginalPath, null, outcome.Error]));
            }
            else if (outcome.NeedsRename)
            {
                renamed++;
                var verb = apply ? "RENAMED" : "WOULD RENAME";
                output.WriteLine($"[{verb}] {outcome.OriginalPath} -> {outcome.ProposedPath}");
                rows?.Add(new ReportRow([apply ? "RENAMED" : "WOULD RENAME", outcome.OriginalPath, outcome.ProposedPath, null]));
            }
            else
            {
                rows?.Add(new ReportRow(["SKIPPED", outcome.OriginalPath, outcome.ProposedPath, null]));
            }
        }

        output.WriteLine($"Scanned {total} file(s): {renamed} renamed, {failed} error(s).");
        if (apply && renamed > 0)
        {
            output.WriteLine($"Run id (use with 'rollback' to undo): {runId}");
        }

        await FlushStructuredReportAsync(
            reportPath,
            format,
            rows,
            "Rename",
            ["Status", "OriginalPath", "ProposedPath", "Error"],
            [$"Scanned {total} file(s): {renamed} renamed, {failed} error(s)."],
            host.Services.GetRequiredService<IEnumerable<IReportWriter>>());
        return failed == 0 ? 0 : 1;
    }
    finally
    {
        reportWriter?.Dispose();
    }
});

var organizePathArgument = new Argument<string>("path") { Description = "Root folder to scan for MP3 files" };
organizePathArgument.Validators.Add(result =>
{
    var path = result.GetValueOrDefault<string>();
    if (path is null || !Directory.Exists(path))
    {
        result.AddError($"Folder not found: {path}");
    }
});

var organizeApplyOption = new Option<bool>("--apply") { Description = "Actually move files (default is dry-run: report only)" };
var organizePruneOption = new Option<bool>("--prune-empty-folders") { Description = "Remove folders left empty after moving their files out (only with --apply; each removal is journaled and reversible via 'rollback')" };

var organizeCommand = new Command("organize", "Move files into an Artist/Album folder tree under the root (falls back to Artist-only when Album is unknown); file names are left as-is");
organizeCommand.Arguments.Add(organizePathArgument);
organizeCommand.Options.Add(organizeApplyOption);
organizeCommand.Options.Add(organizePruneOption);
organizeCommand.Options.Add(reportOption);
organizeCommand.Options.Add(formatOption);
organizeCommand.SetAction(async (parseResult, cancellationToken) =>
{
    var path = parseResult.GetValue(organizePathArgument)!;
    var apply = parseResult.GetValue(organizeApplyOption);
    var pruneEmptyFolders = parseResult.GetValue(organizePruneOption);
    var reportPath = parseResult.GetValue(reportOption);
    var format = parseResult.GetValue(formatOption);
    if (!TryValidateReportFormat(reportPath, format))
    {
        return 1;
    }

    var (output, reportWriter) = CreateOutputWriter(format == ReportFormat.PlainText ? reportPath : null);
    var rows = CreateRowSink(reportPath, format);
    try
    {
        var runId = Guid.NewGuid();
        var organizeEngine = host.Services.GetRequiredService<OrganizeEngine>();

        var total = 0;
        var moved = 0;
        var failed = 0;

        await foreach (var outcome in organizeEngine.OrganizeAsync(path, runId, dryRun: !apply, pruneEmptyFolders, cancellationToken))
        {
            total++;
            if (outcome.Error is not null)
            {
                failed++;
                output.WriteLine($"[ERROR]        {outcome.OriginalPath} — {outcome.Error}");
                rows?.Add(new ReportRow(["ERROR", outcome.OriginalPath, null, outcome.Error]));
            }
            else if (outcome.NeedsRename)
            {
                moved++;
                var verb = apply ? "MOVED" : "WOULD MOVE";
                output.WriteLine($"[{verb}] {outcome.OriginalPath} -> {outcome.ProposedPath}");
                rows?.Add(new ReportRow([apply ? "MOVED" : "WOULD MOVE", outcome.OriginalPath, outcome.ProposedPath, null]));
            }
            else
            {
                rows?.Add(new ReportRow(["SKIPPED", outcome.OriginalPath, outcome.ProposedPath, null]));
            }
        }

        output.WriteLine($"Scanned {total} file(s): {moved} moved, {failed} error(s).");
        if (apply && moved > 0)
        {
            output.WriteLine($"Run id (use with 'rollback' to undo): {runId}");
        }

        await FlushStructuredReportAsync(
            reportPath,
            format,
            rows,
            "Organize",
            ["Status", "OriginalPath", "ProposedPath", "Error"],
            [$"Scanned {total} file(s): {moved} moved, {failed} error(s)."],
            host.Services.GetRequiredService<IEnumerable<IReportWriter>>());
        return failed == 0 ? 0 : 1;
    }
    finally
    {
        reportWriter?.Dispose();
    }
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
findDuplicatesCommand.Options.Add(reportOption);
findDuplicatesCommand.Options.Add(formatOption);
findDuplicatesCommand.SetAction(async (parseResult, cancellationToken) =>
{
    var path = parseResult.GetValue(findDuplicatesPathArgument)!;
    var reportPath = parseResult.GetValue(reportOption);
    var format = parseResult.GetValue(formatOption);
    if (!TryValidateReportFormat(reportPath, format))
    {
        return 1;
    }

    var (output, reportWriter) = CreateOutputWriter(format == ReportFormat.PlainText ? reportPath : null);
    var rows = CreateRowSink(reportPath, format);
    try
    {
        var duplicateFinder = host.Services.GetRequiredService<IDuplicateFinder>();

        var groups = await duplicateFinder.FindAsync(path, cancellationToken);
        var exactGroups = groups.Where(g => g.Kind == DuplicateMatchKind.Exact).ToList();
        var tagMatchGroups = groups.Where(g => g.Kind == DuplicateMatchKind.TagMatch).ToList();

        var wastedBytes = 0L;
        foreach (var group in exactGroups)
        {
            var groupWastedBytes = (group.FilePaths.Count - 1) * (group.FileSize ?? 0);
            wastedBytes += groupWastedBytes;
            output.WriteLine($"[EXACT]     {group.FilePaths.Count} file(s), {FormatBytes(group.FileSize ?? 0)} each ({FormatBytes(groupWastedBytes)} wasted):");
            foreach (var filePath in group.FilePaths)
            {
                output.WriteLine($"            {filePath}");
            }

            rows?.Add(new ReportRow(["EXACT", group.FilePaths.Count.ToString(CultureInfo.InvariantCulture), FormatBytes(group.FileSize ?? 0), FormatBytes(groupWastedBytes), string.Join("; ", group.FilePaths)]));
        }

        foreach (var group in tagMatchGroups)
        {
            output.WriteLine($"[TAG MATCH] {group.FilePaths.Count} file(s) with matching Artist/Title:");
            foreach (var filePath in group.FilePaths)
            {
                output.WriteLine($"            {filePath}");
            }

            rows?.Add(new ReportRow(["TAG MATCH", group.FilePaths.Count.ToString(CultureInfo.InvariantCulture), null, null, string.Join("; ", group.FilePaths)]));
        }

        var summaryLine =
            $"Found {exactGroups.Count} exact group(s) ({FormatBytes(wastedBytes)} wasted) " +
            $"and {tagMatchGroups.Count} tag-match group(s).";
        output.WriteLine(summaryLine);

        await FlushStructuredReportAsync(
            reportPath,
            format,
            rows,
            "Find Duplicates",
            ["Kind", "FileCount", "SizeEach", "Wasted", "Files"],
            [summaryLine],
            host.Services.GetRequiredService<IEnumerable<IReportWriter>>());

        return 0;
    }
    finally
    {
        reportWriter?.Dispose();
    }
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
removeDuplicatesCommand.Options.Add(reportOption);
removeDuplicatesCommand.Options.Add(formatOption);
removeDuplicatesCommand.SetAction(async (parseResult, cancellationToken) =>
{
    var path = parseResult.GetValue(removeDuplicatesPathArgument)!;
    var apply = parseResult.GetValue(removeDuplicatesApplyOption);
    var reportPath = parseResult.GetValue(reportOption);
    var format = parseResult.GetValue(formatOption);
    if (!TryValidateReportFormat(reportPath, format))
    {
        return 1;
    }

    var (output, reportWriter) = CreateOutputWriter(format == ReportFormat.PlainText ? reportPath : null);
    var rows = CreateRowSink(reportPath, format);
    try
    {
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
                output.WriteLine($"[ERROR]        {outcome.FilePath} — {outcome.Error}");
                rows?.Add(new ReportRow(["ERROR", outcome.FilePath, outcome.KeptFilePath, outcome.Error]));
            }
            else
            {
                removed++;
                var verb = apply ? "REMOVED" : "WOULD REMOVE";
                output.WriteLine($"[{verb}] {outcome.FilePath} (keeping {outcome.KeptFilePath})");
                rows?.Add(new ReportRow([apply ? "REMOVED" : "WOULD REMOVE", outcome.FilePath, outcome.KeptFilePath, null]));
            }
        }

        output.WriteLine($"Processed {total} duplicate file(s): {removed} removed, {failed} error(s).");
        output.WriteLine("Tag-match groups (matching tags, different content) are never removed automatically — review with 'find-duplicates'.");
        if (apply && removed > 0)
        {
            output.WriteLine($"Run id (use with 'rollback' to undo): {runId}");
        }

        await FlushStructuredReportAsync(
            reportPath,
            format,
            rows,
            "Remove Duplicates",
            ["Status", "FilePath", "KeptFilePath", "Error"],
            [$"Processed {total} duplicate file(s): {removed} removed, {failed} error(s)."],
            host.Services.GetRequiredService<IEnumerable<IReportWriter>>());
        return failed == 0 ? 0 : 1;
    }
    finally
    {
        reportWriter?.Dispose();
    }
});

var runIdArgument = new Argument<Guid>("run-id") { Description = "Run id printed by 'recover-tags --apply', 'fix-encoding --apply', 'rename --apply', 'organize --apply', or 'remove-duplicates --apply'" };

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

var cleanJournalDaysOption = new Option<int>("--older-than-days") { Description = "Delete runs whose most recent activity is older than this many days" };
cleanJournalDaysOption.DefaultValueFactory = _ => 30;
var cleanJournalApplyOption = new Option<bool>("--apply") { Description = "Actually delete old runs (default is dry-run: report only)" };

var cleanJournalCommand = new Command("clean-journal", "Delete old runs from the journal (backups accumulate under %LocalAppData%/MusicOrganizer/journal/ otherwise)");
cleanJournalCommand.Options.Add(cleanJournalDaysOption);
cleanJournalCommand.Options.Add(cleanJournalApplyOption);
cleanJournalCommand.SetAction(async (parseResult, cancellationToken) =>
{
    var olderThanDays = parseResult.GetValue(cleanJournalDaysOption);
    var apply = parseResult.GetValue(cleanJournalApplyOption);
    var cleanJournalUseCase = host.Services.GetRequiredService<CleanJournalUseCase>();

    var total = 0;

    await foreach (var outcome in cleanJournalUseCase.CleanAsync(TimeSpan.FromDays(olderThanDays), dryRun: !apply, cancellationToken))
    {
        total++;
        var verb = apply ? "DELETED" : "WOULD DELETE";
        Console.WriteLine($"[{verb}] {outcome.RunId} — {outcome.EntryCount} entrie(s), last activity {outcome.LastActivityAtUtc:u}");
    }

    Console.WriteLine($"{(apply ? "Deleted" : "Would delete")} {total} run(s) older than {olderThanDays} day(s).");
    return 0;
});

var rootCommand = new RootCommand("MusicOrganizer - professional MP3 collection manager");
rootCommand.Subcommands.Add(scanCommand);
rootCommand.Subcommands.Add(recoverTagsCommand);
rootCommand.Subcommands.Add(fixEncodingCommand);
rootCommand.Subcommands.Add(renameCommand);
rootCommand.Subcommands.Add(organizeCommand);
rootCommand.Subcommands.Add(findDuplicatesCommand);
rootCommand.Subcommands.Add(removeDuplicatesCommand);
rootCommand.Subcommands.Add(rollbackCommand);
rootCommand.Subcommands.Add(cleanJournalCommand);

return await rootCommand.Parse(args).InvokeAsync();
