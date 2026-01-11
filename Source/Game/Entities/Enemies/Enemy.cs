namespace Mario2
{
    public abstract class Enemy : Entity
    {
        public bool IsStompable { get; set; } = true;
        public bool KillableWithTileBounce { get; set; } = true;
        public EnemyAi Ai { get; set; }

        public bool IsDead { get; private set; }
        public bool IsStomped { get; private set; }

        private const float DieBounceForce = 120f * Game.TextureScale;

        public Enemy(World world) : base(world)
        {
            Ai = new DummyAi(this);
        }

        protected override void OnSpawn()
        {
            Ai.OnSpawn();
        }

        public void Stomp(Entity stomper)
        {
            if (IsStomped || !IsStompable) return;
            IsStomped = true;

            OnStomp(stomper);
            Ai.OnStomp(stomper);
        }

        public void Die()
        {
            if (IsDead) return;
            IsDead = true;

            IsHittable = false;
            IsCollidable = false;
            Velocity.Y = -DieBounceForce;
            Sprite.FlipY = true;

            OnDie();
            Ai.OnDie();
        }

        public override void OnTileBounceHit(Entity bouncer, Tile tile)
        {
            if (KillableWithTileBounce)
            {
                if (Position.X + Size.X / 2f < (tile.Col + 0.5f) * Tile.Size)
                {
                    Velocity.X = -MathF.Abs(Velocity.X);
                }
                else
                {
                    Velocity.X = MathF.Abs(Velocity.X);
                }

                Die();
            }
        }

        public override void Update()
        {
            Ai.Update();
            base.Update();
        }

        protected virtual void OnStomp(Entity stomper) { }
        protected virtual void OnDie() { }

        protected override void OnTileCollision(Tile tile, CollisionSide side)
        {
            Ai.OnTileCollision(tile, side);
        }

        protected override void OnTileOverlap(Tile tile)
        {
            Ai.OnTileOverlap(tile);
        }

        protected override void OnEntityOverlap(Entity other)
        {
            Ai.OnEntityOverlap(other);
        }
    }
}
