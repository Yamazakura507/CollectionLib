using CollectionLib.Controls;

namespace CollectionLib.Models.Popup
{
    internal sealed record PopupReorderPayload(
        CollectionListViewBase Owner,
        int SourceIndex,
        Action<int, int> ReorderAction,
        VerticalStackLayout Host,
        VerticalStackLayout SourceContainer);
}
