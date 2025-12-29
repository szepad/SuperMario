namespace Mario2
{
    public class SimpleTile(int id, bool isCollidable) : Tile(isCollidable)
    {
        public static readonly SimpleTile Air = new(-1, false);

        public int Id { get; set; } = id;

        public override void Update()
        {

        }

        public override void Draw(int col, int row, float opacity = 1f)
        {
            Texture2D tileset = Assets.GetTex("tileset");
            const int TileSizePx = 16;
            int cols = tileset.Width / TileSizePx;

            Raylib.DrawTexturePro(tileset,
                new Rectangle((Id % cols) * TileSizePx, (int)(Id / cols) * TileSizePx, TileSizePx, TileSizePx),
                new Rectangle(col * Size, row * Size, Size, Size),
                Vector2.Zero, 0f, Color.White.Alpha(opacity));
        }
    }
}
