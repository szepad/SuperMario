namespace Mario2
{
    public class SimpleTile(int id, bool isCollidable) : Tile(isCollidable)
    {
        public static readonly SimpleTile Air = new(-1, false);

        public int Id { get; set; } = id;

        public override void Update()
        {

        }

        public override void Draw(int col, int row)
        {
            Raylib.DrawTexturePro(Assets.GetTex("tileset"),
                new Rectangle((Id % 8) * Size, (int)(Id / 8f) * Size, Size, Size),
                new Rectangle(col * Size, row * Size, Size, Size),
                Vector2.Zero, 0f, Color.White);
        }
    }
}
