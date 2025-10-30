namespace Mario2
{
    public static class Input
    {
        public static bool IsKeyDown(KeyboardKey key)
        {
            return Raylib.IsKeyDown(key);
        }

        public static bool IsKeyUp(KeyboardKey key)
        {
            return Raylib.IsKeyUp(key);
        }

        public static bool IsKeyPressed(KeyboardKey key)
        {
            return Raylib.IsKeyPressed(key);
        }
    }
}
