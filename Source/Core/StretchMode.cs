namespace Mario2
{
    public enum StretchMode
    {
        Normal,
        Stretch,
        LetterBox
    }

    public static class GameViewHandler
    {
        public static Rectangle GetNormalView()
        {
            return new(0, 0, Game.Width, Game.Height);
        }

        public static Rectangle GetStretchView()
        {
            return new(0, 0, Raylib.GetScreenWidth(), Raylib.GetScreenHeight());
        }

        public static Rectangle GetLetterBoxView()
        {
            int x = 0, y = 0;
            int winWidth = Raylib.GetScreenWidth();
            int winHeight = Raylib.GetScreenHeight();
            int actualWidth = winWidth, actualHeight = winHeight;

            const float Aspect = Game.Width / (float)Game.Height;
            float ratio = winWidth / (float)winHeight;

            if (ratio > Aspect)
            {
                if (Game.Instance.Settings.IsIntegerScale)
                {
                    int integerHeight = Game.Height / Game.TextureScale;
                    int scale = 1;
                    while (integerHeight * scale <= winHeight)
                    {
                        scale++;
                    }

                    actualHeight = integerHeight * Math.Max(1, scale - 1);
                }

                float lambda = actualHeight / (float)Game.Height;
                actualWidth = (int)(Game.Width * lambda);
            }
            else
            {
                if (Game.Instance.Settings.IsIntegerScale)
                {
                    int integerWidth = Game.Width / Game.TextureScale;
                    int scale = 1;
                    while (integerWidth * scale <= winWidth)
                    {
                        scale++;
                    }

                    actualWidth = integerWidth * Math.Max(1, scale - 1);
                }

                float lambda = actualWidth / (float)Game.Width;
                actualHeight = (int)(Game.Height * lambda);
            }

            x = (winWidth - actualWidth) / 2;
            y = (winHeight - actualHeight) / 2;

            return new(x, y, actualWidth, actualHeight);
        }
    }
}
