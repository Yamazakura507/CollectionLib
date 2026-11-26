using CollectionLib.Controls.Components.Picker;

namespace CollectionLib.Models.Draw
{
    internal sealed class HueSpectrumDrawable(ColorBrushPickerView owner) : IDrawable
    {
        private static readonly LinearGradientPaint HuePaint = new(
            [
                new PaintGradientStop(0, Colors.Red),
                new PaintGradientStop(1f / 6f, Colors.Yellow),
                new PaintGradientStop(2f / 6f, Colors.Lime),
                new PaintGradientStop(3f / 6f, Colors.Cyan),
                new PaintGradientStop(4f / 6f, Colors.Blue),
                new PaintGradientStop(5f / 6f, Colors.Magenta),
                new PaintGradientStop(1, Colors.Red)
            ],
            new Point(0, 0.5),
            new Point(1, 0.5));

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            if (dirtyRect.Width <= 0 || dirtyRect.Height <= 0) return;

            canvas.SetFillPaint(HuePaint, dirtyRect);
            canvas.FillRectangle(dirtyRect.X, dirtyRect.Y, dirtyRect.Width, dirtyRect.Height);

            float markerX = dirtyRect.X + (float)(owner.hue / 360d * dirtyRect.Width);
            canvas.StrokeColor = Colors.Black;
            canvas.StrokeSize = 3;

            canvas.DrawLine(markerX, dirtyRect.Y, markerX, dirtyRect.Bottom);

            canvas.StrokeColor = Colors.White;
            canvas.StrokeSize = 1;

            canvas.DrawLine(markerX, dirtyRect.Y, markerX, dirtyRect.Bottom);
        }
    }
}
