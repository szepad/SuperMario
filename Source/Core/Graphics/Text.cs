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
        public static void Draw(string text, Vector2 pos, Pivot pivot = Pivot.TopLeft)
            => Draw(text, pos.X, pos.Y, pivot);

        public static void Draw(string text, float x, float y, Pivot pivot = Pivot.TopLeft)
        {
            text = text.ToUpper();
            const int fontSize = 8 * Game.TextureScale;

            int width = (int)Raylib.MeasureTextEx(Assets.Font, text, fontSize, 0f).X;

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

            Raylib.DrawTextEx(Assets.Font, text, new Vector2((int)(x - xoff), (int)(y - yoff)), 
                fontSize, 0, Color.White);
        }
    }
}
