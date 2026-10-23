namespace Mario2.Editor
{
    /// <summary>
    /// The viewport camera. <see cref="Target"/> is the world point shown at the viewport's top-left corner.
    /// </summary>
    public sealed class EditorCamera
    {
        public const float MinZoom = 0.05f;
        public const float MaxZoom = 4f;

        public Rectangle Viewport { get; set; }
        public Vector2 Target { get; set; }
        public float Zoom { get; private set; } = 0.25f;

        public Camera2D AsCamera2D => new(Viewport.Position, Target, 0f, Zoom);

        public Rectangle VisibleWorld => new(Target, Viewport.Size / Zoom);

        public Vector2 ScreenToWorld(Vector2 screen) => (screen - Viewport.Position) / Zoom + Target;
        public Vector2 WorldToScreen(Vector2 world) => (world - Target) * Zoom + Viewport.Position;

        public void Pan(Vector2 screenDelta)
        {
            Target -= screenDelta / Zoom;
        }

        /// <summary>Zooms around the middle of the viewport (keyboard zoom).</summary>
        public void ZoomCentered(float factor) => ZoomAt(Viewport.Position + Viewport.Size / 2f, factor);

        public void ZoomAt(Vector2 screen, float factor)
        {
            Vector2 anchor = ScreenToWorld(screen);
            Zoom = Math.Clamp(Zoom * factor, MinZoom, MaxZoom);
            Target = anchor - (screen - Viewport.Position) / Zoom;
        }

        /// <summary>The zoom at which the level's height fills the viewport (with a small margin).</summary>
        public float FitZoom
        {
            get
            {
                const float Margin = 1.1f;
                return Viewport.Height > 0 ? Math.Clamp(Viewport.Height / (LevelData.Height * Tile.Size * Margin), MinZoom, MaxZoom) : Zoom;
            }
        }

        /// <summary>Zooms so the level's height fills the viewport and centers on a world X.</summary>
        public void FitHeight(float centerX)
        {
            Zoom = FitZoom;
            CenterOn(new Vector2(centerX, LevelData.Height * Tile.Size / 2f));
        }

        /// <summary>
        /// The trackpad's smart zoom (two-finger double tap): fits the level's height, or zooms
        /// in to twice that if it already fits. Keeps the point under the cursor in place.
        /// </summary>
        public void ToggleSmartZoom(Vector2 screen)
        {
            float fit = FitZoom;
            float target = MathF.Abs(Zoom - fit) < fit * 0.05f ? fit * 2f : fit;
            ZoomAt(screen, target / Zoom);
        }

        public void CenterOn(Vector2 world)
        {
            Target = world - Viewport.Size / Zoom / 2f;
        }

        /// <summary>Keeps at least part of the level in view so it can't get lost.</summary>
        public void Clamp(float levelWidth)
        {
            Vector2 view = Viewport.Size / Zoom;
            float levelHeight = LevelData.Height * Tile.Size;
            float margin = Tile.Size * 2f;

            Target = new Vector2(
                Math.Clamp(Target.X, -view.X + margin, levelWidth - margin),
                Math.Clamp(Target.Y, -view.Y + margin, levelHeight - margin));
        }
    }
}
