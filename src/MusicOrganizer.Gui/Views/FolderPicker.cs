using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace MusicOrganizer.Gui.Views;

/// <summary>
/// Shared folder-picker helper, since <see cref="IStorageProvider"/> access is inherently tied to a
/// visual (it needs the window to anchor the native dialog to) — every screen with a root-folder
/// input calls this instead of duplicating the same few lines in each View's code-behind.
/// </summary>
internal static class FolderPicker
{
    /// <summary>
    /// Shows a native folder picker anchored to <paramref name="anchor"/>'s window.
    /// </summary>
    /// <param name="anchor">Any control in the window to anchor the dialog to.</param>
    /// <returns>The chosen folder's local path, or <see langword="null"/> if cancelled.</returns>
    public static async Task<string?> PickFolderAsync(Visual anchor)
    {
        var topLevel = TopLevel.GetTopLevel(anchor);
        if (topLevel is null)
        {
            return null;
        }

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            AllowMultiple = false,
        });

        return folders.Count > 0 ? folders[0].Path.LocalPath : null;
    }
}
