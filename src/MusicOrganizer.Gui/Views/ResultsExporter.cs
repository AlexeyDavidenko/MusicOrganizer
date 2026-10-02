using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace MusicOrganizer.Gui.Views;

/// <summary>
/// Shared "Save results…" helper: writes an already-collected results list to a plain-text file
/// chosen via a native save dialog. Every screen's results are already fully in memory once a run
/// finishes, so this simple post-run export is enough — no need to reuse
/// <see cref="MusicOrganizer.Shared.TeeTextWriter"/>, which exists specifically to tee the CLI's
/// *live* console output.
/// </summary>
internal static class ResultsExporter
{
    /// <summary>
    /// Shows a save-file dialog and, if confirmed, writes one formatted line per item.
    /// </summary>
    /// <typeparam name="T">Result item type.</typeparam>
    /// <param name="anchor">Any control in the window to anchor the dialog to.</param>
    /// <param name="results">Items to write, one per line.</param>
    /// <param name="formatLine">Formats a single item as one line of text.</param>
    /// <returns>A task that completes when the file has been written, or the dialog was cancelled.</returns>
    public static async Task SaveAsync<T>(Visual anchor, IEnumerable<T> results, Func<T, string> formatLine)
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
        });

        if (file is null)
        {
            return;
        }

        await using var stream = await file.OpenWriteAsync();
        using var writer = new StreamWriter(stream);
        foreach (var result in results)
        {
            await writer.WriteLineAsync(formatLine(result));
        }
    }
}
