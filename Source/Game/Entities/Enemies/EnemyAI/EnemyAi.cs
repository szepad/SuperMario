namespace Mario2
{
    public abstract class EnemyAi(Enemy owner)
    {
        public Enemy Owner { get; } = owner;

        public virtual void OnSpawn() { }

        public virtual void OnStomp(Entity stomper) { }
        public virtual void OnDie() { }

        public virtual void OnTileCollision(Tile tile, CollisionSide side) { }
        public virtual void OnTileOverlap(Tile tile) { }
        public virtual void OnEntityOverlap(Entity other) { }

        public virtual void Update() { }
    }
}
