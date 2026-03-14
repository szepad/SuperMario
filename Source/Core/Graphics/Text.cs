namespace Mario2
{
    public enum Pivot
    {
        TopLeft, Top, TopRight,
        Left, Center, Right,
        BottomLeft, Bottom, BottomRight
    }

    public static class Text
    {
        public const int FontSize = 8 * Game.TextureScale;

        public static int GetWidth(string text) =>
            (int)Raylib.MeasureTextEx(Assets.Font, text, FontSize, 0f).X;

        public static void Draw(string text, Vector2 pos, Pivot pivot = Pivot.TopLeft, bool greyed = false)
            => Draw(text, pos.X, pos.Y, pivot, greyed);

        public static void Draw(string text, float x, float y, Pivot pivot = Pivot.TopLeft, bool greyed = false)
        {
            text = text.ToUpper();
            int width = GetWidth(text);

            float xoff = 0f;
            if (pivot == Pivot.Top || pivot == Pivot.Center || pivot == Pivot.Bottom)
            {
                xoff = width / 2;
            }
            else if (pivot == Pivot.TopRight || pivot == Pivot.Right || pivot == Pivot.BottomRight)
            {
                xoff = width;
            }

            float yoff = 0f;
            if (pivot == Pivot.Left || pivot == Pivot.Center || pivot == Pivot.Right)
            {
                yoff = FontSize / 2;
            }
            else if (pivot == Pivot.BottomLeft || pivot == Pivot.Bottom || pivot == Pivot.BottomRight)
            {
                yoff = FontSize;
            }

            Raylib.DrawTextEx(Assets.Font, text, 
                new Vector2((int)(x - xoff), (int)(y - yoff)), 
                FontSize, 0, greyed ? Color.White.Alpha(0.5f) : Color.White);
        }
    }
}
