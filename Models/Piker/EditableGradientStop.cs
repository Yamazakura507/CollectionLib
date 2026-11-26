namespace CollectionLib.Models.Piker
{
    internal sealed class EditableGradientStop(double offset, Color color)
    {
        public double Offset { get; set; } = offset;
        public Color Color { get; set; } = color;
    }
}
