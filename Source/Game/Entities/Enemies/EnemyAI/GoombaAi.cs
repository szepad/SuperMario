namespace Mario2
{
    public class GoombaAi(Enemy owner, float walkSpeed) : EnemyAi(owner)
    {
        public float WalkSpeed { get; set; } = walkSpeed;

        public override void OnSpawn()
        {
            Owner.Velocity.X = -WalkSpeed;
        }

        public override void OnTileCollision(Tile tile, CollisionSide side)
        {
            if (side == CollisionSide.Left)
            {
                Owner.Velocity.X = WalkSpeed;
            }
            else if (side == CollisionSide.Right)
            {
                Owner.Velocity.X = -WalkSpeed;
            }
        }

        public override void OnEntityOverlap(Entity other)
        {
            if (other.Position.X + other.Size.X / 2f < Owner.Position.X + Owner.Size.X / 2f)
            {
                Owner.Velocity.X = WalkSpeed;
            }
            else
            {
                Owner.Velocity.X = -WalkSpeed;
            }
        }
    }
}
