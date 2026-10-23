namespace Mario2.Editor
{
    /// <summary>One frame of scrolling and zoom gestures.</summary>
    /// <param name="Points">Smooth scrolling (trackpad, Magic Mouse) in screen points, in the direction the content should move.</param>
    /// <param name="Notches">Mouse wheel clicks, with raylib's sign (positive Y = wheel up).</param>
    /// <param name="Magnify">Pinch zoom as a factor (1 = no pinch).</param>
    /// <param name="SmartMagnify">A two-finger double tap.</param>
    public readonly record struct ScrollInput(Vector2 Points, Vector2 Notches, float Magnify, bool SmartMagnify)
    {
        public static readonly ScrollInput None = new(Vector2.Zero, Vector2.Zero, 1f, false);

        /// <summary>GLFW reports smooth scrolling as points / 10 on macOS. Elsewhere one unit is a wheel click's worth.</summary>
        private static readonly float PointsPerUnit = OperatingSystem.IsMacOS() ? 10f : 50f;

        private static double _lastSmoothTime = double.NegativeInfinity;

        public bool HasScroll => Points != Vector2.Zero || Notches != Vector2.Zero;

        public ScrollInput WithoutScroll() => this with { Points = Vector2.Zero, Notches = Vector2.Zero };

        public static ScrollInput Read()
        {
            if (MacTrackpad.IsInstalled)
                return MacTrackpad.Drain();

            // Without the macOS hook raylib's wheel value is all there is: trackpads report
            // fractions, mouse wheels whole clicks
            Vector2 wheel = Raylib.GetMouseWheelMoveV();
            if (wheel == Vector2.Zero)
                return None;

            double now = Raylib.GetTime();
            bool fractional = wheel.X != MathF.Round(wheel.X) || wheel.Y != MathF.Round(wheel.Y);

            // A trackpad occasionally reports a whole number too; it's still part of the same swipe
            if (fractional || now - _lastSmoothTime < ScrollLatch.GestureGap)
            {
                _lastSmoothTime = now;
                return None with { Points = wheel * PointsPerUnit };
            }

            return None with { Notches = wheel };
        }
    }

    /// <summary>
    /// Keeps a scroll gesture going to whatever it started over (the level or a panel), including
    /// macOS momentum. Otherwise momentum from scrolling the palette would start panning the level
    /// as soon as the pointer drifted onto it.
    /// </summary>
    public sealed class ScrollLatch
    {
        /// <summary>Scroll events closer together than this belong to one gesture.</summary>
        public const double GestureGap = 0.3;

        private object? _target;
        private double _lastTime = double.NegativeInfinity;

        /// <summary>Who this frame's scrolling goes to. <paramref name="hovered"/> is only used when a new gesture starts.</summary>
        public object? Resolve(bool scrolling, object? hovered)
        {
            if (!scrolling)
                return null;

            double now = Raylib.GetTime();
            if (now - _lastTime > GestureGap)
                _target = hovered;

            _lastTime = now;
            return _target;
        }

        public void Reset()
        {
            _target = null;
            _lastTime = double.NegativeInfinity;
        }
    }
}
