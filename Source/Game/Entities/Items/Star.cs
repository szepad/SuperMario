namespace Mario2
{
    public class Star : Item
    {
        private const float MoveSpeed = 40f * Game.TextureScale;
        private const float JumpForce = 200f * Game.TextureScale;

        public Star(World world) : base(world)
        {
            Size = new Vector2(Tile.Size);
            Sprite.Add("star", new Animation(4, 16, 16, 64, 0.1f));
        }

        protected override void OnSpawn()
        {
            base.OnSpawn();
        }

        protected override void OnAppear()
        {
            Velocity.X = MoveSpeed;
        }

        protected override void OnDestroy()
        {

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
            else if (side == CollisionSide.Bottom)
            {
                Velocity.Y = -JumpForce;
            }
        }
    }
}
