using CollectionLib.Enums;

namespace CollectionLib.Models.Savings
{
    internal sealed class SavedBackgroundState
    {
        public bool IsSave { get; set; }
        public CollectionItemBackgroundMode ItemBackgroundMode { get; set; }
        public string ItemBackgroundColor { get; set; } = string.Empty;
        public SavedBrush? ItemBackgroundBrush { get; set; }
        public GradientMotionMode ItemGradientMotionMode { get; set; }
        public CollectionListBackgroundMode ListBackgroundMode { get; set; }
        public string ListBackgroundColor { get; set; } = string.Empty;
        public SavedBrush? ListBackgroundBrush { get; set; }
        public GradientMotionMode ListGradientMotionMode { get; set; }
        public bool HasImage { get; set; }
    }
}
