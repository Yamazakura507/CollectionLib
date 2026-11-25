namespace CollectionLib.Models.Draw
{
    internal sealed class RoundedTreeConnectorDrawable(Color color, bool isLast) : IDrawable
    {
        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            if (dirtyRect.Width <= 0 || dirtyRect.Height <= 0) return;

            float spineX = Math.Min(8.5f, dirtyRect.Width * 0.38f);
            float targetX = Math.Max(spineX + 8, dirtyRect.Width - 1);
            float junctionY = Math.Min(17f, Math.Max(10f, dirtyRect.Height * 0.45f));

            canvas.StrokeColor = color;
            canvas.StrokeSize = 1.7f;
            canvas.StrokeLineCap = LineCap.Round;
            canvas.StrokeLineJoin = LineJoin.Round;

            PathF spine = new();

            spine.MoveTo(spineX, 0);
            spine.LineTo(spineX, isLast ? junctionY : dirtyRect.Height);
            canvas.DrawPath(spine);

            PathF branch = new();

            branch.MoveTo(spineX, junctionY);
            branch.QuadTo(spineX + 5.5f, junctionY + 1.5f, targetX, junctionY);
            canvas.DrawPath(branch);
        }
    }
}
