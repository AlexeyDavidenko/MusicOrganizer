using System.Net;
using System.Text;
using MusicOrganizer.Application.Reporting;

namespace MusicOrganizer.Infrastructure.Reporting;

/// <summary>
/// Serializes a <see cref="ReportDocument"/> as a minimal self-contained HTML document: a title,
/// the summary as a bullet list, and the rows as a bordered table. Every value is
/// <see cref="WebUtility.HtmlEncode(string?)"/>d — file paths and recovered tag text are
/// arbitrary user-originated content (in particular, <c>fix-encoding</c>'s whole purpose is
/// correcting mis-decoded text), so treating it as literal HTML would be a real injection risk.
/// </summary>
public sealed class HtmlReportWriter : IReportWriter
{
    private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);

    /// <inheritdoc />
    public ReportFormat Format => ReportFormat.Html;

    /// <inheritdoc />
    public async Task WriteAsync(ReportDocument document, Stream destination, CancellationToken cancellationToken)
    {
        var writer = new StreamWriter(destination, Utf8NoBom, leaveOpen: true);
        await using (writer.ConfigureAwait(false))
        {
            var title = WebUtility.HtmlEncode(document.Title);
            await writer.WriteLineAsync($"<!DOCTYPE html><html><head><meta charset=\"utf-8\"><title>{title}</title>");
            await writer.WriteLineAsync("<style>table{border-collapse:collapse}td,th{border:1px solid #999;padding:4px 8px;text-align:left}</style>");
            await writer.WriteLineAsync($"</head><body><h1>{title}</h1>");

            if (document.Summary.Count > 0)
            {
                await writer.WriteLineAsync("<ul>");
                foreach (var line in document.Summary)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await writer.WriteLineAsync($"<li>{WebUtility.HtmlEncode(line)}</li>");
                }

                await writer.WriteLineAsync("</ul>");
            }

            await writer.WriteLineAsync("<table><thead><tr>");
            foreach (var column in document.Columns)
            {
                await writer.WriteLineAsync($"<th>{WebUtility.HtmlEncode(column)}</th>");
            }

            await writer.WriteLineAsync("</tr></thead><tbody>");

            foreach (var row in document.Rows)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await writer.WriteLineAsync("<tr>");
                foreach (var value in row.Values)
                {
                    await writer.WriteLineAsync($"<td>{WebUtility.HtmlEncode(value)}</td>");
                }

                await writer.WriteLineAsync("</tr>");
            }

            await writer.WriteLineAsync("</tbody></table></body></html>");
        }
    }
}
