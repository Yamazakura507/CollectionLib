using CollectionLib.Enums;

namespace CollectionLib.Models.Animators;

/// <summary>
/// Animates a shared gradient brush independently from the popup editor that configured it.
/// Flow and pulse move a periodic color sequence in one direction, so colors wrap across the edges.
/// </summary>
internal sealed class GradientBrushMotionAnimator
{
    private const double FrameIntervalSeconds = 1d / 30d;
    private const double SeamLength = 0.35d;

    private readonly IDispatcherTimer timer;
    private readonly List<MotionStop> baseStops = [];
    private readonly List<MotionStop> cyclicStops = [];

    private LinearGradientBrush? target;
    private GradientMotionMode mode;
    private Point baseStart;
    private Point baseEnd;
    private double elapsedSeconds;
    private double cycleLength = 1d + SeamLength;

    public GradientBrushMotionAnimator(IDispatcher dispatcher)
    {
        timer = dispatcher.CreateTimer();
        timer.Interval = TimeSpan.FromSeconds(FrameIntervalSeconds);
        timer.IsRepeating = true;
        timer.Tick += OnTick;
    }

    /// <summary>
    /// Attaches the animator to a brush and starts or stops motion as required.
    /// The original stops and geometry are retained so switching motion off restores the exact brush.
    /// </summary>
    public void Configure(Brush? brush, GradientMotionMode motionMode, bool enabled)
    {
        LinearGradientBrush? nextTarget = brush as LinearGradientBrush;
        bool shouldAnimate = enabled && motionMode != GradientMotionMode.None && nextTarget is not null;
        bool sameTarget = ReferenceEquals(target, nextTarget);

        if (sameTarget && mode == motionMode && timer.IsRunning == shouldAnimate) return;
        if (sameTarget && target is not null && !shouldAnimate)
        {
            RestoreBaseBrush();
            timer.Stop();

            mode = motionMode;
            elapsedSeconds = 0;

            return;
        }

        if (!sameTarget)
        {
            RestoreBaseBrush();
            timer.Stop();

            target = nextTarget;

            CaptureBaseBrush();
        }
        else if (timer.IsRunning)
        {
            mode = motionMode;
            elapsedSeconds = 0;

            return;
        }

        mode = motionMode;
        elapsedSeconds = 0;

        if (shouldAnimate) timer.Start();
    }

    /// <summary>
    /// Stops frame updates and restores the original gradient when the owner detaches.
    /// </summary>
    public void Stop()
    {
        RestoreBaseBrush();
        timer.Stop();

        elapsedSeconds = 0;
    }

    private void CaptureBaseBrush()
    {
        baseStops.Clear();
        cyclicStops.Clear();

        if (target is null) return;

        baseStart = target.StartPoint;
        baseEnd = target.EndPoint;

        foreach (GradientStop stop in target.GradientStops.OrderBy(stop => stop.Offset))
            baseStops.Add(new MotionStop(Math.Clamp(stop.Offset, 0, 1), stop.Color));

        if (baseStops.Count < 2) return;

        cycleLength = 1d + SeamLength;

        foreach (MotionStop stop in baseStops) cyclicStops.Add(stop);

        cyclicStops.Add(new MotionStop(cycleLength, baseStops[0].Color));
    }

    private void RestoreBaseBrush()
    {
        if (target is null || baseStops.Count < 2) return;

        target.StartPoint = baseStart;
        target.EndPoint = baseEnd;
        target.GradientStops = CreateGradientStopCollection(baseStops);
    }

    private void OnTick(object? sender, EventArgs e)
    {
        if (target is null || mode == GradientMotionMode.None || baseStops.Count < 2)
        {
            timer.Stop();

            return;
        }

        elapsedSeconds += FrameIntervalSeconds;

        switch (mode)
        {
            case GradientMotionMode.Flow:
                ApplyCyclicColorMotion(elapsedSeconds / 6d);
                target.StartPoint = baseStart;
                target.EndPoint = baseEnd;
                break;
            case GradientMotionMode.Pulse:
                double pulsePhase = elapsedSeconds / 4d + 0.08d * Math.Sin(elapsedSeconds * 2d * Math.PI / 4d);

                ApplyCyclicColorMotion(pulsePhase);

                double scale = 0.92d + 0.16d * (0.5d + 0.5d * Math.Sin(elapsedSeconds * 2d * Math.PI / 3.5d));
                (target.StartPoint, target.EndPoint) = GetScaledGeometry(scale);
                break;
            case GradientMotionMode.Rotate:
                (target.StartPoint, target.EndPoint) = GetRotatedGeometry();
                break;
        }
    }

