namespace Mario2
{
    public class AnimatedTile(bool isCollidable) : Tile(isCollidable)
    {
        public Sprite Sprite { get; protected set; } = new("tile_anims");
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
