using Mario2.Editor;

namespace Mario2
{
    public abstract class Tile(bool isCollidable)
    {
        public const int Size = 16 * Game.TextureScale;

        public World? World { get; set; } = null;
        public int Col { get; set; }
        public int Row { get; set; }

        public bool IsCollidable { get; set; } = isCollidable;
        public bool IsDestroyed { get; private set; }
        public RuleTile? Rule { get; set; } = null;
        
        public List<CollisionSide> CollisionExclude { get; } = [];

        public event Action<Tile>? OnDestroy;

        public void Destroy()
        {
            if (IsDestroyed)
                return;

            IsDestroyed = true;
            OnDestroy?.Invoke(this);
        }

        public abstract void Update();
        public abstract void Draw(int col, int row, float opacity = 1f);
    }
}
