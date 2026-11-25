using CollectionLib.Models.Handlers;

namespace CollectionLib.Handlers;

/// <summary>
/// Centralizes user-facing notifications raised by CollectionLib.
/// Applications can replace <see cref="Progress"/> to use their own notification UI.
/// </summary>
public static class HandlerAction
{
    /// <summary>
    /// Handles library notifications. By default, messages are displayed in a MAUI alert dialog.
    /// Applications can replace this field with any <see cref="IProgress{T}"/> implementation.
    /// </summary>
    public static IProgress<Message> Progress = new DialogProgress();

    /// <summary>
    /// Reports a notification through the currently configured progress handler.
    /// </summary>
    public static void Report(string title, string text, Page? page = null, string accept = "OK")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        Progress.Report(new Message(title, text, accept, page));
    }

    /// <summary>
    /// Restores the default alert-dialog notification handler.
    /// </summary>
    public static void ResetProgress() => Progress = new DialogProgress();
}