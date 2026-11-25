namespace CollectionLib.Models.Handlers
{
    internal sealed class DialogProgress : IProgress<Message>
    {
        public void Report(Message value)
        {
            ArgumentNullException.ThrowIfNull(value);

            if (MainThread.IsMainThread)
            {
                _ = ShowDialogAsync(value);

                return;
            }

            MainThread.BeginInvokeOnMainThread(() => _ = ShowDialogAsync(value));
        }

        private static async ValueTask ShowDialogAsync(Message message)
        {
            try
            {
                Page? page = message.Page ?? Application.Current?.Windows.FirstOrDefault(window => window.Page is not null)?.Page;

                if (page is null) return;

                await page.DisplayAlertAsync(message.Title, message.Text, message.Accept);
            }
            catch (Exception exception)
            {
                Debug.WriteLine($"Unable to display CollectionLib notification: {exception}");
            }
        }
    }
}
