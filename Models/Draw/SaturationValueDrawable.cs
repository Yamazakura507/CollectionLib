using CollectionLib.Controls.Components.Picker;

namespace CollectionLib.Models.Draw
{
    internal sealed class SaturationValueDrawable(ColorBrushPickerView owner) : IDrawable
    {
        private double cachedHue = double.NaN;
        private LinearGradientPaint? horizontalPaint;
        private static readonly LinearGradientPaint ValueOverlay = new(
            [
                new PaintGradientStop(0, Colors.Transparent),
                new PaintGradientStop(1, Colors.Black)
            ],
            new Point(0.5, 0),
            new Point(0.5, 1));

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            if (dirtyRect.Width <= 0 || dirtyRect.Height <= 0) return;
            if (horizontalPaint is null || Math.Abs(cachedHue - owner.hue) > 0.001)
            {
                cachedHue = owner.hue;
                horizontalPaint = new LinearGradientPaint(
                    [
                        new PaintGradientStop(0, Colors.White),
                        new PaintGradientStop(1, ColorBrushPickerView.FromHsv(owner.hue, 1, 1))
                    ],
                    new Point(0, 0.5),
                    new Point(1, 0.5));
            }

            canvas.SetFillPaint(horizontalPaint, dirtyRect);
            canvas.FillRectangle(dirtyRect.X, dirtyRect.Y, dirtyRect.Width, dirtyRect.Height);

            canvas.SetFillPaint(ValueOverlay, dirtyRect);
            canvas.FillRectangle(dirtyRect.X, dirtyRect.Y, dirtyRect.Width, dirtyRect.Height);

            float markerX = dirtyRect.X + (float)(owner.saturation * dirtyRect.Width);
            float markerY = dirtyRect.Y + (float)((1 - owner.brightness) * dirtyRect.Height);

            canvas.StrokeSize = 3;
            canvas.StrokeColor = Colors.Black;

            canvas.DrawCircle(markerX, markerY, 6);

            canvas.StrokeSize = 1.5f;
            canvas.StrokeColor = Colors.White;

            canvas.DrawCircle(markerX, markerY, 6);
        }
    }
}
