namespace Mario2
{
    public class CastleEntrance : Entity
    {
        public CastleEntrance(World world) : base(world)
        {
            Size = HitboxSize = new Vector2(16f).Scale();
            IsCollidable = false;
            IsHittable = false;
            Gravity = 0f;
        }

        protected override void OnSpawn()
        {
            
        }
    }
}
