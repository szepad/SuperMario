namespace Mario2
{
    public class Mushroom : Item
    {
        private const float MoveSpeed = 40f * Game.TextureScale;
        private const float TileBounceForce = 120f * Game.TextureScale;

        public Mushroom(World world) : base(world)
        {
            Size = new Vector2(16f, 16f).Scale();
            Sprite.Add("mushroom", new Animation(1, 16, 16, 16, 0.1f));
        }

        protected override void OnSpawn()
        {
            base.OnSpawn();
        }

        protected override void OnAppear()
        {
            Velocity.X = MoveSpeed;
        }

        public override void Update()
        {
            base.Update();
        }

        protected override void OnTileCollision(Tile tile, CollisionSide side)
        {
            if (side == CollisionSide.Left)
            {
                Velocity.X = MoveSpeed;
            }
            else if (side == CollisionSide.Right)
            {
                Velocity.X = -MoveSpeed;
            }
        }

        public override void OnTileBounceHit(Entity bouncer, Tile tile)
        {
            Velocity.Y = -TileBounceForce;
            if (Position.X + Size.X / 2f < (tile.Col + 0.5f) * Tile.Size)
            {
                Velocity.X = -MathF.Abs(Velocity.X);
            }
            else
            {
                Velocity.X = MathF.Abs(Velocity.X);
            }
        }
    }
}
