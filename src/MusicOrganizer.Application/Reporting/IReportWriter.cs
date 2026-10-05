namespace MusicOrganizer.Application.Reporting;

/// <summary>
/// Serializes a <see cref="ReportDocument"/> to a concrete structured format. Implemented in
/// Infrastructure, one class per <see cref="ReportFormat"/> value other than
/// <see cref="ReportFormat.PlainText"/> (see that value's documentation for why).
/// </summary>
public interface IReportWriter
{
    /// <summary>The format this writer produces.</summary>
    public ReportFormat Format { get; }

    /// <summary>Serializes <paramref name="document"/> to <paramref name="destination"/>.</summary>
    /// <param name="document">The report to serialize.</param>
    /// <param name="destination">Stream to write the serialized report to. Not closed by this method.</param>
    /// <param name="cancellationToken">Token used to cancel the write.</param>
    /// <returns>A task that completes when the document has been fully written.</returns>
    public Task WriteAsync(ReportDocument document, Stream destination, CancellationToken cancellationToken);
}
