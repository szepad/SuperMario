namespace Mario2
{
    public static class Helper
    {
        public static float Clamp(float val, float min, float max)
        {
            if (val < min) return min;
            if (val > max) return max;
            return val;
        }

        public static float Clamp01(float val) 
            => Clamp(val, 0f, 1f);

        public static bool GuiDisabledButton(string label, bool disabled)
        {
            if (disabled) Gui.BeginDisabled();
            bool result = Gui.Button(label);
            if (disabled) Gui.EndDisabled();
            return result;
        }
        
        public static Color Alpha(this Color color, float opacity)
        {
            return new Color(color.R, color.G, color.B, (byte)(255f * opacity));
        }

        public static Vector2 Scale(this Vector2 vec2)
        {
            return vec2 * Game.TextureScale;
        }

        public static bool CloseTo(this float val, float target, float threshold = 0.01f)
        {
            return MathF.Abs(val - target) < threshold;
        }

        public static bool CloseToZero(this float val, float threshold = 0.01f)
        {
            return CloseTo(val, 0f, threshold);
        }

        public static void Draw(this Rectangle rect, Color color)
        {
            Raylib.DrawRectangle((int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height, color);
        }
    }
}
