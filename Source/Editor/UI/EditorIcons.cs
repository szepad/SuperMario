using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Managers;

namespace Mario2.Editor
{
    /// <summary>The editor's 16x16 pixel-art icons, in the order they sit in <c>res/editor/editor_icons.png</c>.</summary>
    public enum EditorIcon
    {
        Select, Brush, Rectangle, Erase, Pick, Grid, Snap, Play,
        PlayHere, Level, Copy, Cut, Duplicate, Delete, Detach
    }

    internal static class EditorIcons
    {
        public const int Size = 16;

        private const string Texture = "editor_icons";
        private const int Columns = 8;

        /// <summary>A 16x16 sprite showing the icon at its native size.</summary>
        public static SpriteRuntime Sprite(EditorIcon icon)
        {
            int index = (int)icon;
            SpriteRuntime sprite = new(true, false)
            {
                Texture = Assets.GetTex(Texture),
                TextureAddress = TextureAddress.Custom,
                TextureLeft = index % Columns * Size,
                TextureTop = index / Columns * Size,
                TextureWidth = Size,
                TextureHeight = Size,
                WidthUnits = DimensionUnitType.Absolute,
                HeightUnits = DimensionUnitType.Absolute,
                Width = Size,
                Height = Size
            };
            return sprite;
        }
    }
}
