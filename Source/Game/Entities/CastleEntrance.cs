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
            CanBeDestroyed = false;
        }

        protected override void OnSpawn()
        {
            
        }

        protected override void DrawEditor()
        {
            Raylib.DrawTextureEx(Assets.GetTex("editor_exit"), Position, 0f, Game.TextureScale, Color.White);
        }
    }
}
