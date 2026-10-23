namespace Mario2
{
    public static class TilesetInfo
    {
        public const string TextureName = "tileset";
        public const int TileSizePx = 16;
        public const int Columns = 8;

        // Decorative tiles of the tileset that the player can pass through
        private static readonly HashSet<int> _passableIds =
        [
            29, 37, 40,
            44, 45, 46, 47,
            49, 50, 51, 52,
            57, 58, 59, 60,
            64, 65, 66, 67, 68, 69,
            72, 73, 74, 75, 76, 77,
            80, 81, 82, 83, 84, 85,
            88, 89, 90, 91, 92, 93, 94,
            96, 97, 98, 99, 100, 101, 102,
            110,
        ];

        public static int Count
        {
            get
            {
                Texture2D tex = Assets.GetTex(TextureName);
                return tex.Width / TileSizePx * (tex.Height / TileSizePx);
            }
        }

        public static bool IsSolidByDefault(int id) => !_passableIds.Contains(id);

        public static Rectangle SourceRect(int id)
            => new(id % Columns * TileSizePx, id / Columns * TileSizePx, TileSizePx, TileSizePx);
    }
}