    private void ApplyCyclicColorMotion(double phase)
    {
        if (target is null || cyclicStops.Count < 3) return;

        double shift = Modulo(phase * cycleLength, cycleLength);
        Color edgeColor = SampleCyclicColor(shift);

        List<MotionStop> shiftedStops = [];

        foreach (MotionStop stop in cyclicStops)
        {
            double offset = Modulo(stop.Offset - shift, cycleLength) / cycleLength;

            if (offset <= 0.000001d || offset >= 0.999999d) continue;

            shiftedStops.Add(new MotionStop(offset, stop.Color));
        }

        shiftedStops.Sort((left, right) => left.Offset.CompareTo(right.Offset));

        List<MotionStop> normalizedStops = [ new MotionStop(0, edgeColor) ];

        foreach (MotionStop stop in shiftedStops)
        {
            if (Math.Abs(stop.Offset - normalizedStops[^1].Offset) < 0.000001d)
            {
                normalizedStops[^1] = stop;

                continue;
            }

            normalizedStops.Add(stop);
        }

        normalizedStops.Add(new MotionStop(1, edgeColor));

        target.GradientStops = CreateGradientStopCollection(normalizedStops);
    }

    private Color SampleCyclicColor(double position)
    {
        double normalizedPosition = Modulo(position, cycleLength);

        for (int index = 0; index < cyclicStops.Count - 1; index++)
        {
            MotionStop left = cyclicStops[index];
            MotionStop right = cyclicStops[index + 1];

            if (normalizedPosition > right.Offset) continue;

            double span = right.Offset - left.Offset;
            double amount = span <= 0.000001d ? 0 : (normalizedPosition - left.Offset) / span;

            return Interpolate(left.Color, right.Color, amount);
        }

        return cyclicStops[^1].Color;
    }

    private (Point Start, Point End) GetRotatedGeometry()
    {
        double angle = elapsedSeconds * Math.PI / 4d;
        double cosine = Math.Cos(angle);
        double sine = Math.Sin(angle);

        return (RotateAroundCenter(baseStart, cosine, sine), RotateAroundCenter(baseEnd, cosine, sine));
    }

    private (Point Start, Point End) GetScaledGeometry(double scale) => (ScaleAroundCenter(baseStart, scale), ScaleAroundCenter(baseEnd, scale));

    private static GradientStopCollection CreateGradientStopCollection(IEnumerable<MotionStop> stops)
    {
        GradientStopCollection collection = [];

        foreach (MotionStop stop in stops.OrderBy(stop => stop.Offset)) collection.Add(new GradientStop(stop.Color, (float)Math.Clamp(stop.Offset, 0, 1)));

        return collection;
    }

    private static double Modulo(double value, double modulus) => ((value % modulus) + modulus) % modulus;

    private static Color Interpolate(Color start, Color end, double amount)
    {
        float factor = (float)Math.Clamp(amount, 0, 1);

        return new Color(
            start.Red + (end.Red - start.Red) * factor,
            start.Green + (end.Green - start.Green) * factor,
            start.Blue + (end.Blue - start.Blue) * factor,
            start.Alpha + (end.Alpha - start.Alpha) * factor);
    }

    private static Point RotateAroundCenter(Point point, double cosine, double sine)
    {
        double x = point.X - 0.5d;
        double y = point.Y - 0.5d;

        return new Point(0.5d + x * cosine - y * sine, 0.5d + x * sine + y * cosine);
    }

    private static Point ScaleAroundCenter(Point point, double scale) => new(0.5d + (point.X - 0.5d) * scale, 0.5d + (point.Y - 0.5d) * scale);
}
