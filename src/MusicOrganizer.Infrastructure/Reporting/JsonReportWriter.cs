using System.Text.Json;
using MusicOrganizer.Application.Reporting;

namespace MusicOrganizer.Infrastructure.Reporting;

/// <summary>
/// Serializes a <see cref="ReportDocument"/> as indented JSON: <c>{ title, summary, columns,
/// rows }</c>, where each row is an array of values (not an object keyed by column — a consumer
/// zips <c>columns</c> with each row itself, which avoids repeating column names on every row).
/// </summary>
public sealed class JsonReportWriter : IReportWriter
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    /// <inheritdoc />
    public ReportFormat Format => ReportFormat.Json;

    /// <inheritdoc />
    public async Task WriteAsync(ReportDocument document, Stream destination, CancellationToken cancellationToken)
    {
        var payload = new JsonReportPayload(
            document.Title,
            document.Summary,
            document.Columns,
            document.Rows.Select(row => row.Values).ToList());

        await JsonSerializer.SerializeAsync(destination, payload, Options, cancellationToken);
    }

    private sealed record JsonReportPayload(
        string Title,
        IReadOnlyList<string> Summary,
        IReadOnlyList<string> Columns,
        IReadOnlyList<IReadOnlyList<string?>> Rows);
}
