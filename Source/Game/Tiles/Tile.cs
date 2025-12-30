using Mario2.Editor;

namespace Mario2
{
    public abstract class Tile(bool isCollidable)
    {
        public const int Size = 16 * Game.TexScale;

        public bool IsCollidable { get; set; } = isCollidable;
        public RuleTile? Rule { get; set; } = null;

        public abstract void Update();
        public abstract void Draw(int col, int row, float opacity = 1f);
    }
}
