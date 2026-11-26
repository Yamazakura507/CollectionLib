namespace CollectionLib.Models.Savings
{
    internal sealed class SavedBrush
    {
        public string Kind { get; set; } = "Solid";
        public string? Color { get; set; }
        public double StartX { get; set; }
        public double StartY { get; set; } = 0.5;
        public double EndX { get; set; } = 1;
        public double EndY { get; set; } = 0.5;
        public List<SavedGradientStop> Stops { get; set; } = [];
    }
}
