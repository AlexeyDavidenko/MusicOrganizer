namespace MusicOrganizer.Application.Reporting;

/// <summary>
/// One row of a <see cref="ReportDocument"/>. Values only, positionally matched to
/// <see cref="ReportDocument.Columns"/> — every row in a document shares the same column set, so
/// field names are not repeated per row.
/// </summary>
/// <param name="Values">Cell values, in the same order as <see cref="ReportDocument.Columns"/>. A
/// <see langword="null"/> entry renders as an empty cell.</param>
public sealed record ReportRow(IReadOnlyList<string?> Values);
