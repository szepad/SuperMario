namespace Mario2
{
    public class Midway : Entity
    {
        public Midway(World world) : base(world)
        {
            Size = HitboxSize = new Vector2(16f).Scale();
            IsCollidable = false;
            IsHittable = false;
            Gravity = 0f;
            CanBeDestroyed = false;
        }

        protected override void OnSpawn()
        {

        }

        protected override void DrawEditor()
        {
            Raylib.DrawTextureEx(Assets.GetTex("midway"), Position, 0f, Game.TextureScale, Color.White);
        }
    }
}
