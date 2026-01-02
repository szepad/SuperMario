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
        public static void Draw(string text, Vector2 pos, int fontSize, Color color, Pivot pivot = Pivot.Center)
            => Draw(text, pos.X, pos.Y, fontSize, color, pivot);

        public static void Draw(string text, float x, float y, int fontSize, Color color, Pivot pivot = Pivot.TopLeft)
        {
            int width = Raylib.MeasureText(text, fontSize);

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
                yoff = fontSize / 2;
            }
            else if (pivot == Pivot.BottomLeft || pivot == Pivot.Bottom || pivot == Pivot.BottomRight)
            {
                yoff = fontSize;
            }

            Raylib.DrawText(text, (int)(x - xoff), (int)(y - yoff), fontSize, color);
        }
    }
}
