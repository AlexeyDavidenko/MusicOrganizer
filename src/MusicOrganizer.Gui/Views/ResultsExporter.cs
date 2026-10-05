using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Microsoft.Extensions.DependencyInjection;
using MusicOrganizer.Application.Reporting;

namespace MusicOrganizer.Gui.Views;

/// <summary>
/// Shared "Save results…" helper: writes an already-collected results list to a file chosen via a
/// native save dialog, in the format implied by the chosen file's extension. Every screen's
/// results are already fully in memory once a run finishes, so this simple post-run export is
/// enough — no need to reuse <see cref="MusicOrganizer.Shared.TeeTextWriter"/>, which exists
/// specifically to tee the CLI's *live* console output.
/// </summary>
internal static class ResultsExporter
{
    private static readonly FilePickerFileType TextFileType = new("Plain text") { Patterns = ["*.txt"] };
    private static readonly FilePickerFileType JsonFileType = new("JSON") { Patterns = ["*.json"] };
    private static readonly FilePickerFileType CsvFileType = new("CSV") { Patterns = ["*.csv"] };
    private static readonly FilePickerFileType HtmlFileType = new("HTML") { Patterns = ["*.html"] };

    /// <summary>
    /// Shows a save-file dialog and, if confirmed, writes the results in the format implied by the
    /// chosen file's extension: plain text (one line per item, via <paramref name="formatLine"/>)
    /// for <c>.txt</c>, or a structured <see cref="ReportDocument"/> (via <paramref name="toRow"/>)
    /// serialized by the matching <see cref="IReportWriter"/> for <c>.json</c>/<c>.csv</c>/<c>.html</c>.
    /// </summary>
    /// <typeparam name="T">Result item type.</typeparam>
    /// <param name="anchor">Any control in the window to anchor the dialog to.</param>
    /// <param name="results">Items to write.</param>
    /// <param name="formatLine">Formats a single item as one line of plain text.</param>
    /// <param name="title">Report title, used by structured formats.</param>
    /// <param name="columns">Column headers, used by structured formats.</param>
    /// <param name="toRow">Maps a single item to a structured report row.</param>
    /// <returns>A task that completes when the file has been written, or the dialog was cancelled.</returns>
    public static async Task SaveAsync<T>(
        Visual anchor,
        IEnumerable<T> results,
        Func<T, string> formatLine,
        string title,
        IReadOnlyList<string> columns,
        Func<T, ReportRow> toRow)
    {
        var topLevel = TopLevel.GetTopLevel(anchor);
        if (topLevel is null)
        {
            return;
        }

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            SuggestedFileName = "results.txt",
            DefaultExtension = "txt",
            FileTypeChoices = [TextFileType, JsonFileType, CsvFileType, HtmlFileType],
        });

        if (file is null)
        {
            return;
        }

        var format = DetermineFormat(file.Name);
        await using var stream = await file.OpenWriteAsync();

        if (format == ReportFormat.PlainText)
        {
            await using var writer = new StreamWriter(stream);
            foreach (var result in results)
            {
                await writer.WriteLineAsync(formatLine(result));
            }

            return;
        }

        var document = new ReportDocument { Title = title, Columns = columns, Rows = results.Select(toRow).ToList() };
        var reportWriter = App.Services.GetRequiredService<IEnumerable<IReportWriter>>().Single(w => w.Format == format);
        await reportWriter.WriteAsync(document, stream, CancellationToken.None);
    }

    private static ReportFormat DetermineFormat(string fileName) =>
        Path.GetExtension(fileName).ToUpperInvariant() switch
        {
            ".JSON" => ReportFormat.Json,
            ".CSV" => ReportFormat.Csv,
            ".HTML" or ".HTM" => ReportFormat.Html,
            _ => ReportFormat.PlainText,
        };
}
