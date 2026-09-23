using MusicOrganizer.Domain.Encoding;

namespace MusicOrganizer.Application.Encoding;

/// <summary>
/// Port for determining which of a file's tag fields are eligible for the Latin1-reversal
/// encoding fix (see <see cref="TextFieldEligibility"/>).
/// </summary>
public interface IEncodingEligibilityInspector
{
    /// <summary>
    /// Inspects <paramref name="filePath"/>'s tag frames to determine field eligibility.
    /// </summary>
    /// <param name="filePath">Path of the file to inspect.</param>
    /// <param name="cancellationToken">Token used to cancel the inspection.</param>
    public Task<TextFieldEligibility> InspectAsync(string filePath, CancellationToken cancellationToken = default);
}
