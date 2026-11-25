namespace CollectionLib.Models.Handlers
{
    /// <summary>
    /// Notification payload passed to the configured progress handler.
    /// </summary>
    /// <param name="Title">Dialog or notification title.</param>
    /// <param name="Text">Notification text.</param>
    /// <param name="Accept">Default dialog confirmation button text.</param>
    /// <param name="Page">Optional page that should host the default dialog.</param>
    public sealed record Message(string Title, string Text, string Accept = "OK", Page? Page = null);
}
