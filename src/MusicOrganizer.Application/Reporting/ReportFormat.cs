namespace MusicOrganizer.Application.Reporting;

/// <summary>
/// Output format for a command's report, selected via <c>--format</c> (Cli) or a save-dialog file
/// type (Gui).
/// </summary>
public enum ReportFormat
{
    /// <summary>
    /// Plain text, byte-identical to console output. Handled directly by the existing live-tee
    /// mechanism (<see cref="MusicOrganizer.Shared.TeeTextWriter"/> on Cli, the existing
    /// per-screen <c>formatLine</c> functions on Gui) — no <see cref="IReportWriter"/> implements
    /// this value.
    /// </summary>
    PlainText,

    /// <summary>Structured JSON document.</summary>
    Json,

    /// <summary>Tabular CSV (header + data rows only, no title/summary prose).</summary>
    Csv,

    /// <summary>Self-contained HTML document with a table.</summary>
    Html,
}
