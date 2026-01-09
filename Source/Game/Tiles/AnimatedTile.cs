namespace Mario2
{
    public class AnimatedTile(string sheetName, bool isCollidable) : Tile(isCollidable)
    {
        public Sprite Sprite { get; protected set; } = new(sheetName);
        public Vector2 SpriteOffset;

        public override void Update()
        {
            Sprite.Update();
        }

        public override void Draw(int col, int row, float opacity = 1)
        {
            Sprite.Draw(new Vector2(col, row) * Size - SpriteOffset, opacity);
        }
    }
}
