using System.Globalization;
using System.Text;
using MusicOrganizer.Application.Reporting;

namespace MusicOrganizer.Infrastructure.Reporting;

/// <summary>
/// Serializes a <see cref="ReportDocument"/> as RFC 4180 CSV: a header row of
/// <see cref="ReportDocument.Columns"/> followed by one line per <see cref="ReportDocument.Rows"/>
/// entry. Deliberately omits <see cref="ReportDocument.Title"/> and
/// <see cref="ReportDocument.Summary"/> — CSV exists for machine/spreadsheet consumption, which
/// prose mixed into the data region would break for no benefit.
/// </summary>
public sealed class CsvReportWriter : IReportWriter
{
    private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);

    /// <inheritdoc />
    public ReportFormat Format => ReportFormat.Csv;

    /// <inheritdoc />
    public async Task WriteAsync(ReportDocument document, Stream destination, CancellationToken cancellationToken)
    {
        var writer = new StreamWriter(destination, Utf8NoBom, leaveOpen: true) { NewLine = "\r\n" };
        await using (writer.ConfigureAwait(false))
        {
            await writer.WriteLineAsync(string.Join(',', document.Columns.Select(Escape)));
            foreach (var row in document.Rows)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await writer.WriteLineAsync(string.Join(',', row.Values.Select(Escape)));
            }
        }
    }

    private static string Escape(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var needsQuoting = value.Contains(',', StringComparison.Ordinal)
            || value.Contains('"', StringComparison.Ordinal)
            || value.Contains('\r', StringComparison.Ordinal)
            || value.Contains('\n', StringComparison.Ordinal);

        if (!needsQuoting)
        {
            return value;
        }

        return string.Create(CultureInfo.InvariantCulture, $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"");
    }
}
