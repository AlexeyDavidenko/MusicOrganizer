namespace MusicOrganizer.Application.Reporting;

/// <summary>
/// Format-agnostic model of one command run's report: a title, free-form summary lines, and a
/// table of rows. Serialized to a concrete format by an <see cref="IReportWriter"/>.
/// </summary>
public sealed record ReportDocument
{
    /// <summary>Human-readable title, e.g. the command name.</summary>
    public required string Title { get; init; }

    /// <summary>Ordered column headers; every <see cref="ReportRow"/> in <see cref="Rows"/> has
    /// exactly this many values, in this order.</summary>
    public required IReadOnlyList<string> Columns { get; init; }

    /// <summary>Data rows.</summary>
    public IReadOnlyList<ReportRow> Rows { get; init; } = [];

    /// <summary>Free-form summary lines (e.g. "Scanned 10 file(s): 8 with tags, 2 error(s)."). Not
    /// every <see cref="IReportWriter"/> renders this — see the implementation's own documentation.</summary>
    public IReadOnlyList<string> Summary { get; init; } = [];
}
