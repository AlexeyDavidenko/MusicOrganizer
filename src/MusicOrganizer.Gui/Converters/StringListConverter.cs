using System.Globalization;
using Avalonia.Data.Converters;

namespace MusicOrganizer.Gui.Converters;

/// <summary>
/// Joins an <see cref="IReadOnlyList{T}"/> of strings with a comma, for display in a single
/// <c>TextBlock</c>. Used by every results list binding a field like
/// <see cref="MusicOrganizer.Domain.TagRecovery.TagRecoveryOutcome.RecoveredFields"/>.
/// </summary>
public sealed class StringListConverter : IValueConverter
{
    /// <summary>Shared instance, for use as a static XAML resource.</summary>
    public static readonly StringListConverter Instance = new();

    /// <inheritdoc/>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is IReadOnlyList<string> items ? string.Join(", ", items) : null;

    /// <inheritdoc/>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
