namespace Mario2
{
    public class Axe : Entity
    {
        public Axe(World world)
            : base(world)
        {
            Size = new Vector2(Tile.Size);
            HitboxSize = Size;
            Gravity = 0f;
            IsCollidable = false;
            CanBeDestroyed = false;

            Sprite.SetSheet("axe");
            Sprite.Add("axe", new Animation(3, 16, 16, 0, 0.075f));
        }

        protected override void OnSpawn()
        {
            
        }
    }
}
