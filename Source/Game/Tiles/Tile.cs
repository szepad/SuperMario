namespace Mario2
{
    public abstract class Tile(bool isCollidable)
    {
        public const int Size = 16;

        public bool IsCollidable { get; set; } = isCollidable;

        public abstract void Update();
        public abstract void Draw(int col, int row);
    }
}
