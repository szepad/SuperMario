namespace Mario2.Editor
{
    public static class Shortcuts
    {
        /// <summary>The command modifier as shown to the user.</summary>
        public static string Mod { get; } = OperatingSystem.IsMacOS() ? "Cmd" : "Ctrl";

        /// <summary>Ctrl, or Cmd on macOS (Ctrl works there too).</summary>
        public static bool IsModDown()
            => Raylib.IsKeyDown(KeyboardKey.LeftControl) || Raylib.IsKeyDown(KeyboardKey.RightControl)
            || Raylib.IsKeyDown(KeyboardKey.LeftSuper) || Raylib.IsKeyDown(KeyboardKey.RightSuper);

        public static bool IsShiftDown()
            => Raylib.IsKeyDown(KeyboardKey.LeftShift) || Raylib.IsKeyDown(KeyboardKey.RightShift);

        public static bool IsAltDown()
            => Raylib.IsKeyDown(KeyboardKey.LeftAlt) || Raylib.IsKeyDown(KeyboardKey.RightAlt);

        public static bool Pressed(KeyboardKey key) => Raylib.IsKeyPressed(key);

        public static bool PressedOrRepeated(KeyboardKey key) => Raylib.IsKeyPressed(key) || Raylib.IsKeyPressedRepeat(key);

        public static string WithMod(string key) => $"{Mod}+{key}";
    }
}
