namespace Mario2
{
    public sealed class EdgeDetectionAi(Enemy owner, float walkSpeed) 
        : GoombaAi(owner, walkSpeed)
    {
        public override void Update()
        {
            int row = (int)((Owner.Feet.Y + 4f.Scale()) / Tile.Size);
            int colLeft = (int)(Owner.Rect.X / Tile.Size);
            int colRight = (int)((Owner.Rect.X + Owner.Size.X) / Tile.Size);

            Owner.World.GetTile(colLeft, row, out Tile leftTile);
            Owner.World.GetTile(colRight, row, out Tile rightTile);

            if (leftTile == SimpleTile.Air) Owner.Velocity.X = WalkSpeed;
            if (rightTile == SimpleTile.Air) Owner.Velocity.X = -WalkSpeed;
        }
    }
}
