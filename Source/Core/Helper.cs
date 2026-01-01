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
    }
}
